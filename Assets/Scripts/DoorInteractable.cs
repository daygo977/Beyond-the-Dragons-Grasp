using System.Collections;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
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
    private bool isOpen = false;
    private bool isMoving = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        if (doorToMove == null)
            doorToMove = transform;

        if (lockObject == null)
            lockObject = gameObject;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        closedPosition = doorToMove.position;
        openPosition = closedPosition + Vector3.up * moveUpAmount;
    }

    public string GetPromptText()
    {
        if (!string.IsNullOrEmpty(temporaryPrompt))
            return temporaryPrompt;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

        if (requiresKey && hasKey && !isOpen)
            return unlockedPrompt;

        if (isOpen)
            return "";

        return defaultPrompt;
    }

    public void Interact()
    {
        if (isOpen || isMoving)
            return;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(requiredKeyId);

        if (!requiresKey || hasKey)
        {
            StartCoroutine(OpenDoor());
        }
        else
        {
            ShowTemporaryPrompt(failPrompt);
        }
    }

    IEnumerator OpenDoor()
    {
        isMoving = true;

        if (lockObject != null)
            HideLockObject();

        if (gateOpenSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(gateOpenSound, gateOpenVolume);
            else
                AudioSource.PlayClipAtPoint(gateOpenSound, doorToMove.position, gateOpenVolume);
        }

        while (Vector3.Distance(doorToMove.position, openPosition) > 0.01f)
        {
            doorToMove.position = Vector3.MoveTowards(
                doorToMove.position,
                openPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        doorToMove.position = openPosition;
        isOpen = true;
        isMoving = false;
    }

    void ShowTemporaryPrompt(string message)
    {
        temporaryPrompt = message;

        if (resetPromptRoutine != null)
            StopCoroutine(resetPromptRoutine);

        resetPromptRoutine = StartCoroutine(ResetPromptAfterDelay());
    }

    IEnumerator ResetPromptAfterDelay()
    {
        yield return new WaitForSeconds(failMessageDuration);
        temporaryPrompt = "";
        resetPromptRoutine = null;
    }

    void HideLockObject()
    {
        GameObject targetLock = lockObject != null ? lockObject : gameObject;

        Renderer[] renderers = targetLock.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
            rend.enabled = false;

        Collider[] colliders = targetLock.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.enabled = false;
    }
}