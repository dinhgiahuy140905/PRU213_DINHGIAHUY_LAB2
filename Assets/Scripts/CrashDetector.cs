using System.Collections;
using UnityEngine;

/// <summary>
/// Handling Collisions and Triggers - the crash side of a collision.
///
/// When the player hits an <see cref="Obstacle"/> the crash effect and the crash
/// sound play, the player loses control and <see cref="GameManager"/> removes a life.
/// Non-fatal obstacles only scrub speed.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class CrashDetector : MonoBehaviour
{
    [Header("Feedback")]
    [Tooltip("Delay before the level reacts (lets the crash effect be seen).")]
    [SerializeField] float loadDelay = 1f;
    [Tooltip("How long the boarder blinks after a soft hit (ice patch).")]
    [SerializeField] float softHitBlink = 0.5f;
    [SerializeField] ParticleSystem crashEffect;
    [SerializeField] AudioClip crashSFX;

    [Header("Soft hit")]
    [Tooltip("Fraction of the speed that is kept after hitting a non-fatal obstacle.")]
    [Range(0f, 1f)]
    [SerializeField] float crashSpeedPenalty = 0.45f;

    PlayerController controller;
    AudioSource audioSource;
    InvincibilityBlink blink;
    bool hasCrashed;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        blink = GetComponent<InvincibilityBlink>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContact(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleContact(other);
    }

    void HandleContact(Collider2D other)
    {
        if (other == null || hasCrashed)
        {
            return;
        }

        Obstacle obstacle = other.GetComponentInParent<Obstacle>();
        if (obstacle == null)
        {
            return;
        }

        // Invincibility power-up: shatter the obstacle and keep going.
        if (GameManager.Instance != null && GameManager.Instance.IsInvincible)
        {
            obstacle.Break();
            return;
        }

        if (obstacle.IsFatal)
        {
            StartCoroutine(Crash());
        }
        else
        {
            // Soft obstacle (ice patch): scrub speed once per contact and blink.
            if (!obstacle.TryTakeHit())
            {
                return;
            }

            controller.ApplySpeedPenalty(crashSpeedPenalty);
            ScoreManager.Instance?.BreakCombo();
            obstacle.OnHit();
            blink?.BlinkHurt(softHitBlink);
        }
    }

    /// <summary>Called when the board lands on its head - a wipeout, so a life is lost.</summary>
    public void TriggerHeadCrash()
    {
        if (!hasCrashed)
        {
            StartCoroutine(Crash());
        }
    }

    /// <summary>Full crash: feedback, scoring reset and a life lost.</summary>
    public IEnumerator Crash()
    {
        if (hasCrashed)
        {
            yield break;
        }

        hasCrashed = true;
        controller.DisableControls();
        ScoreManager.Instance?.BreakCombo();

        if (crashEffect != null)
        {
            crashEffect.Play();
        }

        if (crashSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(crashSFX, 1f);
        }

        // The damage feedback: the boarder blinks while the level restarts.
        if (blink == null)
        {
            blink = GetComponent<InvincibilityBlink>();
        }

        blink?.BlinkHurt();

        yield return new WaitForSeconds(loadDelay);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerCrashed();
        }
    }

    /// <summary>Lets the level reset the crash state after a respawn.</summary>
    public void ResetCrash()
    {
        hasCrashed = false;
    }
}
