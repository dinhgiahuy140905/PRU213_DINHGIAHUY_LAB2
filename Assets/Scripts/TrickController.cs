using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game Logic and Scoring Systems - tricks and combos.
///
/// While the player is airborne we accumulate the rotation on the Z axis. On landing
/// the total rotation is rounded to the nearest 180 degrees and awarded as a trick
/// (180 / 360 / 540 / 720), which feeds the combo multiplier in
/// <see cref="ScoreManager"/>.
///
/// "Grounded" is tracked through the player's own collision events, which is both
/// cheaper and more reliable than a raycast (the boarder's colliders sit well below
/// the transform pivot).
/// </summary>
public class TrickController : MonoBehaviour
{
    [Tooltip("Minimum time in the air before a rotation counts as a trick.")]
    [SerializeField] float minAirTime = 0.25f;

    public bool IsAirborne { get; private set; }
    public float AirTime { get; private set; }
    public float Rotation { get; private set; }
    public bool IsGrounded => groundContacts.Count > 0;

    readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();

    float lastZ;
    HUDController hud;

    void Start()
    {
        lastZ = transform.eulerAngles.z;
        hud = FindAnyObjectByType<HUDController>();
    }

    void Update()
    {
        // Drop references to colliders that were destroyed (e.g. a broken obstacle).
        if (groundContacts.Count > 0)
        {
            groundContacts.RemoveWhere(collider => collider == null);
        }

        float z = transform.eulerAngles.z;
        float delta = Mathf.DeltaAngle(lastZ, z);
        lastZ = z;

        if (!IsGrounded)
        {
            IsAirborne = true;
            AirTime += Time.deltaTime;
            Rotation += Mathf.Abs(delta);
        }
        else
        {
            if (IsAirborne && AirTime >= minAirTime)
            {
                Land();
            }

            IsAirborne = false;
            AirTime = 0f;
            Rotation = 0f;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsGround(collision.collider))
        {
            groundContacts.Add(collision.collider);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        groundContacts.Remove(collision.collider);
    }

    static bool IsGround(Collider2D collider)
    {
        // Anything that is not a trigger and not an obstacle counts as rideable snow.
        return collider != null && !collider.isTrigger && collider.GetComponentInParent<Obstacle>() == null;
    }

    void Land()
    {
        int degrees = Mathf.RoundToInt(Rotation / 180f) * 180;
        if (degrees < 180)
        {
            return;
        }

        ScoreManager.Instance?.RegisterTrick(degrees);

        string name = degrees switch
        {
            180 => "SHOVE-IT!",
            360 => "360 SPIN!",
            540 => "540 SPIN!",
            _ => "MEGA SPIN!"
        };

        hud?.ShowBanner($"{name}   +{degrees}");
    }
}
