using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    public Health playerHealth;
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    void Start()
    {
        if (playerHealth != null && healthSlider != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            healthSlider.value = playerHealth.currentHealth;
        }

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerHealth == null) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            healthSlider.value = playerHealth.currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = playerHealth.currentHealth + " / " + playerHealth.maxHealth;
        }
    }
}