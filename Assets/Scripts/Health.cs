using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death")]
    public bool destroyOnDeath = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] hurtClips;
    [Range(0f, 1f)] public float hurtVolume = 1f;

    void Awake()
    {
        currentHealth = maxHealth;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth > 0)
            PlayRandomHurtSound();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void Die()
    {
        if (destroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    void PlayRandomHurtSound()
    {
        if (audioSource == null) return;
        if (hurtClips == null || hurtClips.Length == 0) return;

        AudioClip clip = hurtClips[Random.Range(0, hurtClips.Length)];

        if (clip == null) return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip, hurtVolume);
    }
}