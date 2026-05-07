using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

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

    private void Awake()
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

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDeath;
    }

    //Multiplayer edit, new logic
    private void Start()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (playerHealth != null && playerHealth.IsDead)
            HandleDeath();
    }

    //Multiplayer new function
    private void Update()
    {
        if (!hasDied && playerHealth != null && playerHealth.IsDead)
            HandleDeath();
    }

    private void HandleDeath()
    {
        if (hasDied) return;

        hasDied = true;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(isDeadBoolName, true);
            playerAnimator.SetTrigger(deathTriggerName);
        }

        //Multiplayer edit, new logic
        if (IsLocalPlayer())
        {
            if (playerController != null)
                playerController.enabled = false;

            if (characterController != null)
                characterController.enabled = false;

            ShowDeathScreen();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    //Multiplayer new function
    private bool IsLocalPlayer()
    {
        if (playerHealth == null)
            return false;

        if (!playerHealth.IsSpawned)
            return true;

        return playerHealth.IsOwner;
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