using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public enum LeverType
    {
        Sword,
        Shield,
        Beast,
        Wrong
    }

    [Header("Correct Order")]
    [SerializeField] private LeverType[] correctOrder =
    {
        LeverType.Sword,
        LeverType.Shield,
        LeverType.Beast
    };

    [Header("Reward")]
    [SerializeField] private GameObject keyToSpawn;

    [Header("Optional Reset")]
    [SerializeField] private PuzzleLever[] allLevers;

    private int currentStep = 0;
    private bool puzzleSolved = false;

    private void Start()
    {
        if (keyToSpawn != null)
        {
            keyToSpawn.SetActive(false);
        }
    }

    public void PullLever(LeverType leverType)
    {
        if (puzzleSolved)
        {
            return;
        }

        if (leverType == correctOrder[currentStep])
        {
            currentStep++;

            if (currentStep >= correctOrder.Length)
            {
                SolvePuzzle();
            }
        }
        else
        {
            ResetPuzzle();
        }
    }

    private void SolvePuzzle()
    {
        puzzleSolved = true;

        if (keyToSpawn != null)
        {
            keyToSpawn.SetActive(true);
        }

        Debug.Log("Puzzle solved! Key spawned.");
    }

    private void ResetPuzzle()
    {
        currentStep = 0;

        if (allLevers != null)
        {
            foreach (PuzzleLever lever in allLevers)
            {
                if (lever != null)
                {
                    lever.ResetLever();
                }
            }
        }

        Debug.Log("Wrong lever order. Puzzle reset.");
    }
}