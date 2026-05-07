using UnityEngine;
using Unity.Netcode;

public class EnemyHealth : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    //Multiplayer edit: int to network variable int
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private EnemyController enemyController;
    //Multiplayer edit: int to network variable int
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int CurrentHealth => currentHealth.Value;
    public bool IsDead => isDead.Value;

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
    }

    //Multiplayer new function
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }
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

    //Multiplayer new function
    [ServerRpc(RequireOwnership = false)]
    private void RequestTakeDamageServerRpc(int amount)
    {
        TakeDamage(amount);
    }

    //Multiplayer new function
    public void TakeDamage(int amount)
    {
        if (!IsServer) return;
        if (isDead.Value) return;
        if (amount <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);

        if (currentHealth.Value <= 0)
        {
            isDead.Value = true;

            if (enemyController != null)
                enemyController.Die();
        }
        else
        {
            if (enemyController != null)
                enemyController.TakeDamageReaction();
        }
    }
}