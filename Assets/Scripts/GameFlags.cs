using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class GameFlags : NetworkBehaviour
{
    public static GameFlags Instance;

    [Header("Global Flags")]
    public NetworkList<FixedString64Bytes> collectedKeys;

    private List<string> localCollectedKeys = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            collectedKeys = new NetworkList<FixedString64Bytes>();
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

        if (!IsSpawned)
        {
            if (!localCollectedKeys.Contains(keyId))
                localCollectedKeys.Add(keyId);

            return;
        }

        if (!IsServer) return;

        FixedString64Bytes fixedKeyId = keyId;

        if (!collectedKeys.Contains(fixedKeyId))
            collectedKeys.Add(fixedKeyId);
    }

    public bool HasKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return false;

        if (!IsSpawned)
            return localCollectedKeys.Contains(keyId);

        if (collectedKeys == null)
            return false;

        FixedString64Bytes fixedKeyId = keyId;
        return collectedKeys.Contains(fixedKeyId);
    }
}