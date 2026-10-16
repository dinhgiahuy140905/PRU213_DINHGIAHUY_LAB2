using UnityEngine;

/// <summary>
/// Obstacle Course and Challenges - jump ramps.
///
/// A ramp is a trigger that launches the boarder into the air when ridden over.
/// Using a trigger (instead of a solid wedge) guarantees the player can never get
/// wedged against the raised end of the ramp when approaching it slowly.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Ramp : MonoBehaviour
{
    [Tooltip("Forward part of the launch impulse.")]
    [SerializeField] float launchForward = 5f;
    [Tooltip("Upward part of the launch impulse.")]
    [SerializeField] float launchUp = 11f;

    bool playerInside;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            playerInside = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only one launch per pass, otherwise a ramp the board lingers in would add
        // impulse after impulse.
        if (playerInside)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        playerInside = true;
        player.Launch(launchForward, launchUp);
    }
}
