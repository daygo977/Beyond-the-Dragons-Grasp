using UnityEngine;

public class PuzzleLever : MonoBehaviour, IInteractable
{
    [Header("Puzzle")]
    public PuzzleManager puzzleManager;
    public PuzzleSymbol leverSymbol;

    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to pull lever";

    [TextArea]
    public string solvedText = "The mechanism is already solved.";

    [Header("Optional Visual Movement")]
    public Transform leverHandle;
    public Vector3 pulledRotation = new Vector3(-45f, 0f, 0f);

    private bool hasBeenPulled = false;
    private Quaternion originalRotation;

    void Start()
    {
        if (leverHandle != null)
        {
            originalRotation = leverHandle.localRotation;
        }
    }

    public string GetPromptText()
    {
        if (puzzleManager != null && puzzleManager.IsSolved())
            return solvedText;

        return promptText;
    }

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogWarning("PuzzleLever is missing a PuzzleManager reference.");
            return;
        }

        puzzleManager.ActivateLever(leverSymbol);
        PullLeverVisual();
    }

    private void PullLeverVisual()
    {
        if (leverHandle == null)
            return;

        if (!hasBeenPulled)
        {
            leverHandle.localRotation = originalRotation * Quaternion.Euler(pulledRotation);
            hasBeenPulled = true;
        }
        else
        {
            leverHandle.localRotation = originalRotation;
            hasBeenPulled = false;
        }
    }
}