using UnityEngine;
using Unity.Netcode;

public class GameFlags : NetworkBehaviour
{
    public static GameFlags Instance;

    [Header("Global Flags")]
    public NetworkVariable<bool> hasDoorKey = new NetworkVariable<bool>(false);

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

    public void SetHasDoorKey(bool value)
    {
        if (!IsServer) return;
        hasDoorKey.Value = value;
    }
}