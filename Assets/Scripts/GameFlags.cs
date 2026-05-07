using System.Collections.Generic;
using UnityEngine;

public class GameFlags : MonoBehaviour
{
    public static GameFlags Instance;

    [Header("Global Flags")]
    public List<string> collectedKeys = new List<string>();

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

    public void AddKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return;

        if (!collectedKeys.Contains(keyId))
            collectedKeys.Add(keyId);
    }

    public bool HasKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return false;

        return collectedKeys.Contains(keyId);
    }
}