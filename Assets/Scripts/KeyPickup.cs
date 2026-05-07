using System.Collections;
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

    //Multiplayer edit, changed bool variable to network variable bool
    private NetworkVariable<bool> pickedUp = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    //Multiplayer new line
    private Coroutine pickupRoutine;

    //New multiplayer function
    public override void OnNetworkSpawn()
    {
        pickedUp.OnValueChanged += OnPickedUpChanged;
        ApplyPickupVisualState();
    }

    //New multiplayer function
    public override void OnNetworkDespawn()
    {
        pickedUp.OnValueChanged -= OnPickedUpChanged;
    }

    //New multiplayer function
    private void OnPickedUpChanged(bool oldValue, bool newValue)
    {
        ApplyPickupVisualState();

        if (newValue)
            PlayPickupSoundLocal();
    }

    public string GetPromptText()
    {
        //Multiplayer edit, added .Value
        if (pickedUp.Value)
            return "";

        return promptText;
    }

    //Multiplayer edit,
    public void Interact()
    {
        if (!IsServer) return;

        if (pickedUp.Value) return;

        pickedUp.Value = true;

        if (GameFlags.Instance != null)
            GameFlags.Instance.AddKey(keyId);
    }

    //Multiplayer new function
    private void ApplyPickupVisualState()
    {
        bool visible = !pickedUp.Value;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = visible;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
            rend.enabled = visible;
    }

    //Multiplayer new function
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

    //Note: gameObject.SetActive(false) is not called for keys, usually better to hide colliders and renderers
    //      to keep object alive.
}