using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class UnityLobbyManager : MonoBehaviour
{
    public static UnityLobbyManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string lobbyBrowseSceneName = "LobbyBrowse";
    [SerializeField] private string lobbyRoomSceneName = "LobbyRoom";
    [SerializeField] private string gameSceneName = "TestPlayer";

    [Header("Lobby Settings")]
    [SerializeField] private int maxPlayers = 4;

    [Header("Timers")]
    //Heartbeat ping to lobby to prevent it from deactivating from inactivity
    [SerializeField] private float heartbeatTimeMax = 15f;
    //Poll refreshes lobby info for all players
    [SerializeField] private float lobbyPollTimeMax = 1.2f;

    public event Action<List<Lobby>> OnAvailableLobbiesChanged;
    public event Action<Lobby> OnCurrentLobbyChanged;
    public event Action OnLeftLobby;

    public List<Lobby> Lobbies { get; private set; } = new List<Lobby>();

    public Lobby CurrentLobby => joinedLobby;

    //Verifys if player is host, first checks to see if in lobby, then if player is signed in (should anonymous),
    //then checks if host id matches player id to see if player is host
    public bool IsHost
    {
        get
        {
            if (CurrentLobby == null) return false;
            if (!AuthenticationService.Instance.IsSignedIn) return false;
            return CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;
        }
    }

    private Lobby hostLobby;
    private Lobby joinedLobby;

    //Poll refreshes lobby info for all players
    private float lobbyPollTimer;
    //Heartbeat ping to lobby to prevent it from deactivating from inactivity
    private float heartbeatTimer;

    private const string countdownActiveKey = "CountdownActive";
    private const string countdownEndTimeKey = "CountdownEndTime";

    private bool hasStartedRelayClient;
    private bool hasStartedRelayHost;
    private const string relayJoinCodeKey = "RelayJoinCode";
    private const string gameStartingKey = "GameStarting";

    private void Awake()
    {
        //singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //Run heartbeat for host and poll joined lobby for LobbyRoom updates, not LobbyBrowse
    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPoll();
    }

    public async Task<bool> CreateLobby(string playerName, string lobbyName, string password)
    {
        //signed in check
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player not signed in");
            return false;
        }

        try
        {
            //Creates host player with input data
            Player player = new Player(
                id: AuthenticationService.Instance.PlayerId,
                //Display Name that is typed by player
                data: new Dictionary<string, PlayerDataObject>
                {
                    {
                        //Players in lobbies can see each othes names
                        "DisplayName",
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                    }
                }
            );

            //Lobby options, includes a public viewable lobby, potential password, player (who created lobby)
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Password = string.IsNullOrWhiteSpace(password) ? null : password,
                Player = player,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "HasPassword",
                        new DataObject(
                            DataObject.VisibilityOptions.Public,
                            string.IsNullOrWhiteSpace(password) ? "0" : "1"
                        )
                    },
                    {
                        countdownActiveKey,
                        new DataObject(DataObject.VisibilityOptions.Member, "0")
                    },
                    {
                        countdownEndTimeKey,
                        new DataObject(DataObject.VisibilityOptions.Member, "0")
                    },
                    {
                        gameStartingKey,
                        new DataObject(DataObject.VisibilityOptions.Member, "0")
                    },
                    {
                        relayJoinCodeKey,
                        new DataObject(DataObject.VisibilityOptions.Member, "")
                    }
                }
            };

            //Create lobby with previous variable above containing the input data provided by players
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            SetCurrentLobby(lobby);
            ResetTimers();

            //Checks to see if OnCurrentLo is null, if yes then throws null instead of exception, else it runs invoke
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);
            ShowLobbyRoomPanel();

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Create Lobby failed... " + e);
            return false;
        }
    }

    public async Task RefreshLobbies()
    {
        try
        {
            //Will show list of available lobbies which have more than 0 open slots. If lobby is full, will not show in list
            List<QueryFilter> filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            //Will show lobbies in order they were created in
            List<QueryOrder> order = new List<QueryOrder>
            {
                new QueryOrder(false, QueryOrder.FieldOptions.Created)
            };


            //Lobbies will show 25 at a time, with previous filters set, and order set above.
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = filters,
                Order = order
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            //If response.Results is null, use new List<Lobby>()
            Lobbies = response.Results ?? new List<Lobby>();

            //Checks to see if OnAvai is null, if yes then throws null instead of exception, else it runs invoke
            OnAvailableLobbiesChanged?.Invoke(Lobbies);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("RefreshLobbies failed: " + e);
        }
    }

    public async Task<bool> JoinLobby(string lobbyId, string playerName, string password = null)
    {
        //signed in check
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player not signed in");
            return false;
        }

        try
        {
            //Same lines present in CreateLobby()
            Player player = new Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject>
                {
                    {
                        //Players in lobbies can see each othes names
                        "DisplayName",
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                    }
                }
            );

            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Password = string.IsNullOrWhiteSpace(password) ? null : password,
                Player = player
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

            SetCurrentLobby(lobby);
            ResetTimers();

            OnCurrentLobbyChanged?.Invoke(CurrentLobby);
            ShowLobbyRoomPanel();

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("JoinLobby failed... " + e);
            return false;
        }
    }

    public async Task LeaveLobby()
    {
        //not in a looby at the moment
        if (joinedLobby == null)
            return;

        try
        {
            //migrate host id to first joined player
            if (IsHost && CurrentLobby != null && CurrentLobby.Players.Count > 1)
                await MigrateLobbyHost();

            //remove player (self) from lobby
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("LeaveLobby failed... " + e);
        }
        finally
        {
            if (UnityRelayManager.Instance != null)
                UnityRelayManager.Instance.Disconnect();
            
            ClearLobby();
            OnLeftLobby?.Invoke();
            OnCurrentLobbyChanged?.Invoke(null);

            ShowLobbyBrowsePanel();
            await RefreshLobbies();
        }
    }

    public void ShowLobbyBrowsePanel()
    {
        if (lobbyRoomPanel != null)
            lobbyRoomPanel.SetActive(false);

        if (lobbyBrowsePanel != null)
            lobbyBrowsePanel.SetActive(true);
    }

    public void ShowLobbyRoomPanel()
    {
        if (lobbyBrowsePanel != null)
            lobbyBrowsePanel.SetActive(false);

        if (lobbyRoomPanel != null)
            lobbyRoomPanel.SetActive(true);
    }

    //Store current lobby and update host lobby if local player is host
    private void SetCurrentLobby(Lobby lobby)
    {
        joinedLobby = lobby;

        //If lobby is not null, you are signed in (anonymously) and your player id is the same as host id, then set lobby to host lobby
        if (lobby != null && AuthenticationService.Instance.IsSignedIn && lobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            hostLobby = lobby;
        }
        else
        {
            hostLobby = null;
        }
    }

    /// <summary>
    /// Clears parameters/fields to prevent any exceptions thrown, when leaving or disconnection from prev current lobby
    /// </summary>
    private void ClearLobby()
    {
        hostLobby = null;
        joinedLobby = null;

        lobbyPollTimer = 0f;
        heartbeatTimer = 0f;

        hasStartedRelayClient = false;
        hasStartedRelayHost = false;
    }

    //Reset heartbeat and poll timers
    private void ResetTimers()
    {
        heartbeatTimer = heartbeatTimeMax;
        lobbyPollTimer = lobbyPollTimeMax;
    }

    //Host sends heartbeat so lobby stays active
    private async void HandleLobbyHeartbeat()
    {
        //only host sends heartbeats
        if (hostLobby == null)
            return;

        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer > 0f)
            return;

        heartbeatTimer = heartbeatTimeMax;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Heartbeat failed: " + e);
        }
    }

    //Poll lobby so all players get latest room data
    private async void HandleLobbyPoll()
    {
        //everyone in lobby will poll to update lobby room
        if (joinedLobby == null)
            return;

        lobbyPollTimer -= Time.deltaTime;

        if (lobbyPollTimer > 0f)
            return;

        lobbyPollTimer = lobbyPollTimeMax;

        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

            SetCurrentLobby(lobby);
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);

            await TryStartGameAfterCountdown();
            TryStartClientFromLobbyData();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Lobby poll failed: " + e);
        }
    }

    //Check if lobby needs password before joining
    public bool LobbyRequiresPassword(Lobby lobby)
    {
        //check public password flag
        if (lobby?.Data == null)
            return false;

        return lobby.Data.TryGetValue("HasPassword", out DataObject hasPasswordData) &&
               hasPasswordData.Value == "1";
    }

    //Move host to next player before current host leaves
    private async Task MigrateLobbyHost()
    {
        //Checks if lobby is not null or players in lobby null
        if (CurrentLobby == null || CurrentLobby.Players == null)
            return;
        //If not host, return
        if (!IsHost)
            return;
        //If only 1 player in lobby, return
        if (CurrentLobby.Players.Count < 2)
            return;

        try
        {     //Set host id to first joined player (client) id
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                CurrentLobby.Id,
                new UpdateLobbyOptions
                {
                    HostId = CurrentLobby.Players[1].Id
                });

            //Update lobby room
            SetCurrentLobby(updatedLobby);
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Host migration failed: " + e);
        }
    }

    //Check if shared countdown is active
    public bool IsCountdownActive()
    {
        if (CurrentLobby?.Data == null)
            return false;

        if (!CurrentLobby.Data.TryGetValue(countdownActiveKey, out DataObject value))
            return false;

        return value.Value == "1";
    }

    //Get countdown end time from lobby data
    public long GetCountdownEndTime()
    {
        if (CurrentLobby?.Data == null)
            return 0;

        if (!CurrentLobby.Data.TryGetValue(countdownEndTimeKey, out DataObject value))
            return 0;

        if (long.TryParse(value.Value, out long endTime))
            return endTime;

        return 0;
    }

    //Start shared countdown for all players
    public async Task StartGameCountdown(int seconds)
    {
        if (!IsHost || CurrentLobby == null)
            return;

        hasStartedRelayHost = false;

        long endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;

        try
        {
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                CurrentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { countdownActiveKey, new DataObject(DataObject.VisibilityOptions.Member, "1") },
                        { countdownEndTimeKey, new DataObject(DataObject.VisibilityOptions.Member, endTime.ToString()) }
                    }
                });
            SetCurrentLobby(updatedLobby);
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("StartGameCountdown failed: " + e);
        }
    }

    //Cancel shared countdown for all players
    public async Task CancelGameCountdown()
    {
        if (!IsHost || CurrentLobby == null)
            return;

        hasStartedRelayHost = false;

        try
        {
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                CurrentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { countdownActiveKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                        { countdownEndTimeKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                        { gameStartingKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                        { relayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, "") }
                    }
                });
            SetCurrentLobby(updatedLobby);
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("CancelGameCountdown failed: " + e);
        }
    }

    /// <summary>
    /// New code
    /// </summary>
    /// <returns></returns>
    public async Task StartGameWithRelay()
    {
        if (!IsHost || CurrentLobby == null)
        {
            Debug.LogWarning("Only lobby host can start game with Relay.");
            return;
        }

        if (UnityRelayManager.Instance == null)
        {
            Debug.LogError("UnityRelayManager not found.");
            return;
        }

        string relayJoinCode = await UnityRelayManager.Instance.StartHostWithRelay();

        if (string.IsNullOrWhiteSpace(relayJoinCode))
        {
            Debug.LogError("Could not start Relay host.");
            return;
        }

        try
        {
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                CurrentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { gameStartingKey, new DataObject(DataObject.VisibilityOptions.Member, "1") },
                        { relayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                        { countdownActiveKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                        { countdownEndTimeKey, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                    }
                });

            SetCurrentLobby(updatedLobby);
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);

            Debug.Log("Relay join code stored in lobby: " + relayJoinCode);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("NetworkManager is not running as server. Cannot load game scene.");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to store Relay join code in Lobby: " + e);
        }
    }

    /// <summary>
    /// New code
    /// </summary>
    /// <returns></returns>
    private async Task TryStartGameAfterCountdown()
    {
        if (!IsHost)
            return;

        if (hasStartedRelayHost)
            return;

        if (!IsCountdownActive())
            return;

        long endTime = GetCountdownEndTime();

        if (endTime <= 0)
            return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (now < endTime)
            return;

        hasStartedRelayHost = true;

        Debug.Log("Countdown finished. Starting Relay game...");
        await StartGameWithRelay();
    }

    /// <summary>
    /// New code
    /// </summary>
    /// <returns></returns>
    private async void TryStartClientFromLobbyData()
    {
        if (IsHost)
            return;

        if (hasStartedRelayClient)
            return;

        if (CurrentLobby?.Data == null)
            return;

        if (!CurrentLobby.Data.TryGetValue(gameStartingKey, out DataObject gameStartingData))
            return;

        if (gameStartingData.Value != "1")
            return;

        if (!CurrentLobby.Data.TryGetValue(relayJoinCodeKey, out DataObject relayCodeData))
            return;

        string relayJoinCode = relayCodeData.Value;

        if (string.IsNullOrWhiteSpace(relayJoinCode))
            return;

        if (UnityRelayManager.Instance == null)
        {
            Debug.LogError("UnityRelayManager not found.");
            return;
        }

        hasStartedRelayClient = true;

        bool connected = await UnityRelayManager.Instance.StartClientWithRelay(relayJoinCode);

        if (!connected)
        {
            hasStartedRelayClient = false;
            Debug.LogError("Failed to connect Relay client from lobby data.");
        }
    }

    [Serializable]
    public class LobbyPlayerViewData
    {
        public string PlayerId;
        public string DisplayName;
        public bool IsHost;
        public int JoinOrder;
    }
}