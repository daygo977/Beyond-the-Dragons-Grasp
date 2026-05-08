using UnityEngine;
using Unity.Netcode;

public class HealthPickup : NetworkBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to consume";
    

    [Header("Healing")]
    public int healAmount = 30;
    public bool consumeIfPlayerIsFullHealth = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip healSound;
    [Range(0f, 1f)] public float healVolume = 1f;

    //Multiplayer bool to network variable bool
    private NetworkVariable<bool> isAvailable = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> consumed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        //Multiplayer edit, new logic
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ApplyVisualState();
    }

    //Multiplayer new function
    public override void OnNetworkSpawn()
    {
        isAvailable.OnValueChanged += OnPickupStateChanged;
        consumed.OnValueChanged += OnPickupStateChanged;

        ApplyVisualState();
    }

    //Multiplayer new function
    public override void OnNetworkDespawn()
    {
        isAvailable.OnValueChanged -= OnPickupStateChanged;
        consumed.OnValueChanged -= OnPickupStateChanged;
    }
    
    //Multiplayer new function
    private void OnPickupStateChanged(bool oldValue, bool newValue)
    {
        ApplyVisualState();
    }

    public string GetPromptText()
    {
        //Multiplayer edit, new logic
        if (!isAvailable.Value || consumed.Value)
            return "";

        return promptText;
    }

    public void Interact()
    {
        Debug.LogWarning($"{name}: HealthPickup.Interact() was called without a client id. Use Interact(ulong clientId) from PlayerController.");
    }

    //Multiplayer new function
    public void Interact(ulong clientId)
    {
        if (!IsServer) return;
        if (!isAvailable.Value) return;
        if (consumed.Value) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            return;

        if (client.PlayerObject == null)
            return;

        Health playerHealth = client.PlayerObject.GetComponent<Health>();

        if (playerHealth == null)
            return;

        if (!consumeIfPlayerIsFullHealth && playerHealth.CurrentHealth >= playerHealth.maxHealth)
            return;

        playerHealth.Heal(healAmount);

        consumed.Value = true;
        isAvailable.Value = false;

        PlayHealSoundClientRpc(transform.position);
    }

    //Multiplayer new function
    public void SetAvailable(bool available)
    {
        if (!IsServer) return;

        if (consumed.Value)
        {
            isAvailable.Value = false;
            return;
        }

        isAvailable.Value = available;
    }

    //Multiplayer new function, ClientRpc
    [ClientRpc]
    private void PlayHealSoundClientRpc(Vector3 soundPosition)
    {
        if (healSound == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(healSound, healVolume);
        else
            AudioSource.PlayClipAtPoint(healSound, soundPosition, healVolume);
    }

    //Multiplayer new function
    private void ApplyVisualState()
    {
        bool visible = isAvailable.Value && !consumed.Value;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            renderer.enabled = visible;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = visible;
    }
}