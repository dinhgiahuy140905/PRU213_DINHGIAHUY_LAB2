using UnityEngine;

/// <summary>
/// Visuals and Aesthetics - the boarder's damage / power-up feedback.
///
/// Two states are shown by blinking the boarder's sprites:
///  * <b>invincible</b> (power-up active): soft cyan blink for as long as it lasts,
///  * <b>hurt</b> (just hit an obstacle): a short red blink, which is the "lost health"
///    feedback requested for the lab.
///
/// The class keeps its original name so the pre-built scene keeps its reference.
/// </summary>
public class InvincibilityBlink : MonoBehaviour
{
    [Header("Invincible")]
    [SerializeField] float blinkSpeed = 8f;
    [SerializeField] Color invincibleTint = new Color(0.6f, 0.95f, 1f);

    [Header("Hurt")]
    [Tooltip("Default duration of the red blink when an obstacle is hit.")]
    [SerializeField] float hurtDuration = 1.1f;
    [SerializeField] float hurtBlinkSpeed = 16f;
    [SerializeField] Color hurtTint = new Color(1f, 0.45f, 0.45f);

    SpriteRenderer[] renderers;
    Color[] originalColors;
    float hurtUntil;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
        }
    }

    /// <summary>Short red blink used as the "you lost health" feedback.</summary>
    public void BlinkHurt(float duration = -1f)
    {
        hurtUntil = Time.unscaledTime + (duration > 0f ? duration : hurtDuration);
    }

    void Update()
    {
        bool invincible = GameManager.Instance != null && GameManager.Instance.IsInvincible;
        bool hurt = Time.unscaledTime < hurtUntil;

        float alpha = 1f;
        Color tint = Color.white;

        if (invincible)
        {
            alpha = Mathf.Lerp(0.4f, 1f, (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f);
            tint = invincibleTint;
        }
        else if (hurt)
        {
            alpha = Mathf.Lerp(0.15f, 1f, (Mathf.Sin(Time.unscaledTime * hurtBlinkSpeed) + 1f) * 0.5f);
            tint = hurtTint;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Color color = alpha >= 1f && !invincible && !hurt ? originalColors[i] : tint;
            color.a = originalColors[i].a * alpha;
            renderers[i].color = color;
        }
    }

    void OnDisable()
    {
        // Never leave the boarder invisible.
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i];
            }
        }
    }
}
