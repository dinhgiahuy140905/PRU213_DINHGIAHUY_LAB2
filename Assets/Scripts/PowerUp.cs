using UnityEngine;

/// <summary>
/// Handling Collisions and Triggers - power-ups.
///
/// These are triggers that temporarily change the player's state:
///  * SpeedBoost   - a temporary burst of speed,
///  * Invincibility - the player smashes through obstacles,
///  * ExtraLife    - one more life.
///
/// A collected power-up hides for a while and then comes back, so the course does not
/// slowly empty out.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PowerUp : MonoBehaviour
{
    public enum Kind
    {
        SpeedBoost,
        Invincibility,
        ExtraLife
    }

    [SerializeField] Kind kind = Kind.SpeedBoost;
    [SerializeField] float boostDuration = 2.5f;
    [SerializeField] float invincibilityDuration = 5f;
    [SerializeField] float spinSpeed = 90f;
    [Tooltip("Seconds before this power-up reappears.")]
    [SerializeField] float respawnDelay = 20f;
    [SerializeField] AudioClip pickupSFX;

    HUDController hud;
    Vector3 startPosition;
    bool taken;

    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        hud = FindAnyObjectByType<HUDController>();
        startPosition = transform.position;
    }

    void Update()
    {
        if (taken)
        {
            return;
        }

        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        transform.position = startPosition + new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.3f, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        string message = string.Empty;

        switch (kind)
        {
            case Kind.SpeedBoost:
                player.ApplyBoost(boostDuration);
                message = "SPEED BOOST!";
                break;
            case Kind.Invincibility:
                GameManager.Instance?.AddInvincibility(invincibilityDuration);
                message = "INVINCIBLE!";
                break;
            case Kind.ExtraLife:
                GameManager.Instance?.AddLife();
                message = "+1 LIFE";
                break;
        }

        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, GameSettings.MasterVolume);
        }

        hud?.ShowBanner(message);
        hud?.Refresh();

        StartCoroutine(Respawn());
    }

    /// <summary>Hides the power-up for a while instead of destroying it for good.</summary>
    System.Collections.IEnumerator Respawn()
    {
        SetVisible(false);
        yield return new WaitForSeconds(respawnDelay);
        transform.position = startPosition;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        taken = !visible;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = visible;
        }
    }
}
