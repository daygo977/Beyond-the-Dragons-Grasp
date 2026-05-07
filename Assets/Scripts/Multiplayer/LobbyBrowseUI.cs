using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyBrowseUI : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_InputField passwordField;

    [Header("Buttons")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button enterPasswordButton;
    [SerializeField] private Button cancelPasswordButton;
    [SerializeField] private Button returnButton;

    [Header("Lobby List")]
    [SerializeField] private Transform lobbyListContent;
    [SerializeField] private GameObject roomEntryPrefab;

    [Header("Password Join Popup")]
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private TMP_InputField joinPasswordField;

    [Header("Save Data")]
    [SerializeField] private string playerNameKey = "PLAYER_NAME";

    private Lobby selectedLobby;

    private async void Start()
    {
        //Wait until Unity Services is ready
        while (!UnityServicesBootstrap.IsInitialized)
            await Task.Yield();

        //Hooks up buttons to their corresponding functions
        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(CreateLobby);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshLobbies);

        if (enterPasswordButton != null)
            enterPasswordButton.onClick.AddListener(JoinWithPassword);

        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.AddListener(ClosePasswordPanel);
        if (returnButton != null)
            returnButton.onClick.AddListener(ReturnToMainMenu);

        //Password panel starts deactivated
        ClosePasswordPanel();
        //Load saved player name from previous session
        LoadPlayerName();

        if (UnityLobbyManager.Instance == null)
        {
            Debug.LogWarning("UnityLobbyManager not found");
            return;
        }

        UnityLobbyManager.Instance.OnAvailableLobbiesChanged += ShowLobbies;
        await UnityLobbyManager.Instance.RefreshLobbies();
    }

    private void OnDestroy()
    {

        //Removes button functions
        if (createLobbyButton != null)
            createLobbyButton.onClick.RemoveListener(CreateLobby);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(RefreshLobbies);

        if (enterPasswordButton != null)
            enterPasswordButton.onClick.RemoveListener(JoinWithPassword);

        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.RemoveListener(ClosePasswordPanel);
        if (returnButton != null)
            returnButton.onClick.RemoveListener(ReturnToMainMenu);

        if (UnityLobbyManager.Instance != null)
            UnityLobbyManager.Instance.OnAvailableLobbiesChanged -= ShowLobbies;
    }

    //Create lobby from input fields
    private async void CreateLobby()
    {
        //Takes input fields from players
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";
        string lobbyName = lobbyNameField != null ? lobbyNameField.text.Trim() : "";
        string password = passwordField != null ? passwordField.text.Trim() : "";

        //Checks to see if required input fields are not blank
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            Debug.LogWarning("Lobby name is required");
            return;
        }

        SavePlayerName(playerName);

        //Sends info to CreateLobby function in UnityLobbyManager
        if (UnityLobbyManager.Instance != null)
            await UnityLobbyManager.Instance.CreateLobby(playerName, lobbyName, password);
    }

    //Refresh lobby browser list
    private async void RefreshLobbies()
    {
        if (UnityLobbyManager.Instance != null)
            await UnityLobbyManager.Instance.RefreshLobbies();
    }

    //Join selected lobby or open password panel
    private async void SelectLobby(Lobby lobby)
    {
        //Takes only player name field
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";
        //Checks player name field is not blank
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required before joining");
            return;
        }

        SavePlayerName(playerName);

        if (UnityLobbyManager.Instance == null)
            return;

        
        //If selected lobby you want to join needs password, opens password planel
        if (UnityLobbyManager.Instance.LobbyRequiresPassword(lobby))
        {
            OpenPasswordPanel(lobby);
            return;
        }

        //Joins selected lobby
        await UnityLobbyManager.Instance.JoinLobby(lobby.Id, playerName);
    }

    //Join selected password-protected lobby
    private async void JoinWithPassword()
    {
        //safe guard
        if (selectedLobby == null)
        {
            ClosePasswordPanel();
            return;
        }

        //Takes player input fields, only name and password you input to join lobby
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";
        string password = joinPasswordField != null ? joinPasswordField.text.Trim() : "";
        //Checks to see if name is not blank
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required before joining");
            return;
        }

        SavePlayerName(playerName);

        if (UnityLobbyManager.Instance == null)
            return;

        //Sends info to function in UnityLobbyManager, and if password was correct, true, else, false
        bool joined = await UnityLobbyManager.Instance.JoinLobby(selectedLobby.Id, playerName, password);

        //If password true, then password panel will close
        if (joined)
            ClosePasswordPanel();
    }

    //Activate password popup for selected lobby
    private void OpenPasswordPanel(Lobby lobby)
    {
        //Grabs lobby to grab correct password for lobby
        selectedLobby = lobby;

        //Make sure input has no prior password in field when it opens
        if (joinPasswordField != null)
            joinPasswordField.text = "";

        if (passwordPanel != null)
            passwordPanel.SetActive(true);
    }

    //Deactivate password popup and clear data
    private void ClosePasswordPanel()
    {
        //Clears selected lobby
        selectedLobby = null;

        //Clears field when closing panel
        if (joinPasswordField != null)
            joinPasswordField.text = "";

        if (passwordPanel != null)
            passwordPanel.SetActive(false);
    }

    //Rebuild lobby rows in scroll list
    private void ShowLobbies(List<Lobby> lobbies)
    {
        //Safe guard
        if (lobbyListContent == null)
        {
            Debug.LogWarning("LobbyListContent is missing");
            return;
        }

        //Remove old lobbies (rows) in scroll view
        for (int i = lobbyListContent.childCount - 1; i >= 0; i--)
            Destroy(lobbyListContent.GetChild(i).gameObject);
        //Safe guard when no lobbies
        if (lobbies == null)
            return;

        //Create one row (prefab) per lobby
        foreach (Lobby lobby in lobbies)
        {
            GameObject row = Instantiate(roomEntryPrefab, lobbyListContent);
            LobbyRoomEntryUI rowUI = row.GetComponent<LobbyRoomEntryUI>();

            //If not null, setup the lobby name and click action for prefab
            if (rowUI != null)
                rowUI.Setup(lobby.Name, () => SelectLobby(lobby));
        }
    }

    private void SavePlayerName(string playerName)
    {
        //Save player name locally
        PlayerPrefs.SetString(playerNameKey, playerName);
        PlayerPrefs.Save();
    }

    private void LoadPlayerName()
    {
        //Load saved name into input field
        if (playerNameField != null && PlayerPrefs.HasKey(playerNameKey))
            playerNameField.text = PlayerPrefs.GetString(playerNameKey);
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}