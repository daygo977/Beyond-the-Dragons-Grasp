using UnityEngine;
using Unity.Netcode;

public class DoorInteractable : NetworkBehaviour, IInteractable
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
    private float failPromptUntil = 0f;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    void Start()
    {
        if (doorToMove == null)
            doorToMove = transform;

        closedPosition = doorToMove.position;
        openPosition = closedPosition + Vector3.up * moveUpAmount;
    }

    void Update()
    {
        Vector3 target = isOpen.Value ? openPosition : closedPosition;
        doorToMove.position = Vector3.MoveTowards(
            doorToMove.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Time.time > failPromptUntil)
        {
            temporaryPrompt = "";
        }
    }

    public string GetPromptText()
    {
        if (!string.IsNullOrEmpty(temporaryPrompt))
            return temporaryPrompt;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.hasDoorKey.Value;

        if (isOpen.Value)
            return "";

        if (requiresKey && hasKey)
            return unlockedPrompt;

        return defaultPrompt;
    }

    public void Interact()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (isOpen.Value) return;

        bool hasKey = GameFlags.Instance != null && GameFlags.Instance.hasDoorKey.Value;

        if (!requiresKey || hasKey)
        {
            isOpen.Value = true;
        }
    }

    public void ShowFailPromptLocal()
    {
        temporaryPrompt = failPrompt;
        failPromptUntil = Time.time + failMessageDuration;
    }
}