using UnityEngine;
using Unity.Netcode;

public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Fallback Damage")]
    public int damage = 10;

    [Header("Source Enemy")]
    public EnemyController sourceEnemy;

    private bool hasHit;

    private void OnEnable()
    {
        hasHit = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        Health playerHealth = other.GetComponent<Health>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<Health>();

        if (playerHealth == null)
            return;

        hasHit = true;

        int finalDamage = sourceEnemy != null ? sourceEnemy.attackDamage : damage;

        playerHealth.TakeDamage(finalDamage);
    }
}