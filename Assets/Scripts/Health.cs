using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Death")]
    public bool destroyOnDeath = false;

    public int CurrentHealth => currentHealth.Value;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        if (currentHealth.Value <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (!IsServer) return;

        currentHealth.Value += amount;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);
    }

    void Die()
    {
        if (!IsServer) return;

        if (destroyOnDeath)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Debug.Log($"{gameObject.name} died.");
        }
    }
}