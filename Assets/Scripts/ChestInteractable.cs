using UnityEngine;
using Unity.Netcode;

public class ChestInteractable : NetworkBehaviour, IHoldInteractable
{
    [Header("Prompt Text")]
    [TextArea] public string closedPrompt = "Hold E to open chest";
    [TextArea] public string openedPrompt = "";

    [Header("Hold Settings")]
    public float holdDuration = 1.5f;

    [Header("Chest Models")]
    public GameObject closedChestModel;
    public GameObject openedChestModel;

    [Header("Reward")]
    public GameObject healthPickupObject;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip holdSound;
    public AudioClip[] openSounds;
    [Range(0f, 1f)] public float holdVolume = 0.6f;
    [Range(0f, 1f)] public float openVolume = 1f;

    private float holdTimer;
    private bool isHoldingSoundPlaying;

    //Multiplayer edit, changed bool to network variable bool
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    //Multiplayer edit, added private
    private void Start()
    {   
        //Multiplayer edit
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ApplyChestVisualState();
    }

    //Multiplayer new funciton
    public override void OnNetworkSpawn()
    {
        isOpen.OnValueChanged += OnOpenChanged;
        ApplyChestVisualState();
    }

    //Multiplayer new funciton
    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnOpenChanged;
    }

    //Multiplayer new funciton
    private void OnOpenChanged(bool oldValue, bool newValue)
    {
        ApplyChestVisualState();

        if (newValue)
        {
            StopHoldSound();
            PlayRandomOpenSound();
        }
    }

    public string GetPromptText()
    {
        //Multiplayer edit, added .Value
        if (isOpen.Value)
            return openedPrompt;

        return closedPrompt;
    }

    public void Interact()
    {
        // Chest uses hold interaction only.
    }

    public void HoldInteract(float deltaTime)
    {   
        //Multiplayer edit, added .Value
        if (isOpen.Value) return;

        holdTimer += deltaTime;
        PlayHoldSound();

        if (holdTimer >= holdDuration)
        {
            //Multiplayer edit, 
            holdTimer = holdDuration;
            StopHoldSound();
            RequestOpenChestServerRpc();
        }
    }

    //Multiplayer new ServerRpc (client to server) function
    [ServerRpc(RequireOwnership = false)]
    private void RequestOpenChestServerRpc()
    {
        if (isOpen.Value) return;

        isOpen.Value = true;
    }

    public void ResetHold()
    {
        //Multiplayer edit, added .Value
        if (isOpen.Value) return;

        holdTimer = 0f;
        StopHoldSound();
    }

    //Multiplayer new function
    private void ApplyChestVisualState()
    {
        if (closedChestModel != null)
            closedChestModel.SetActive(!isOpen.Value);

        if (openedChestModel != null)
            openedChestModel.SetActive(isOpen.Value);

        if (healthPickupObject != null)
        {
            HealthPickup healthPickup = healthPickupObject.GetComponent<HealthPickup>();

            if (healthPickup == null)
                healthPickup = healthPickupObject.GetComponentInChildren<HealthPickup>(true);

            if (healthPickup != null)
            {
                if (IsServer)
                    healthPickup.SetAvailable(isOpen.Value);
            }
            else
            {
                healthPickupObject.SetActive(isOpen.Value);
            }
        }
    }

    //Multiplayer new function
    private void PlayHoldSound()
    {
        if (audioSource == null) return;
        if (holdSound == null) return;
        if (isHoldingSoundPlaying) return;

        audioSource.clip = holdSound;
        audioSource.loop = true;
        audioSource.volume = holdVolume;
        audioSource.Play();

        isHoldingSoundPlaying = true;
    }

    //Multiplayer new function
    private void StopHoldSound()
    {
        if (audioSource == null) return;
        if (!isHoldingSoundPlaying) return;

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;

        isHoldingSoundPlaying = false;
    }

    //Multiplayer new function
    private void PlayRandomOpenSound()
    {
        if (openSounds == null || openSounds.Length == 0)
            return;

        AudioClip clip = openSounds[Random.Range(0, openSounds.Length)];

        if (clip == null)
            return;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, openVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, openVolume);
        }
    }
}