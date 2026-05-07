using UnityEngine;

public class HealthPickup : MonoBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to consume";

    [Header("Healing")]
    public int healAmount = 30;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip healSound;
    [Range(0f, 1f)] public float healVolume = 1f;

    private bool consumed;

    public string GetPromptText()
    {
        if (consumed)
            return "";

        return promptText;
    }

    public void Interact()
    {
        if (consumed) return;

        Health playerHealth = Object.FindFirstObjectByType<Health>();

        if (playerHealth == null)
            return;

        consumed = true;

        playerHealth.Heal(healAmount);

        if (healSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(healSound, healVolume);
            else
                AudioSource.PlayClipAtPoint(healSound, transform.position, healVolume);
        }

        gameObject.SetActive(false);
    }
}