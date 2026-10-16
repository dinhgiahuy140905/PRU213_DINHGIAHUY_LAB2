using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Snowboarding Adventure - core player controller.
///
/// The downhill momentum is produced by the <see cref="SurfaceEffector2D"/> that is
/// part of the level ("Slope Physics"). This script adds:
///  * responsive steering: on the snow A/D (or ←/→) <b>leans / carves</b> the board
///    (limited angle, auto-aligned to the slope), while in the air it <b>spins</b>
///    freely so tricks can be performed,
///  * jumping (Space / Up / W / gamepad A),
///  * speed control: tuck &amp; boost (Shift / right trigger) and brake (Down / S),
///  * a boost hook that power-ups can trigger,
///  * a safety net that puts the board back on the last safe spot if it ever leaves
///    the level.
///
/// The serialized field names (torqueAmount / boostSpeed / baseSpeed) are kept so the
/// pre-built level scene keeps its tuned values.
///
/// Controls
///   Left/Right arrow or A/D .. steer (carve on the ground, spin in the air)
///   Space, Up or W .......... jump
///   Left Shift .............. tuck &amp; boost
///   Down or S ............... brake
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Steering - air")]
    [Tooltip("Gain of the airborne spin torque (kept for tuning).")]
    [SerializeField] float torqueAmount = 10f;

    [Tooltip("Maximum airborne spin speed in degrees per second.")]
    [SerializeField] float maxTurnSpeed = 800f;

    [Header("Steering - ground")]
    [Tooltip("Fastest the board can spin while on the snow (degrees / second).")]
    [SerializeField] float carveTurnRate = 600f;

    [Tooltip("Maximum lean used when self-righting with no input (degrees).")]
    [SerializeField] float carveAngle = 40f;

    [Tooltip("Fastest self-righting turn (degrees / second).")]
    [SerializeField] float rightingTurnRate = 400f;

    [Tooltip("How quickly the board reaches that turn rate (degrees / second squared).")]
    [SerializeField] float carveAcceleration = 9000f;

    [Tooltip("How quickly the airborne spin reaches its rate (degrees / second squared).")]
    [SerializeField] float spinAcceleration = 9000f;

    [Header("Speed (Surface Effector)")]
    [Tooltip("Surface speed while tucking / boosting.")]
    [SerializeField] float boostSpeed = 40f;
    [Tooltip("Cruising surface speed.")]
    [SerializeField] float baseSpeed = 20f;
    [Tooltip("Surface speed while braking.")]
    [SerializeField] float brakeSpeed = 8f;
    [Header("Power-up boost")]
    [Tooltip("How long a speed-boost power-up lasts.")]
    [SerializeField] float boostDuration = 2.5f;

    [Header("Jump")]
    [Tooltip("Impulse applied when hopping over obstacles.")]
    [SerializeField] float jumpForce = 18f;
    [Tooltip("Minimum time between two jumps.")]
    [SerializeField] float jumpCooldown = 0.12f;
    [Tooltip("A jump pressed this long before landing is remembered (makes it feel snappy).")]
    [SerializeField] float jumpBufferTime = 0.18f;
    [Tooltip("Grace period after leaving the ground during which a jump still works.")]
    [SerializeField] float coyoteTime = 0.15f;

    [Header("Safety")]
    [Tooltip("How far below the snow the board may go before being put back.")]
    [SerializeField] float fallMargin = 22f;

    [Tooltip("Landing with the board rotated further than this wipes the rider out (degrees).")]
    [SerializeField] float headCrashAngle = 110f;

    /// <summary>False while the player crashed or the level is finished.</summary>
    public bool CanMove { get; private set; } = true;

    /// <summary>True while the player is holding the boost key or a boost power-up is active.</summary>
    public bool IsBoosting => boostTimer > 0f || boostHeld;

    /// <summary>True while the brake key is held.</summary>
    public bool IsBraking => brakeHeld;

    /// <summary>True while the board is on the snow.</summary>
    public bool IsGrounded => trickController == null || trickController.IsGrounded;

    /// <summary>Current speed in Unity units / second.</summary>
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

    /// <summary>How fast the boarder can go, and the "boost" speed while tucking.</summary>
    public float BaseSpeed => baseSpeed;

    /// <summary>Speed shown in the HUD (a friendlier "km/h" style number).</summary>
    public float DisplaySpeed => CurrentSpeed * 3.6f;

    /// <summary>The game's reference top speed, used for scoring and the HUD bar.</summary>
    public float MaxSpeed => boostSpeed;

    Rigidbody2D rb;
    SurfaceEffector2D surfaceEffector;
    TrickController trickController;
    CrashDetector crashDetector;
    float boostTimer;
    bool boostHeld;
    bool brakeHeld;
    bool jumpPressed;
    float steerInput;
    float lastJumpTime = -10f;
    float jumpBufferTimer;
    float lastGroundedTime = -10f;
    float airTime;
    bool wasGrounded = true;
    Vector3 lastSafePosition;

    // Sampled height of the snow, used by the "fell out of the level" safety net.
    float[] surfaceSamples;
    float surfaceMinX;
    const float SurfaceStep = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Make absolutely sure a fast boarder cannot tunnel through the thin slope collider.
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }
    }

    void Start()
    {
        surfaceEffector = FindAnyObjectByType<SurfaceEffector2D>();
        trickController = GetComponent<TrickController>();
        crashDetector = GetComponent<CrashDetector>();

        GameManager.EnsureExists();
        ScoreManager.EnsureExists().SetPlayer(this);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyDifficultyTo(this);
        }
        else if (surfaceEffector != null)
        {
            surfaceEffector.speed = baseSpeed;
        }

        BuildSurfaceTable();
        lastSafePosition = transform.position;
    }

    // =====================================================================
    //  Input
    // =====================================================================

    void Update()
    {
        ReadInput();

        // Remember a jump press for a moment so it is not lost while airborne.
        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (!CanMove)
        {
            steerInput = 0f;
            return;
        }

        UpdateSurfaceSpeed();
        CheckOutOfBounds();
    }

    void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        boostHeld = false;
        brakeHeld = false;
        jumpPressed = false;
        float steer = 0f;

        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) steer += 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) steer -= 1f;

            boostHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            brakeHeld = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed;

            jumpPressed = keyboard.spaceKey.wasPressedThisFrame
                          || keyboard.upArrowKey.wasPressedThisFrame
                          || keyboard.wKey.wasPressedThisFrame;
        }
        else if (LegacyInputAvailable())
        {
            // Fallback: the new Input System has no keyboard device (this happens in
            // batch mode and in some editor sessions) - use the classic Input API.
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A)) steer += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D)) steer -= 1f;

            boostHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            brakeHeld = UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.S);

            jumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space)
                          || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                          || UnityEngine.Input.GetKeyDown(KeyCode.W);
        }

        if (gamepad != null)
        {
            if (gamepad.leftStick != null)
            {
                float stick = gamepad.leftStick.x.ReadValue();
                if (Mathf.Abs(stick) > 0.2f) steer = -stick;
            }

            if (gamepad.rightTrigger != null) boostHeld |= gamepad.rightTrigger.isPressed;
            if (gamepad.leftTrigger != null) brakeHeld |= gamepad.leftTrigger.isPressed;
            if (gamepad.buttonSouth != null) jumpPressed |= gamepad.buttonSouth.wasPressedThisFrame;
        }

        steerInput = Mathf.Clamp(steer, -1f, 1f);

        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
        }
    }

    static bool legacyInputChecked;
    static bool legacyInputWorks;

    /// <summary>
    /// True when the classic <see cref="UnityEngine.Input"/> API is usable (the project
    /// must be set to "Both" in Player Settings → Active Input Handling).
    /// </summary>
    static bool LegacyInputAvailable()
    {
        if (legacyInputChecked)
        {
            return legacyInputWorks;
        }

        legacyInputChecked = true;

        try
        {
            // This throws when the project is set to "Input System Package (New)" only.
            UnityEngine.Input.GetKey(KeyCode.None);
            legacyInputWorks = true;
        }
        catch (System.Exception)
        {
            legacyInputWorks = false;
        }

        return legacyInputWorks;
    }

    // =====================================================================
    //  Physics
    // =====================================================================

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        bool grounded = IsGrounded;

        if (grounded)
        {
            lastGroundedTime = Time.time;

            // Landing on the head (the board upside down) is a wipeout: it costs a life.
            if (!wasGrounded && airTime > 0.15f
                && Mathf.Abs(Mathf.DeltaAngle(0f, rb.rotation)) > headCrashAngle)
            {
                crashDetector?.TriggerHeadCrash();
            }

            airTime = 0f;
        }
        else
        {
            airTime += Time.fixedDeltaTime;
        }

        wasGrounded = grounded;

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;
        }

        ClampVelocity();

        // Remember the last spot where the board was safely on the snow.
        if (CanMove && grounded && CurrentSpeed > 1f)
        {
            lastSafePosition = transform.position;
        }

        if (!CanMove)
        {
            return;
        }

        // Buffered jump: the press is remembered for a moment, and the boarder may be a
        // few centimetres off the snow (coyote time) - both keep the input feeling snappy.
        if (jumpBufferTimer > 0f)
        {
            Jump();
        }

        if (grounded)
        {
            Carve();
        }
        else
        {
            Spin();
        }
    }

    /// <summary>On the snow: pressing left/right spins the board; releasing self-rights it.</summary>
    void Carve()
    {
        float step = carveAcceleration * Mathf.Deg2Rad * Time.fixedDeltaTime;

        if (Mathf.Abs(steerInput) > 0.01f)
        {
            // Spin with the same feel as in the air, so the input is unmistakable.
            float wantedRate = steerInput * carveTurnRate * Mathf.Deg2Rad;
            rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, wantedRate, step);
            return;
        }

        // No input: gently line the board up with the slope (and never stay upside down).
        Vector2 velocity = rb.linearVelocity;
        float target = velocity.sqrMagnitude > 4f
            ? Mathf.Clamp(Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg, -carveAngle, carveAngle)
            : 0f;

        float error = Mathf.DeltaAngle(rb.rotation, target);
        float wanted = Mathf.Clamp(error * 6f, -rightingTurnRate, rightingTurnRate) * Mathf.Deg2Rad;
        rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, wanted, step);
    }

    /// <summary>In the air: spin freely and smoothly so tricks can be chained.</summary>
    void Spin()
    {
        float wantedRate = steerInput * maxTurnSpeed * Mathf.Deg2Rad;
        float acceleration = Mathf.Abs(steerInput) > 0.01f ? spinAcceleration : spinAcceleration * 0.4f;

        rb.angularVelocity = Mathf.MoveTowards(
            rb.angularVelocity, wantedRate, acceleration * Mathf.Deg2Rad * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Applies the jump impulse. Called by the buffered jump logic, and public so it can
    /// also be driven from the UI or an automated test.
    /// </summary>
    public bool Jump()
    {
        if (rb == null || Time.time - lastJumpTime < jumpCooldown)
        {
            return false;
        }

        // Must be on the snow (or have just left it) and not already rising fast.
        if (Time.time - lastGroundedTime >= coyoteTime || rb.linearVelocity.y > 6f)
        {
            return false;
        }

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        lastJumpTime = Time.time;
        jumpBufferTimer = 0f;
        lastGroundedTime = -10f;
        return true;
    }

    void UpdateSurfaceSpeed()
    {
        if (surfaceEffector == null)
        {
            return;
        }

        if (brakeHeld)
        {
            surfaceEffector.speed = brakeSpeed;
        }
        else
        {
            surfaceEffector.speed = IsBoosting ? boostSpeed : baseSpeed;
        }
    }

    /// <summary>
    /// Hard safety limit: a physics glitch (deep penetration, a squeezed collider...)
    /// must never be able to fling the board into orbit.
    /// </summary>
    void ClampVelocity()
    {
        float speedCap = Mathf.Max(50f, boostSpeed * 1.8f);
        if (rb.linearVelocity.sqrMagnitude > speedCap * speedCap)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speedCap;
        }

        float spinCap = maxTurnSpeed * Mathf.Deg2Rad * 2f;
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -spinCap, spinCap);
    }

    // =====================================================================
    //  Safety net
    // =====================================================================

    /// <summary>Samples the snow height every couple of units so the board can be caught.</summary>
    void BuildSurfaceTable()
    {
        GameObject terrain = GameObject.Find("Level Sprite Shape");
        EdgeCollider2D edge = terrain != null ? terrain.GetComponent<EdgeCollider2D>() : null;
        if (edge == null)
        {
            surfaceSamples = null;
            return;
        }

        Vector2[] local = edge.points;
        Vector2[] world = new Vector2[local.Length];
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < local.Length; i++)
        {
            world[i] = edge.transform.TransformPoint(local[i]);
            minX = Mathf.Min(minX, world[i].x);
            maxX = Mathf.Max(maxX, world[i].x);
        }

        surfaceMinX = minX;
        int count = Mathf.CeilToInt((maxX - minX) / SurfaceStep) + 1;
        surfaceSamples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float x = minX + i * SurfaceStep;
            float best = float.NegativeInfinity;

            for (int j = 0; j < world.Length - 1; j++)
            {
                Vector2 a = world[j];
                Vector2 b = world[j + 1];

                if ((a.x <= x && x <= b.x) || (b.x <= x && x <= a.x))
                {
                    float t = Mathf.Approximately(a.x, b.x) ? 0f : (x - a.x) / (b.x - a.x);
                    best = Mathf.Max(best, Mathf.Lerp(a.y, b.y, t));
                }
            }

            surfaceSamples[i] = best;
        }
    }

    float SampleSurface(float x)
    {
        if (surfaceSamples == null || surfaceSamples.Length == 0)
        {
            return float.NegativeInfinity;
        }

        int index = Mathf.Clamp(Mathf.RoundToInt((x - surfaceMinX) / SurfaceStep), 0, surfaceSamples.Length - 1);
        return surfaceSamples[index];
    }

    /// <summary>
    /// If the boarder ever ends up far below the snow (a physics glitch or a very
    /// unlucky fast landing) it is put back on the last safe spot instead of falling
    /// out of the world.
    /// </summary>
    void CheckOutOfBounds()
    {
        float surface = SampleSurface(transform.position.x);
        float limit = float.IsNegativeInfinity(surface)
            ? transform.position.y - 80f
            : surface - fallMargin;

        if (transform.position.y > limit)
        {
            return;
        }

        transform.position = lastSafePosition + Vector3.up * 1.5f;
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (surfaceEffector != null)
        {
            surfaceEffector.speed = baseSpeed;
        }
    }

    // =====================================================================
    //  Public helpers used by other systems
    // =====================================================================

    /// <summary>Temporary speed boost given by a power-up.</summary>
    public void ApplyBoost(float duration)
    {
        boostTimer = Mathf.Max(boostTimer, duration > 0f ? duration : boostDuration);
    }

    /// <summary>Small hop so the board rides over a low obstacle instead of getting stuck.</summary>
    public void ApplySoftBounce(float force = 6f)
    {
        if (rb != null)
        {
            rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        }
    }

    /// <summary>Launch used by the jump ramps.</summary>
    public void Launch(float forward, float up)
    {
        if (rb != null)
        {
            rb.AddForce(new Vector2(forward, up), ForceMode2D.Impulse);
        }
    }

    /// <summary>Slows the player down after brushing an obstacle. Keeps a fraction of the speed.</summary>
    public void ApplySpeedPenalty(float keptFraction)
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity *= Mathf.Clamp01(keptFraction);
        rb.angularVelocity *= Mathf.Clamp01(keptFraction);
    }

    public void DisableControls() => CanMove = false;

    public void EnableControls() => CanMove = true;

    /// <summary>Used by the main menu to apply a difficulty preset to the player.</summary>
    public void ApplyDifficulty(float baseSpeedValue, float boostSpeedValue)
    {
        baseSpeed = baseSpeedValue;
        boostSpeed = boostSpeedValue;
        brakeSpeed = Mathf.Max(4f, baseSpeedValue * 0.4f);
        UpdateSurfaceSpeed();
    }
}
