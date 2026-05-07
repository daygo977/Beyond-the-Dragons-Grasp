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

    [Header("Pickup")]
    public float disableDelay = 1f;

    private NetworkVariable<bool> pickedUp = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool pickedUpLocal;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        pickedUp.OnValueChanged += OnPickedUpChanged;
        ApplyPickupVisualState();
    }

    public override void OnNetworkDespawn()
    {
        pickedUp.OnValueChanged -= OnPickedUpChanged;
    }

    private void OnPickedUpChanged(bool oldValue, bool newValue)
    {
        ApplyPickupVisualState();

        if (newValue)
            PlayPickupSoundLocal();
    }

    public string GetPromptText()
    {
        bool isPickedUp = IsSpawned ? pickedUp.Value : pickedUpLocal;

        if (isPickedUp)
            return "";

        return promptText;
    }

    public void Interact()
    {
        if (!IsSpawned)
        {
            if (pickedUpLocal) return;

            pickedUpLocal = true;

            if (GameFlags.Instance != null)
                GameFlags.Instance.AddKey(keyId);

            ApplyPickupVisualStateLocal(false);
            PlayPickupSoundLocal();
            return;
        }

        if (!IsServer) return;
        if (pickedUp.Value) return;

        pickedUp.Value = true;

        if (GameFlags.Instance != null)
            GameFlags.Instance.AddKey(keyId);
    }

    private void ApplyPickupVisualState()
    {
        bool visible = !pickedUp.Value;
        ApplyPickupVisualStateLocal(visible);
    }

    private void ApplyPickupVisualStateLocal(bool visible)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
            col.enabled = visible;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in renderers)
            rend.enabled = visible;
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