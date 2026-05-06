using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

/// <summary>
/// Initializes Unity Services once for the whole game and signs the player in anonymously.
/// Relay needs this before host/client Relay connection can work.
/// </summary>
public class UnityServicesBootstrap : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    private static UnityServicesBootstrap instance;

    private async void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (IsInitialized)
            return;

        await InitializeServices();
    }

    private async System.Threading.Tasks.Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsInitialized = true;

            Debug.Log("Unity Services ready. Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to initialize Unity Services: " + e.Message);
        }
    }
}