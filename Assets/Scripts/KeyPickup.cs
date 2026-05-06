using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using NUnit.Framework;

public class KeyPickup : NetworkBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to pick up key";

    private NetworkVariable<bool> isCollected = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        UpdateVisual();
        isCollected.OnValueChanged += OnCollectedChanged;
    }

    public override void OnNetworkDespawn()
    {
        isCollected.OnValueChanged -= OnCollectedChanged;
    }

    void OnCollectedChanged(bool prev, bool current)
    {
        UpdateVisual();
    }

    void UpdateVisual()
    {
        gameObject.SetActive(!isCollected.Value);
    }

    public string GetPromptText()
    {
        if (isCollected.Value) return "";
        return promptText;
    }

    public void Interact()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (isCollected.Value) return;

        isCollected.Value = true;

        if (GameFlags.Instance != null)
        {
            GameFlags.Instance.SetHasDoorKey(true);
        }
    }
}