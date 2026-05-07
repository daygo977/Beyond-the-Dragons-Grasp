using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to pick up key";

    public string GetPromptText()
    {
        return promptText;
    }

    public void Interact()
    {
        if (GameFlags.Instance != null)
        {
            GameFlags.Instance.hasDoorKey = true;
        }

        gameObject.SetActive(false);
    }
}