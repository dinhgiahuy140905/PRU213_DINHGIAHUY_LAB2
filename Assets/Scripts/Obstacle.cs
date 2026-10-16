using UnityEngine;

/// <summary>
/// Handling Collisions and Triggers - obstacle behaviour.
///
/// Obstacles are the trees and ice patches on the piste. A "fatal" obstacle ends the
/// run (the crash detector handles that), while a "slow" obstacle only scrubs speed.
/// Invincibility breaks the obstacle and awards a small bonus.
///
/// An obstacle only ever affects the player <b>once per contact</b>: without that, a
/// pass-through trigger (an ice patch) that the board keeps overlapping would scrub
/// 45% of the speed every frame and the player could never escape it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    public enum Kind
    {
        Fatal,
        Slow
    }

    [SerializeField] Kind kind = Kind.Fatal;
    [SerializeField] int breakBonus = 75;
    [SerializeField] AudioClip hitSFX;

    public bool IsFatal => kind == Kind.Fatal;

    Collider2D cachedCollider;
    bool consumedThisContact;

    void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            consumedThisContact = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<PlayerController>() != null)
        {
            consumedThisContact = false;
        }
    }

    /// <summary>
    /// Returns true only the first time the obstacle touches the player; further
    /// contacts while still overlapping return false.
    /// </summary>
    public bool TryTakeHit()
    {
        if (consumedThisContact)
        {
            return false;
        }

        consumedThisContact = true;
        return true;
    }

    /// <summary>Non-fatal hit: feedback only.</summary>
    public void OnHit()
    {
        if (hitSFX != null)
        {
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, GameSettings.MasterVolume);
        }
    }

    /// <summary>
    /// Called when the player is invincible and smashes through: the obstacle awards its
    /// bonus and lets the board pass, but it stays visible and comes back after a moment.
    /// </summary>
    public void Break()
    {
        ScoreManager.Instance?.AwardFinishBonus(breakBonus);

        if (hitSFX != null)
        {
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, GameSettings.MasterVolume);
        }

        if (cachedCollider != null && cachedCollider.enabled)
        {
            cachedCollider.enabled = false;
            StartCoroutine(RestoreCollider());
        }
    }

    System.Collections.IEnumerator RestoreCollider()
    {
        yield return new WaitForSeconds(1.5f);

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
            consumedThisContact = false;
        }
    }
}
