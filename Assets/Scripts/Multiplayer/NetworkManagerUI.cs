using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(StartRelayHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartRelayClient);
    }

    private void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveListener(StartRelayHost);

        if (clientButton != null)
            clientButton.onClick.RemoveListener(StartRelayClient);
    }

    private async void StartRelayHost()
    {
        SetButtons(false);

        string joinCode = await UnityRelayManager.Instance.StartHostWithRelay();

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Failed to start Relay Host.");
            SetButtons(true);
            return;
        }

        GUIUtility.systemCopyBuffer = joinCode;

        Debug.Log("Relay Host started.");
        Debug.Log("Join Code: " + joinCode);
        Debug.Log("Join Code copied to clipboard.");
    }

    private async void StartRelayClient()
    {
        SetButtons(false);

        string joinCode = GUIUtility.systemCopyBuffer.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("No join code found in clipboard. Copy the host join code first.");
            SetButtons(true);
            return;
        }

        bool joined = await UnityRelayManager.Instance.StartClientWithRelay(joinCode);

        if (!joined)
        {
            Debug.LogError("Failed to start Relay Client.");
            SetButtons(true);
            return;
        }

        Debug.Log("Relay Client connected using join code: " + joinCode);
    }

    private void SetButtons(bool enabled)
    {
        if (hostButton != null)
            hostButton.interactable = enabled;

        if (clientButton != null)
            clientButton.interactable = enabled;
    }
}