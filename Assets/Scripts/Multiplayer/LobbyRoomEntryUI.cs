using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controls one room row in the browse list.
/// This row only shows the lobby name and handles the click action.
/// </summary>
public class LobbyRoomEntryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private Button joinButton;

    public void Setup(string lobbyName, UnityAction onClick)
    {
        SetLobbyName(lobbyName);
        SetJoinButton(onClick);
    }

    private void SetLobbyName(string lobbyName)
    {
        if (lobbyNameText != null)
            lobbyNameText.text = lobbyName;
    }

    private void SetJoinButton(UnityAction onClick)
    {
        if (joinButton == null)
            return;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(onClick);
    }
}