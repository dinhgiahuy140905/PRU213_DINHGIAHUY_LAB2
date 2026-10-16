using System.Collections;
using UnityEngine;

/// <summary>
/// Obstacle Course and Challenges - reaching the finish line.
///
/// The finish line is a trigger. When the player reaches it the finish effect and
/// sound play, a large time/bonus is awarded and the level is completed after a
/// short delay (so the effect can be appreciated).
/// </summary>
public class FinishLine : MonoBehaviour
{
    [SerializeField] float loadDelay = 1f;
    [SerializeField] ParticleSystem finishEffect;

    [Header("Reward")]
    [SerializeField] int finishBonus = 500;

    bool triggered;
    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        triggered = true;
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.DisableControls();
        }

        if (finishEffect != null)
        {
            finishEffect.Play();
        }

        if (audioSource != null)
        {
            audioSource.Play();
        }

        ScoreManager.Instance?.AwardFinishBonus(finishBonus);

        yield return new WaitForSeconds(loadDelay);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LevelCompleted();
        }
    }
}
