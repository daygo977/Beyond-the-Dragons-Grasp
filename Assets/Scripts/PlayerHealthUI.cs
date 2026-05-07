using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerHealthUI : MonoBehaviour
{
    //Player health will be auto found (with FindLocalPlayerHealth()) and assigned to this field
    public Health playerHealth;
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    void Start()
    {
        FindLocalPlayerHealth();
        UpdateUI();
    }

    void Update()
    {
        if (playerHealth == null)
            FindLocalPlayerHealth();
        UpdateUI();
    }

    private void FindLocalPlayerHealth()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.LocalClient == null)
            return;

        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        playerHealth = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Health>();
    }

    void UpdateUI()
    {
        if (playerHealth == null) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            healthSlider.value = playerHealth.CurrentHealth;
        }

        if (healthText != null)
        {
            healthText.text = playerHealth.CurrentHealth + " / " + playerHealth.maxHealth;
        }
    }
}