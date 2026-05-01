using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button confirmLeaveButton;
    [SerializeField] private Button cancelLeaveButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Popup")]
    [SerializeField] private GameObject leaveConfirmScreen;

    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerPanelPrefab;

    [Header("Settings")]
    [SerializeField] private int maxPlayers = 4;

    

    private void Start()
    {
        //button listeners
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.AddListener(OpenLeavePanel);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(PressStartButton);

        if (confirmLeaveButton != null)
            confirmLeaveButton.onClick.AddListener(LeaveLobby);

        if (cancelLeaveButton != null)
            cancelLeaveButton.onClick.AddListener(CloseLeavePanel);

        //Confirm leave panel starts deactivated
        CloseLeavePanel();

        //Countdown starts deactivated
        HideCountdown();

        if (UnityLobbyManager.Instance == null)
        {
            Debug.LogWarning("UnityLobbyManager not found");
            return;
        }

        //Subscribe to lobby updates
        UnityLobbyManager.Instance.OnCurrentLobbyChanged += RefreshRoom;

        //CurrentLobby is joinedLobby in UnityLobbyManager
        //Refresh room at start
        RefreshRoom(UnityLobbyManager.Instance.CurrentLobby);
    }

    //Update shared countdown text
    private void Update()
    {
        UpdateCountdown();
    }

    private void OnDestroy()
    {
        //remove button listeners
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.RemoveListener(OpenLeavePanel);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(PressStartButton);

        if (confirmLeaveButton != null)
            confirmLeaveButton.onClick.RemoveListener(LeaveLobby);

        if (cancelLeaveButton != null)
            cancelLeaveButton.onClick.RemoveListener(CloseLeavePanel);
        
        //Unsubscribe to lobby updates
        if (UnityLobbyManager.Instance != null)
            UnityLobbyManager.Instance.OnCurrentLobbyChanged -= RefreshRoom;
    }

    //Refresh room UI when lobby data changes
    private void RefreshRoom(Lobby lobby)
    {
        //Clear old player rows
        ClearPlayerRows();
        
        if (UnityLobbyManager.Instance == null || lobby == null)
            return;
        
        UpdateStartButton();
        ShowPlayers(lobby);
    }

    //Spawn player rows from current lobby players
    private void ShowPlayers(Lobby lobby)
    {
        if (lobby.Players == null)
            return;

        int playerCount = Mathf.Min(lobby.Players.Count, maxPlayers);

        for (int i = 0; i < playerCount; i++)
        {
            Player player = lobby.Players[i];
            string playerName = player.Id;

            if (player.Data != null &&
                player.Data.TryGetValue("DisplayName", out PlayerDataObject nameData) &&
                !string.IsNullOrWhiteSpace(nameData.Value))
            {
                playerName = nameData.Value;
            }

            CreatePlayerRow(playerName);
        }
    }

    //Update start button state for host and clients
    private void UpdateStartButton()
    {
        if (startGameButton == null)
            return;

        bool isHost = UnityLobbyManager.Instance != null && UnityLobbyManager.Instance.IsHost;
        bool countdownActive = UnityLobbyManager.Instance != null && UnityLobbyManager.Instance.IsCountdownActive();

        // Only host can press start or cancel
        startGameButton.interactable = isHost;

        TMP_Text buttonText = startGameButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
            buttonText.text = countdownActive ? "Cancel" : "Start Game";
    }

    //Show shared countdown for all players
    private void UpdateCountdown()
    {
        if (UnityLobbyManager.Instance == null)
            return;

        if (!UnityLobbyManager.Instance.IsCountdownActive())
        {
            HideCountdown();
            return;
        }

        long endTime = UnityLobbyManager.Instance.GetCountdownEndTime();
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long secondsLeft = endTime - now;

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        if (secondsLeft > 0)
        {
            if (countdownText != null)
                countdownText.text = "Starting Game in " + secondsLeft + "...";
        }
        else
        {
            if (countdownText != null)
                countdownText.text = "Starting Game...";
        }

        UpdateStartButton();
    }

    //Open leave confirmation panel
    private void OpenLeavePanel()
    {
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(true);
    }

    //Close leave confirmation panel
    private void CloseLeavePanel()
    {
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(false);
    }

    //Leave current lobby
    private async void LeaveLobby()
    {
        if (UnityLobbyManager.Instance != null)
            await UnityLobbyManager.Instance.LeaveLobby();
    }

    //Host starts or cancels shared countdown
    private async void PressStartButton()
    {
        if (UnityLobbyManager.Instance == null || !UnityLobbyManager.Instance.IsHost)
        {
            Debug.Log("Only host can start or cancel countdown");
            return;
        }

        if (UnityLobbyManager.Instance.IsCountdownActive())
            await UnityLobbyManager.Instance.CancelGameCountdown();
        else
            await UnityLobbyManager.Instance.StartGameCountdown(10);

        UpdateStartButton();
}

    //Deactivate countdown text
    private void HideCountdown()
    {
        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
    }

    //Remove old player rows from scroll list
    private void ClearPlayerRows()
    {
        if (playerListContent == null)
        {
            Debug.LogWarning("PlayerListContent is missing");
            return;
        }

        for (int i = playerListContent.childCount - 1; i >= 0; i--)
            Destroy(playerListContent.GetChild(i).gameObject);
    }

    //Create one player row
    private void CreatePlayerRow(string playerName)
    {
        if (playerPanelPrefab == null || playerListContent == null)
        {
            Debug.LogWarning("PlayerPanelPrefab or PlayerListContent is missing");
            return;
        }

        GameObject row = Instantiate(playerPanelPrefab, playerListContent);
        LobbyPlayerEntryUI rowUI = row.GetComponent<LobbyPlayerEntryUI>();

        if (rowUI != null)
            rowUI.Setup(playerName);
    }
}