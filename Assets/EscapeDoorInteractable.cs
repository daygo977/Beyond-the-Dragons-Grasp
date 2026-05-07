using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class EscapeDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string escapePrompt = "Press E to escape!";

    [TextArea]
    public string notReadyPrompt = "You need to be near the door.";

    [Header("Local Escape Check")]
    public bool playerIsInEscapeZone;

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

    private bool escaped;
    private PlayerController playerController;

    void Start()
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

    void OnDestroy()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
    }

    public string GetPromptText()
    {
        if (escaped)
            return "";

        if (playerIsInEscapeZone)
            return escapePrompt;

        return notReadyPrompt;
    }

    public void Interact()
    {
        if (escaped) return;

        if (!playerIsInEscapeZone)
            return;

        TriggerEscape();
    }

    public void SetPlayerInEscapeZone(bool inside)
    {
        playerIsInEscapeZone = inside;
    }

    void TriggerEscape()
    {
        escaped = true;

        playerController = Object.FindFirstObjectByType<PlayerController>();

        if (playerController != null)
            playerController.enabled = false;

        Time.timeScale = 0f;

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

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}