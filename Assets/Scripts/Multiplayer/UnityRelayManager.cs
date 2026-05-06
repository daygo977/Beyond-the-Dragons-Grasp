using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using UnityEngine;

/// <summary>
/// Handles Relay host/client connection without Lobby (yet).
/// Host creates a Relay allocation and join code.
/// Client enters join code and joins that Relay allocation.
/// </summary>
public class UnityRelayManager : MonoBehaviour
{
    public static UnityRelayManager Instance { get; private set; }

    [Header("Relay Settings")]
    [SerializeField] private int maxConnections = 3;
    [SerializeField] private string connectionType = "dtls";
    [SerializeField] private bool useWebSockets = false;

    public string CurrentJoinCode { get; private set; } = "";

    private UnityTransport unityTransport;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("UnityRelayManager: NetworkManager.Singleton not found.");
            return;
        }

        unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (unityTransport == null)
        {
            Debug.LogError("UnityRelayManager: UnityTransport not found on NetworkManager.");
        }
    }

    public async Task<string> StartHostWithRelay()
    {
        if (!await WaitForUnityServices())
            return null;

        if (!ValidateNetworkSetup())
            return null;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            unityTransport.UseWebSockets = useWebSockets;
            unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            bool started = NetworkManager.Singleton.StartHost();

            if (!started)
            {
                Debug.LogError("Failed to start Relay host.");
                return null;
            }

            CurrentJoinCode = joinCode;

            Debug.Log("Relay Host started. Join Code: " + joinCode);
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError("StartHostWithRelay failed: " + e.Message);
            return null;
        }
    }

    public async Task<bool> StartClientWithRelay(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Join code is empty.");
            return false;
        }

        if (!await WaitForUnityServices())
            return false;

        if (!ValidateNetworkSetup())
            return false;

        try
        {
            joinCode = joinCode.Trim().ToUpper();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            unityTransport.UseWebSockets = useWebSockets;
            unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

            bool started = NetworkManager.Singleton.StartClient();

            if (!started)
            {
                Debug.LogError("Failed to start Relay client.");
                return false;
            }

            CurrentJoinCode = joinCode;

            Debug.Log("Relay Client started with Join Code: " + joinCode);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("StartClientWithRelay failed: " + e.Message);
            return false;
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        CurrentJoinCode = "";
    }

    private async Task<bool> WaitForUnityServices()
    {
        try
        {
            while (!UnityServicesBootstrap.IsInitialized)
                await Task.Yield();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services not ready: " + e.Message);
            return false;
        }
    }

    private bool ValidateNetworkSetup()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is missing.");
            return false;
        }

        if (unityTransport == null)
            unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (unityTransport == null)
        {
            Debug.LogError("UnityTransport is missing from NetworkManager.");
            return false;
        }

        return true;
    }
}