using UnityEngine;

/// <summary>
/// Visuals and Aesthetics - snow spray under the board.
///
/// The emission rate and the size of the dust particles scale with the player's
/// speed, so the snow spray becomes a "speedometer" for the game feel.
/// </summary>
public class DustTrail : MonoBehaviour
{
    [SerializeField] ParticleSystem dustParticles;

    [Tooltip("Emission rate (particles / second) at maximum speed.")]
    [SerializeField] float maxEmissionRate = 30f;

    const float MinEmissionRate = 0f;

    PlayerController controller;
    ParticleSystem.EmissionModule emission;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (dustParticles != null)
        {
            emission = dustParticles.emission;
        }
    }

    void Update()
    {
        if (dustParticles == null || controller == null)
        {
            return;
        }

        // Normalised speed 0..1 against the game's top speed.
        float t = Mathf.Clamp01(controller.CurrentSpeed / Mathf.Max(1f, controller.MaxSpeed));

        emission.rateOverTime = Mathf.Lerp(MinEmissionRate, maxEmissionRate, t);

        // Only emit while actually moving.
        if (t < 0.05f && dustParticles.isPlaying)
        {
            dustParticles.Stop();
        }
        else if (t >= 0.05f && !dustParticles.isPlaying)
        {
            dustParticles.Play();
        }
    }
}
