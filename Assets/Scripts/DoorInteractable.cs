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
    public bool disableLockAfterDoorOpens = true;
    public float doorOpenDistanceThreshold = 1.95f;
    public float lockHideDelayAfterOpeningStarts = 0.15f;
    private Coroutine hideLockRoutine;

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
        if (newValue)
        {
            if (!playedOpenSound)
            {
                playedOpenSound = true;
                PlayOpenSoundLocal();
            }

            if (hideLockRoutine != null)
                StopCoroutine(hideLockRoutine);

            hideLockRoutine = StartCoroutine(HideLockAfterDoorOpens());
        }
        else
        {
            ShowLockObject();
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
        if (isOpen.Value)
        {
            if (hideLockRoutine != null)
                StopCoroutine(hideLockRoutine);

            hideLockRoutine = StartCoroutine(HideLockAfterDoorOpens());
        }
        else
        {
            ShowLockObject();
        }
    }

    //Multiplayer new function
    private IEnumerator HideLockAfterDoorOpens()
    {
        if (!disableLockAfterDoorOpens)
            yield break;

        if (lockObject == null)
            yield break;

        // Wait one frame so the door has actually started opening.
        yield return null;

        if (lockHideDelayAfterOpeningStarts > 0f)
            yield return new WaitForSeconds(lockHideDelayAfterOpeningStarts);

        HideLockObject();
        hideLockRoutine = null;
    }

    //Multiplayer new function
    private void ShowLockObject()
    {
        if (lockObject == null)
            return;

        SetLockVisible(true);
    }

    //Multiplayer new function
    private void HideLockObject()
    {
        if (lockObject == null)
            return;

        SetLockVisible(false);
    }

    //Multiplayer new function
    private void SetLockVisible(bool visible)
    {
        if (lockObject == null)
            return;

        // If the lock object is the same object that owns DoorInteractable,
        // do NOT SetActive(false), because that disables this script.
        // Instead, hide renderers and disable colliders.
        if (lockObject == gameObject)
        {
            SetRenderersAndColliders(lockObject, visible);
            return;
        }

        // If the lock is a separate child/object, this is safe.
        lockObject.SetActive(visible);
    }

    //Multiplayer new function
    private void SetRenderersAndColliders(GameObject target, bool visible)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (doorToMove != null && renderer.transform.IsChildOf(doorToMove))
                continue;

            renderer.enabled = visible;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            if (doorToMove != null && collider.transform.IsChildOf(doorToMove))
                continue;

            collider.enabled = visible;
        }
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