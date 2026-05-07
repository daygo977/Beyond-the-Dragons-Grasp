using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("References")]
    public Health playerHealth;
    public PlayerController playerController;
    public CharacterController characterController;
    public Animator playerAnimator;

    [Header("Death Animation")]
    public string deathTriggerName = "Die";
    public string isDeadBoolName = "IsDead";

    [Header("Death UI")]
    public GameObject deathPanel;
    public Image redOverlay;
    public TextMeshProUGUI deathText;

    [Header("UI Settings")]
    public string deathMessage = "You Died!";
    [Range(0f, 1f)] public float redOverlayAlpha = 0.65f;

    private bool hasDied;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDeath;
    }

    void Start()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    void HandleDeath()
    {
        if (hasDied) return;

        hasDied = true;

        if (playerController != null)
            playerController.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(isDeadBoolName, true);
            playerAnimator.SetTrigger(deathTriggerName);
        }

        ShowDeathScreen();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowDeathScreen()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);

        if (redOverlay != null)
        {
            Color color = redOverlay.color;
            color.a = redOverlayAlpha;
            redOverlay.color = color;
        }

        if (deathText != null)
            deathText.text = deathMessage;
    }
}