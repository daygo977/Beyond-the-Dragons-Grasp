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
        if (hasHit)
            return;

        // Only the server/host applies real damage.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        Health playerHealth = other.GetComponent<Health>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<Health>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead)
            return;

        hasHit = true;

        int finalDamage = sourceEnemy != null ? sourceEnemy.attackDamage : damage;

        playerHealth.TakeDamage(finalDamage);

        Debug.Log($"{name} damaged {playerHealth.name} for {finalDamage}");
    }
}