using UnityEngine;
using Unity.Netcode;

public class KeyPickup : NetworkBehaviour, IInteractable
{
    [Header("Key")]
    public string keyId = "DefaultKey";

    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to pick up key";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    private NetworkVariable<bool> pickedUp = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isAvailable = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        pickedUp.OnValueChanged += OnStateChanged;
        isAvailable.OnValueChanged += OnStateChanged;

        ApplyVisualState();
    }

    public override void OnNetworkDespawn()
    {
        pickedUp.OnValueChanged -= OnStateChanged;
        isAvailable.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(bool oldValue, bool newValue)
    {
        ApplyVisualState();

        if (pickedUp.Value)
            PlayPickupSoundLocal();
    }

    public string GetPromptText()
    {
        if (!isAvailable.Value || pickedUp.Value)
            return "";

        return promptText;
    }

    public void Interact()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        if (!isAvailable.Value || pickedUp.Value)
            return;

        pickedUp.Value = true;
        isAvailable.Value = false;

        if (GameFlags.Instance != null)
            GameFlags.Instance.AddKey(keyId);
    }

    public void SetAvailable(bool available)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        if (pickedUp.Value)
        {
            isAvailable.Value = false;
            return;
        }

        isAvailable.Value = available;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        bool visible = isAvailable.Value && !pickedUp.Value;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
            rend.enabled = visible;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = visible;
    }

    private void PlayPickupSoundLocal()
    {
        if (pickupSound == null)
            return;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.PlayOneShot(pickupSound, pickupVolume);
        else
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
    }
}