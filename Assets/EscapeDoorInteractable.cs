using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class EscapeDoorInteractable : NetworkBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string escapePrompt = "Press E to escape!";

    [TextArea]
    public string notReadyPrompt = "All alive players need to be near the door.";

    [Header("Prompt Timing")]
    public float temporaryPromptDuration = 2f;

    [Header("Escape UI")]
    public GameObject escapePanel;
    public TextMeshProUGUI escapeText;
    public string escapedMessage = "You've escaped!";

    [Header("Buttons")]
    public Button mainMenuButton;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip escapeSound;
    [Range(0f, 1f)] public float escapeVolume = 1f;

    private readonly HashSet<ulong> playersInZone = new HashSet<ulong>();

    private bool escaped;
    private string temporaryPrompt = "";
    private float temporaryPromptTimer;

    private void Start()
    {
        if (escapePanel != null)
            escapePanel.SetActive(false);

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (temporaryPromptTimer > 0f)
        {
            temporaryPromptTimer -= Time.deltaTime;

            if (temporaryPromptTimer <= 0f)
                temporaryPrompt = "";
        }
    }

    public override void OnDestroy()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        base.OnDestroy();
    }

    public string GetPromptText()
    {
        if (escaped)
            return "";

        if (!string.IsNullOrEmpty(temporaryPrompt))
            return temporaryPrompt;

        if (IsLocalPlayerInZone())
            return escapePrompt;

        return notReadyPrompt;
    }

    public void Interact()
    {
        if (escaped)
            return;

        if (!IsSpawned)
        {
            if (IsLocalPlayerInZone())
                TriggerEscapeLocal();
            else
                ShowTemporaryPromptLocal(notReadyPrompt);

            return;
        }

        RequestEscapeServerRpc();
    }

    public void SetPlayerInEscapeZone(ulong clientId, bool inside)
    {
        if (inside)
            playersInZone.Add(clientId);
        else
            playersInZone.Remove(clientId);
    }

    private bool IsLocalPlayerInZone()
    {
        if (!IsSpawned || NetworkManager.Singleton == null)
            return true;

        return playersInZone.Contains(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEscapeServerRpc(ServerRpcParams rpcParams = default)
    {
        if (escaped)
            return;

        ulong requestingClientId = rpcParams.Receive.SenderClientId;

        if (!playersInZone.Contains(requestingClientId))
        {
            SendNotReadyPromptClientRpc(requestingClientId);
            return;
        }

        if (!AllAlivePlayersInZone())
        {
            SendNotReadyPromptClientRpc(requestingClientId);
            return;
        }

        escaped = true;
        TriggerEscapeClientRpc();
    }

    private bool AllAlivePlayersInZone()
    {
        if (NetworkManager.Singleton == null)
            return true;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            Health health = client.PlayerObject.GetComponent<Health>();

            if (health != null && health.IsDead)
                continue;

            if (!playersInZone.Contains(client.ClientId))
                return false;
        }

        return true;
    }

    [ClientRpc]
    private void TriggerEscapeClientRpc()
    {
        TriggerEscapeLocal();
    }

    [ClientRpc]
    private void SendNotReadyPromptClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        ShowTemporaryPromptLocal(notReadyPrompt);
    }

    private void ShowTemporaryPromptLocal(string message)
    {
        temporaryPrompt = message;
        temporaryPromptTimer = temporaryPromptDuration;
    }

    private void TriggerEscapeLocal()
    {
        escaped = true;

        PlayerController localPlayerController = GetLocalPlayerController();

        if (localPlayerController != null)
            localPlayerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (escapePanel != null)
            escapePanel.SetActive(true);

        if (escapeText != null)
            escapeText.text = escapedMessage;

        if (escapeSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(escapeSound, escapeVolume);
            else
                AudioSource.PlayClipAtPoint(escapeSound, transform.position, escapeVolume);
        }
    }

    private PlayerController GetLocalPlayerController()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        }

        return Object.FindFirstObjectByType<PlayerController>();
    }

    public void ReturnToMainMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}