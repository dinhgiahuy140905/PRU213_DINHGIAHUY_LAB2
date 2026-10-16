using UnityEngine;

/// <summary>
/// Obstacle Course and Challenges - collectible snowflakes.
///
/// A trigger the player flies through. Collecting adds points (scaled by the combo
/// multiplier) and extends the combo, so chaining snowflakes is worth more.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [SerializeField] int points = 50;
    [SerializeField] float spinSpeed = 120f;
    [SerializeField] float bobAmplitude = 0.25f;
    [SerializeField] float bobSpeed = 2f;
    [Tooltip("The snowflake comes back after this many seconds so the course never empties.")]
    [SerializeField] float respawnDelay = 8f;
    [SerializeField] AudioClip pickupSFX;
    [SerializeField] GameObject pickupEffectPrefab;

    Vector3 startPosition;
    HUDController hud;
    bool taken;

    void Start()
    {
        startPosition = transform.position;
        GetComponent<Collider2D>().isTrigger = true;
        hud = FindAnyObjectByType<HUDController>();
    }

    void Update()
    {
        if (taken)
        {
            return;
        }

        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (bobAmplitude > 0f)
        {
            float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = startPosition + new Vector3(0f, y, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        ScoreManager manager = ScoreManager.Instance;
        manager?.CollectItem(points);
        int awarded = points * (manager != null ? manager.Multiplier : 1);

        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, GameSettings.MasterVolume);
        }

        if (pickupEffectPrefab != null)
        {
            Destroy(Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity), 2f);
        }

        hud?.ShowBanner($"+{awarded}  SNOWFLAKE", 0.8f);
        StartCoroutine(Respawn());
    }

    /// <summary>Hides the snowflake for a moment instead of destroying it for good.</summary>
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
