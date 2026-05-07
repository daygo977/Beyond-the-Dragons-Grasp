using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class GameFlags : NetworkBehaviour
{
    public static GameFlags Instance;

    [Header("Global Flags")]
    //Multiplayer edit, turned list to network list
    public NetworkList<FixedString64Bytes> collectedKeys;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //Multiplayer edit
            collectedKeys = new NetworkList<FixedString64Bytes>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //Multiplayer edit,
    public void AddKey(string keyId)
    {
        if (!IsServer) return;

        if (string.IsNullOrWhiteSpace(keyId)) 
            return;

        FixedString64Bytes fixedKeyId = keyId;

        if (!collectedKeys.Contains(fixedKeyId))
            collectedKeys.Add(fixedKeyId);
    }

    //Multiplayer edit,
    public bool HasKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return false;

        if (collectedKeys == null)
            return false;

        FixedString64Bytes fixedKeyId = keyId;
        return collectedKeys.Contains(fixedKeyId);
    }
}