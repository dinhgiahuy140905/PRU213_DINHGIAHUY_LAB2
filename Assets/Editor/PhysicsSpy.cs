#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Diagnostic helper used by <see cref="Lab2SmokeTest"/>.
///
/// It logs any physics step in which the board's velocity changes abruptly, which is
/// how "the player was launched into orbit" bugs are tracked down.
/// </summary>
public class PhysicsSpy : MonoBehaviour
{
    [SerializeField] float spikeThreshold = 8f;

    Rigidbody2D body;
    Vector2 previous;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        previous = body != null ? body.linearVelocity : Vector2.zero;
    }

    void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        float delta = (velocity - previous).magnitude;

        if (delta > spikeThreshold)
        {
            Debug.Log($"[Spy] dv={delta:F1} v=({velocity.x:F1},{velocity.y:F1}) " +
                      $"w={body.angularVelocity:F0} pos=({transform.position.x:F1},{transform.position.y:F1}) " +
                      $"t={Time.timeSinceLevelLoad:F2}");
        }

        previous = velocity;
    }
}
#endif
