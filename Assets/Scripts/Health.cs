using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death")]
    public bool destroyOnDeath = false;
    public bool disableOnDeath = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] hurtClips;
    [Range(0f, 1f)] public float hurtVolume = 1f;

    public bool IsDead { get; private set; }

    public event Action OnDied;
    public event Action<int> OnDamaged;

    void Awake()
    {
        currentHealth = maxHealth;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnDamaged?.Invoke(amount);

        if (currentHealth > 0)
            PlayRandomHurtSound();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void Die()
    {
        if (IsDead) return;

        IsDead = true;
        currentHealth = 0;

        OnDied?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
        else if (disableOnDeath)
            gameObject.SetActive(false);
    }

    void PlayRandomHurtSound()
    {
        if (audioSource == null) return;
        if (hurtClips == null || hurtClips.Length == 0) return;

        AudioClip clip = hurtClips[UnityEngine.Random.Range(0, hurtClips.Length)];

        if (clip == null) return;

        audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip, hurtVolume);
    }
}