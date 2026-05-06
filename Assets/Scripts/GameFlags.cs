using UnityEngine;

public class GameFlags : MonoBehaviour
{
    public static GameFlags Instance;

    [Header("Global Flags")]
    public bool hasDoorKey = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}