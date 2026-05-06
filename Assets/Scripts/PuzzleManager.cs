using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("Correct Order")]
    public PuzzleSymbol[] correctOrder =
    {
        PuzzleSymbol.Sword,
        PuzzleSymbol.Shield,
        PuzzleSymbol.Dragon
    };

    [Header("Reward")]
    public GameObject keyReward;

    [Header("Optional Objects")]
    public GameObject solvedEffect;

    private int currentStep = 0;
    private bool puzzleSolved = false;

    public bool IsSolved()
    {
        return puzzleSolved;
    }

    public void ActivateLever(PuzzleSymbol symbol)
    {
        if (puzzleSolved)
            return;

        if (symbol == correctOrder[currentStep])
        {
            Debug.Log(symbol + " was correct.");

            currentStep++;

            if (currentStep >= correctOrder.Length)
            {
                SolvePuzzle();
            }
        }
        else
        {
            Debug.Log(symbol + " was wrong. Puzzle reset.");
            ResetPuzzle();
        }
    }

    private void SolvePuzzle()
    {
        puzzleSolved = true;

        if (keyReward != null)
        {
            keyReward.SetActive(true);
        }

        if (solvedEffect != null)
        {
            solvedEffect.SetActive(true);
        }

        Debug.Log("Puzzle solved. Boss key spawned.");
    }

    private void ResetPuzzle()
    {
        currentStep = 0;
    }
}

public enum PuzzleSymbol
{
    Sword,
    Shield,
    Dragon
}