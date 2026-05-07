using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerHealthUI : MonoBehaviour
{
    public Health playerHealth;
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    
    //Multiplayer edit, new logic
    private void Start()
    {
        FindLocalPlayerHealth();
        UpdateUI();
    }
    
    //Multiplayer edit, new logic
    private void Update()
    {
        if (playerHealth == null)
            FindLocalPlayerHealth();

        UpdateUI();
    }

    //Multiplayer new function, 
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
            //Multiplayer edit
            healthSlider.value = playerHealth.CurrentHealth;
        }

        if (healthText != null)
        {
            healthText.text = playerHealth.CurrentHealth + " / " + playerHealth.maxHealth;
        }
    }
}