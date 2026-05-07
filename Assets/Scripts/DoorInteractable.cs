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
    public float lockHideDelayAfterOpeningStarts = 0.15f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gateOpenSound;
    [Range(0f, 1f)] public float gateOpenVolume = 1f;

    private string temporaryPrompt = "";
    private Coroutine resetPromptRoutine;
    private Coroutine hideLockRoutine;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isOpenLocal;
    private bool playedOpenSound;

    private Vector3 closedPosition;
    private Vector3 openPosition;

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

        ApplyDoorVisualState();
    }

    public override void OnNetworkSpawn()
    {
        isOpen.OnValueChanged += OnOpenStateChanged;
        ApplyDoorVisualState();
    }

    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnOpenStateChanged;
    }

    private void Update()
    {
        if (doorToMove == null)
            return;

        bool open = IsSpawned ? isOpen.Value : isOpenLocal;
        Vector3 target = open ? openPosition : closedPosition;

        doorToMove.position = Vector3.MoveTowards(
            doorToMove.position,
            target,
            moveSpeed * Time.deltaTime
        );
    }

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
            playedOpenSound = false;
            ShowLockObject();
        }
    }

    public string GetPromptText()
    {
        if (!string.IsNullOrEmpty(temporaryPrompt))
            return temporaryPrompt;

        bool open = IsSpawned ? isOpen.Value : isOpenLocal;

        if (open)
            return "";

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

        if (requiresKey && hasKey)
            return unlockedPrompt;

        return defaultPrompt;
    }

    public void Interact()
    {
        if (!IsSpawned)
        {
            if (isOpenLocal)
                return;

            bool hasKeyLocal = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

            if (!requiresKey || hasKeyLocal)
            {
                isOpenLocal = true;
                PlayOpenSoundLocal();

                if (hideLockRoutine != null)
                    StopCoroutine(hideLockRoutine);

                hideLockRoutine = StartCoroutine(HideLockAfterDoorOpens());
            }
            else
            {
                ShowFailPromptLocal();
            }

            return;
        }

        if (!IsServer) return;
        if (isOpen.Value) return;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

        if (!requiresKey || hasKey)
            isOpen.Value = true;
    }

    public bool CanOpen()
    {
        if (!requiresKey)
            return true;

        return GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);
    }

    public void ShowFailPromptLocal()
    {
        ShowTemporaryPrompt(failPrompt);
    }

    private void ApplyDoorVisualState()
    {
        bool open = IsSpawned ? isOpen.Value : isOpenLocal;

        if (open)
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

    private IEnumerator HideLockAfterDoorOpens()
    {
        if (!disableLockAfterDoorOpens)
            yield break;

        if (lockObject == null)
            yield break;

        yield return null;

        if (lockHideDelayAfterOpeningStarts > 0f)
            yield return new WaitForSeconds(lockHideDelayAfterOpeningStarts);

        HideLockObject();
        hideLockRoutine = null;
    }

    private void ShowLockObject()
    {
        if (lockObject == null)
            return;

        SetLockVisible(true);
    }

    private void HideLockObject()
    {
        if (lockObject == null)
            return;

        SetLockVisible(false);
    }

    private void SetLockVisible(bool visible)
    {
        if (lockObject == null)
            return;

        if (lockObject == gameObject)
        {
            SetRenderersAndColliders(lockObject, visible);
            return;
        }

        lockObject.SetActive(visible);
    }

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

    private void ShowTemporaryPrompt(string message)
    {
        temporaryPrompt = message;

        if (resetPromptRoutine != null)
            StopCoroutine(resetPromptRoutine);

        resetPromptRoutine = StartCoroutine(ResetPromptAfterDelay());
    }

    private IEnumerator ResetPromptAfterDelay()
    {
        yield return new WaitForSeconds(failMessageDuration);
        temporaryPrompt = "";
        resetPromptRoutine = null;
    }

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