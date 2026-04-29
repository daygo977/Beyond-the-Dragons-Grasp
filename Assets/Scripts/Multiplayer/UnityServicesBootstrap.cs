using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

/// <summary>
/// Initializes Unity Services once for the whole game and signs player in anonymously
/// </summary>
public class UnityServicesBootstrap : MonoBehaviour
{
    //Tracks to see if Unity Services have been initialized
    //Static, shared globally across scenes
    public static bool IsInitialized { get; private set; }
    private static UnityServicesBootstrap instance;

    //Gets called when game is initialized
    //Waits for un
    private async void Awake()
    {
        // one bootstrap only
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

    /// <summary>
    /// Calls unity server to see if active, if not signed in, then anonymous sign in (default)
    /// </summary>
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