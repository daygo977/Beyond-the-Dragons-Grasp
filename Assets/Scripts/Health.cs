using System;
using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    //Multiplayer edit, changed int to network variable 
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    //Multiplayer edit, bool to network variable bool
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Death")]
    public bool destroyOnDeath = false;
    public bool disableOnDeath = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] hurtClips;
    [Range(0f, 1f)] public float hurtVolume = 1f;

    //Multiplayer edit, other classes will use CurrentHealth and IsDead to read info on new field changes
    public int CurrentHealth => currentHealth.Value;
    public bool IsDead => isDead.Value;

    public event Action OnDied;
    public event Action<int> OnDamaged;

    //Multiplayer new field
    private bool localDeathEventFired;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    //Multiplayer new function
    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeathStateChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }

        localDeathEventFired = isDead.Value;

        if (isDead.Value)
            OnDied?.Invoke();
    }

    //Multiplayer new function
    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    //Multiplayer new function
    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (newHealth < oldHealth)
        {
            int damageAmount = oldHealth - newHealth;
            OnDamaged?.Invoke(damageAmount);

            if (newHealth > 0)
                PlayRandomHurtSound();
        }
    }

    //Multiplayer new function
    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
        {
            localDeathEventFired = false;
            return;
        }

        if (localDeathEventFired)
            return;

        localDeathEventFired = true;
        OnDied?.Invoke();

        if (disableOnDeath)
            ApplyDisabledVisuals();
    }

    //Multiplayer edit, new logic
    public void TakeDamage(int amount)
    {
        if (!IsServer) return;
        if (isDead.Value) return;
        if (amount <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);

        if (currentHealth.Value <= 0)
            Die();
    }

    //Multiplayer new function
    public void RequestTakeDamage(int amount)
    {
        if (IsServer)
        {
            TakeDamage(amount);
            return;
        }

        RequestTakeDamageServerRpc(amount);
    }

    //Multiplayer new ServerRpc function
    [ServerRpc] 
    private void RequestTakeDamageServerRpc(int amount)
    {
        TakeDamage(amount);
    }

    //Multiplayer edit, new logic
    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (isDead.Value) return;
        if (amount <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
    }

    //Multiplayer new function
    public void RequestHeal(int amount)
    {
        if (IsServer)
        {
            Heal(amount);
            return;
        }

        RequestHealServerRpc(amount);
    }

    //Multiplayer new ServerRpc function
    [ServerRpc]
    private void RequestHealServerRpc(int amount)
    {
        Heal(amount);
    }

    //Multiplayer edit, new logic
    private void Die()
    {
        if (!IsServer) return;
        if (isDead.Value) return;

        currentHealth.Value = 0;
        isDead.Value = true;

        if (destroyOnDeath)
        {
            NetworkObject.Despawn(true);
        }
    }

    //Multiplayer new function
    private void ApplyDisabledVisuals()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = false;
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