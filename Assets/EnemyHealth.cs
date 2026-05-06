using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    private EnemyController enemyController;
    private bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
        enemyController = GetComponent<EnemyController>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            isDead = true;

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