using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    public int damage = 10;

    private bool hasHit;

    void OnEnable()
    {
        hasHit = false;
    }

    void OnDisable()
    {
    }

    void OnTriggerEnter(Collider other)
    {

        if (hasHit) return;

        Health playerHealth = other.GetComponent<Health>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<Health>();

        if (playerHealth != null)
        {
            hasHit = true;
            playerHealth.TakeDamage(damage);
        }
    }
}