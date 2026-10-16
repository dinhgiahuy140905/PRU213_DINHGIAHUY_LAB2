using UnityEngine;

/// <summary>
/// Visuals and Aesthetics - simple parallax for background props (clouds).
///
/// Attach it to a background sprite and give it a "depth": 0 keeps the prop locked to
/// the world (no parallax), 1 glues it to the camera so it looks infinitely far away.
/// </summary>
public class Parallax : MonoBehaviour
{
    [SerializeField] Transform targetCamera;

    [Range(0f, 1f)]
    [SerializeField] float depth = 0.6f;

    Vector3 startPosition;
    float startCameraX;

    void Start()
    {
        if (targetCamera == null)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                targetCamera = camera.transform;
            }
        }

        startPosition = transform.position;
        startCameraX = targetCamera != null ? targetCamera.position.x : 0f;
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        float delta = targetCamera.position.x - startCameraX;
        transform.position = startPosition + new Vector3(delta * depth, 0f, 0f);
    }
}
