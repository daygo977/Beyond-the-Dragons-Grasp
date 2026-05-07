using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class DoorInteractable : NetworkBehaviour, IInteractable
{
    [Header("Key Pairing")]
    public string requiredKeyId = "DefaultKey";

    [Header("Prompt Text")]
    [TextArea]
    public string defaultPrompt = "Press E to open";
    [TextArea]
    public string unlockedPrompt = "Use key to open door";
    [TextArea]
    public string failPrompt = "You do not have the key.";

    [Header("Settings")]
    public float failMessageDuration = 3f;
    public bool requiresKey = true;

    [Header("Door Movement")]
    public Transform doorToMove;
    public float moveUpAmount = 2f;
    public float moveSpeed = 2f;

    [Header("Lock Object")]
    public GameObject lockObject;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gateOpenSound;
    [Range(0f, 1f)] public float gateOpenVolume = 1f;

    private string temporaryPrompt = "";
    private Coroutine resetPromptRoutine;

    //Multiplayer edit, changed from bool to network variable bool
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool playedOpenSound;

    //Multiplayer edit, added private
    private void Start()
    {
        if (doorToMove == null)
            doorToMove = transform;

        if (lockObject == null)
            lockObject = gameObject;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        closedPosition = doorToMove.position;
        openPosition = closedPosition + Vector3.up * moveUpAmount;

        //Multiplayer edit,
        ApplyDoorVisualState();
    }
    
    //Multiplayer new function
    public override void OnNetworkSpawn()
    {
        isOpen.OnValueChanged += OnOpenStateChanged;
        ApplyDoorVisualState();
    }

    //Multiplayer new function
    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnOpenStateChanged;
    }

    //Multiplayer new function
    private void Update()
    {
        Vector3 target = isOpen.Value ? openPosition : closedPosition;

        if (doorToMove != null)
        {
            doorToMove.position = Vector3.MoveTowards(
                doorToMove.position,
                target,
                moveSpeed * Time.deltaTime
            );
        }
    }

    //Multiplayer new function
    private void OnOpenStateChanged(bool oldValue, bool newValue)
    {
        ApplyDoorVisualState();

        if (newValue && !playedOpenSound)
        {
            playedOpenSound = true;
            PlayOpenSoundLocal();
        }
    }

    public string GetPromptText()
    {
        if (!string.IsNullOrEmpty(temporaryPrompt))
            return temporaryPrompt;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

        //Multiplayer edit, basic logic
        if (isOpen.Value)
            return "";

        if (requiresKey && hasKey)
            return unlockedPrompt;

        return defaultPrompt;
    }

    public void Interact()
    {
        //Multiplayer edit,
        if (!IsServer) return;

        if (isOpen.Value) return;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

        if (!requiresKey || hasKey)
        {
            isOpen.Value = true;
        }
    }

    //Multiplayer new function
    public bool CanOpen()
    {
        if (!requiresKey)
            return true;

        return GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);
    }
    
    //Multiplayer new function
    public void ShowFailPromptLocal()
    {
        ShowTemporaryPrompt(failPrompt);
    }

    //Multiplayer new function
    private void ApplyDoorVisualState()
    {
        if (lockObject != null)
            lockObject.SetActive(!isOpen.Value);
    }

    //Multiplayer edit, added private
    private void ShowTemporaryPrompt(string message)
    {
        temporaryPrompt = message;

        if (resetPromptRoutine != null)
            StopCoroutine(resetPromptRoutine);

        resetPromptRoutine = StartCoroutine(ResetPromptAfterDelay());
    }

    //Multiplayer edit, added private
    private IEnumerator ResetPromptAfterDelay()
    {
        yield return new WaitForSeconds(failMessageDuration);
        temporaryPrompt = "";
        resetPromptRoutine = null;
    }

    //Multiplayer new function
    private void PlayOpenSoundLocal()
    {
        if (gateOpenSound == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(gateOpenSound, gateOpenVolume);
        else if (doorToMove != null)
            AudioSource.PlayClipAtPoint(gateOpenSound, doorToMove.position, gateOpenVolume);
    }
}