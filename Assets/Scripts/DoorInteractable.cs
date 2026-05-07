using System.Collections;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
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

        closedPosition = doorToMove.position;
        openPosition = closedPosition + Vector3.up * moveUpAmount;
    }

    public string GetPromptText()
    {
        if (!string.IsNullOrEmpty(temporaryPrompt))
            return temporaryPrompt;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.hasDoorKey;

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

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.hasDoorKey;

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
}