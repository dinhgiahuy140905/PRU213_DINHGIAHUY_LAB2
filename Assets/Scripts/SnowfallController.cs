using UnityEngine;

/// <summary>
/// Visuals and Aesthetics - dynamic weather.
///
/// Keeps a snow particle system centred on the camera and, every few seconds,
/// changes the "weather": the snowfall density and the visibility (camera
/// background) smoothly fade between a clear day and a heavy snow storm.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SnowfallController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float depthOffset = 10f;

    [Header("Snowfall")]
    [SerializeField] float minEmissionRate = 15f;
    [SerializeField] float maxEmissionRate = 140f;

    [Header("Weather change")]
    [SerializeField] float minInterval = 6f;
    [SerializeField] float maxInterval = 14f;
    [SerializeField] float transitionSpeed = 0.6f;
    [SerializeField] Color clearSky = new Color(0.59f, 0.84f, 0.91f, 1f);
    [SerializeField] Color stormySky = new Color(0.82f, 0.86f, 0.9f, 1f);

    ParticleSystem snow;
    ParticleSystem.EmissionModule emission;
    float currentDensity = 0.5f;
    float targetDensity = 0.5f;
    float nextChange;

    void Awake()
    {
        snow = GetComponent<ParticleSystem>();
        emission = snow.emission;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        ScheduleNextChange();
    }

    void Update()
    {
        FollowCamera();

        currentDensity = Mathf.MoveTowards(currentDensity, targetDensity, transitionSpeed * Time.deltaTime);
        emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, currentDensity);

        if (targetCamera != null)
        {
            targetCamera.backgroundColor = Color.Lerp(clearSky, stormySky, currentDensity * 0.7f);
        }

        if (Time.time >= nextChange)
        {
            targetDensity = Random.value;
            ScheduleNextChange();
        }
    }

    void FollowCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 position = targetCamera.transform.position;
        transform.position = new Vector3(position.x, position.y + 4f, depthOffset);
    }

    void ScheduleNextChange()
    {
        nextChange = Time.time + Random.Range(minInterval, maxInterval);
    }
}
