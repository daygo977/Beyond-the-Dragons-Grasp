using UnityEngine;
using Unity.Netcode;

public class PuzzleManager : NetworkBehaviour
{
    public enum LeverType
    {
        Sword,
        Shield,
        Beast,
        Wrong
    }

    [Header("Correct Order")]
    [SerializeField]
    private LeverType[] correctOrder =
    {
        LeverType.Sword,
        LeverType.Shield,
        LeverType.Beast
    };

    [Header("Reward")]
    [SerializeField] private GameObject keyToSpawn;

    [Header("Optional Reset")]
    [SerializeField] private PuzzleLever[] allLevers;

    private int currentStep;
    private bool puzzleSolved;

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        SetKeyVisible(false);
    }

    public void PullLever(LeverType leverType)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        if (puzzleSolved)
            return;

        Debug.Log($"PuzzleManager: received lever {leverType}. Current step={currentStep}");

        if (correctOrder == null || correctOrder.Length == 0)
        {
            Debug.LogWarning("PuzzleManager: Correct Order is empty.");
            return;
        }

        if (currentStep >= correctOrder.Length)
            return;

        if (leverType == correctOrder[currentStep])
        {
            currentStep++;

            Debug.Log($"PuzzleManager: correct lever. New step={currentStep}");

            if (currentStep >= correctOrder.Length)
                SolvePuzzle();

            return;
        }

        Debug.Log("PuzzleManager: wrong lever. Resetting.");
        ResetPuzzle();
    }

    private void SolvePuzzle()
    {
        puzzleSolved = true;

        Debug.Log("PuzzleManager: puzzle solved. Showing key.");

        SetKeyVisible(true);
    }

    private void ResetPuzzle()
    {
        currentStep = 0;

        if (allLevers != null)
        {
            foreach (PuzzleLever lever in allLevers)
            {
                if (lever != null)
                    lever.ResetLeverState();
            }
        }
    }

    private void SetKeyVisible(bool visible)
    {
        if (keyToSpawn == null)
        {
            Debug.LogWarning("PuzzleManager: Key To Spawn is missing.");
            return;
        }

        keyToSpawn.SetActive(true);

        KeyPickup keyPickup = keyToSpawn.GetComponent<KeyPickup>();

        if (keyPickup == null)
            keyPickup = keyToSpawn.GetComponentInChildren<KeyPickup>(true);

        if (keyPickup != null)
        {
            keyPickup.SetAvailable(visible);
            Debug.Log($"PuzzleManager: KeyPickup SetAvailable({visible}) called on {keyPickup.name}");
            return;
        }

        Renderer[] renderers = keyToSpawn.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
            rend.enabled = visible;

        Collider[] colliders = keyToSpawn.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = visible;

        Debug.Log($"PuzzleManager: raw key renderers/colliders set to {visible}");
    }
}