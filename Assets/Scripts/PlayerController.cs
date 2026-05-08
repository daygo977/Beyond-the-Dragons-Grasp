using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    PlayerInput playerInput;
    PlayerInput.MainActions input;

    CharacterController controller;
    AudioSource audioSource;
    AudioListener audioListener;
    Health playerHealth;

    [Header("First Person Animation")]
    public Animator firstPersonAnimator;

    PlayerModelVisibility modelVisibility;
    Unity.Netcode.Components.NetworkAnimator thirdPersonNetworkAnimator;

    [Header("Controller")]
    public float moveSpeed = 3.5f;
    public float sprintSpeed = 6.0f;
    public float gravity = -9.8f;
    public float jumpHeight = 1.0f;

    Vector3 _playerVelocity;
    bool isGrounded;
    bool wasGrounded;
    bool isSprinting;

    [Header("Fall Damage")]
    public bool enableFallDamage = true;
    public float fallDamageThreshold = 12f;
    public float fallDamageMultiplier = 4f;
    public int maxFallDamage = 100;

    [Header("Camera")]
    public Camera cam;
    public float sensitivity = 0.1f;

    float xRotation = 0f;

    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public TextMeshProUGUI interactText;
    public string sceneInteractTextObjectName = "Interact Text";

    IInteractable currentInteractable;
    IHoldInteractable currentHoldInteractable;

    [Header("Attacking")]
    public float attackDistance = 3f;
    public float attackDelay = 0.4f;
    public float attackSpeed = 1f;
    public int attackDamage = 1;
    public LayerMask attackLayer;

    public GameObject hitEffect;

    [Header("Player Audio")]
    public AudioClip[] swordSwingClips;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float swordSwingVolume = 0.8f;
    [Range(0f, 1f)] public float hitSoundVolume = 1f;

    [Header("Third Person Model Animation")]
    public Animator thirdPersonAnimator;
    public string moveXParameter = "MoveX";
    public string moveYParameter = "MoveY";
    public string isMovingParameter = "IsMoving";
    public string isRunningParameter = "IsRunning";
    public string isGroundedParameter = "IsGrounded";
    public string jumpTriggerParameter = "Jump";
    public string attackTriggerParameter = "Attack";

    bool attacking = false;
    bool readyToAttack = true;
    int attackCount;

    public const string IDLE = "Idle";
    public const string WALK = "Walk";
    public const string ATTACK1 = "Attack 1";
    public const string ATTACK2 = "Attack 2";

    string currentAnimationState;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerHealth = GetComponent<Health>();

        if (firstPersonAnimator == null)
            firstPersonAnimator = GetComponentInChildren<Animator>();

        playerInput = new PlayerInput();
        input = playerInput.Main;

        AssignInputs();

        if (cam != null)
            audioListener = cam.GetComponent<AudioListener>();

        modelVisibility = GetComponent<PlayerModelVisibility>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    bool HasControl()
    {
        return !IsSpawned || IsOwner;
    }

    public override void OnNetworkSpawn()
    {
        if (HasControl())
        {
            input.Enable();

            if (cam != null)
                cam.enabled = true;

            if (audioListener != null)
                audioListener.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            FindSceneInteractText();

            if (interactText != null)
                interactText.text = "";
        }
        else
        {
            input.Disable();

            if (cam != null)
                cam.enabled = false;

            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        input.Disable();
    }

    void Update()
    {
        if (!HasControl()) return;

        FindSceneInteractText();
        RefreshThirdPersonAnimator();

        if (controller == null)
            return;

        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        if (!wasGrounded && isGrounded)
            CheckFallDamage();

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            SetPausedAnimationState();

            if (interactText != null)
                interactText.text = "";

            currentInteractable = null;
            currentHoldInteractable = null;
            return;
        }

        Vector2 moveInput = input.Movement.ReadValue<Vector2>();
        Vector2 lookInput = input.Look.ReadValue<Vector2>();

        isSprinting = Keyboard.current != null &&
                      Keyboard.current.leftShiftKey.isPressed &&
                      moveInput.y > 0f;

        MoveInput(moveInput);
        UpdateThirdPersonAnimator(moveInput);
        LookInput(lookInput);

        CheckForInteractable();
        HandleInteractionInput();

        SetAnimations();
    }

    void FindSceneInteractText()
    {
        if (interactText != null)
            return;

        if (string.IsNullOrWhiteSpace(sceneInteractTextObjectName))
            return;

        GameObject textObject = GameObject.Find(sceneInteractTextObjectName);

        if (textObject == null)
            return;

        interactText = textObject.GetComponent<TextMeshProUGUI>();

        if (interactText != null)
            interactText.text = "";
    }

    void MoveInput(Vector2 inputValue)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = inputValue.x;
        moveDirection.z = inputValue.y;

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        controller.Move(transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime);

        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (isGrounded && _playerVelocity.y < 0f)
            _playerVelocity.y = -2f;

        _playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(_playerVelocity * Time.deltaTime);
    }

    void CheckFallDamage()
    {
        if (!enableFallDamage) return;
        if (playerHealth == null) return;

        float landingSpeed = Mathf.Abs(_playerVelocity.y);

        if (landingSpeed <= fallDamageThreshold)
            return;

        int damage = Mathf.RoundToInt((landingSpeed - fallDamageThreshold) * fallDamageMultiplier);
        damage = Mathf.Clamp(damage, 0, maxFallDamage);

        if (damage <= 0)
            return;

        if (!IsSpawned)
            playerHealth.TakeDamage(damage);
        else
            RequestSelfDamageServerRpc(damage);
    }

    [ServerRpc]
    void RequestSelfDamageServerRpc(int damageAmount)
    {
        Health health = GetComponent<Health>();

        if (health == null)
            return;

        health.TakeDamage(damageAmount);
    }

    void UpdateThirdPersonAnimator(Vector2 moveInput)
    {
        if (thirdPersonAnimator == null) return;

        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        bool moving = moveInput.sqrMagnitude > 0.01f;

        thirdPersonAnimator.SetFloat(moveXParameter, moveInput.x);
        thirdPersonAnimator.SetFloat(moveYParameter, moveInput.y);
        thirdPersonAnimator.SetBool(isMovingParameter, moving);
        thirdPersonAnimator.SetBool(isRunningParameter, isSprinting);
        thirdPersonAnimator.SetBool(isGroundedParameter, isGrounded);
    }

    void SetPausedAnimationState()
    {
        if (thirdPersonAnimator != null)
        {
            thirdPersonAnimator.SetFloat(moveXParameter, 0f);
            thirdPersonAnimator.SetFloat(moveYParameter, 0f);
            thirdPersonAnimator.SetBool(isMovingParameter, false);
            thirdPersonAnimator.SetBool(isRunningParameter, false);
            thirdPersonAnimator.SetBool(isGroundedParameter, true);
        }

        if (!attacking)
            ChangeAnimationState(IDLE);
    }

    void LookInput(Vector2 inputValue)
    {
        if (cam == null) return;

        float mouseX = inputValue.x;
        float mouseY = inputValue.y;

        xRotation -= mouseY * sensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX * sensitivity);
    }

    void CheckForInteractable()
    {
        currentInteractable = null;
        currentHoldInteractable = null;

        if (cam == null)
            return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactLayer))
        {
            MonoBehaviour[] behaviours = hit.collider.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    currentInteractable = interactable;

                    if (behaviour is IHoldInteractable holdInteractable)
                        currentHoldInteractable = holdInteractable;

                    if (interactText != null)
                        interactText.text = currentInteractable.GetPromptText();

                    return;
                }
            }
        }

        if (interactText != null)
            interactText.text = "";
    }

    void HandleInteractionInput()
    {
        if (Keyboard.current == null)
            return;

        if (currentHoldInteractable != null)
        {
            if (Keyboard.current.eKey.isPressed)
            {
                currentHoldInteractable.HoldInteract(Time.deltaTime);

                if (interactText != null)
                    interactText.text = currentHoldInteractable.GetPromptText();
            }
            else
            {
                currentHoldInteractable.ResetHold();

                if (interactText != null && currentInteractable != null)
                    interactText.text = currentInteractable.GetPromptText();
            }

            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
            TryInteract();
    }

    void TryInteract()
    {
        if (!HasControl()) return;
        if (cam == null) return;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactLayer))
            return;

        bool networkRunning = IsSpawned &&
                              NetworkManager.Singleton != null &&
                              NetworkManager.Singleton.IsListening;

        if (networkRunning)
        {
            NetworkObject networkObject = hit.collider.GetComponentInParent<NetworkObject>();

            if (networkObject != null)
            {
                RequestInteractServerRpc(networkObject.NetworkObjectId);
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable.Interact();

            if (interactText != null)
                interactText.text = currentInteractable.GetPromptText();
        }
    }

    [ServerRpc]
    void RequestInteractServerRpc(ulong objectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObject))
            return;

        KeyPickup keyPickup = netObject.GetComponentInChildren<KeyPickup>();

        if (keyPickup != null)
        {
            keyPickup.Interact();
            return;
        }

        DoorInteractable door = netObject.GetComponentInChildren<DoorInteractable>();

        if (door != null)
        {
            bool hasKey = GameFlags.Instance != null && GameFlags.Instance.HasKey(door.requiredKeyId);

            if (door.requiresKey && !hasKey)
            {
                ShowDoorFailPromptClientRpc(OwnerClientId, objectId);
                return;
            }

            door.Interact();
            return;
        }

        EscapeDoorInteractable escapeDoor = netObject.GetComponentInChildren<EscapeDoorInteractable>();

        if (escapeDoor != null)
        {
            escapeDoor.Interact();
            return;
        }

        HealthPickup healthPickup = netObject.GetComponentInChildren<HealthPickup>();

        if (healthPickup != null)
        {
            healthPickup.Interact(OwnerClientId);
            return;
        }

        MonoBehaviour[] behaviours = netObject.GetComponentsInChildren<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                interactable.Interact();
                return;
            }
        }
    }

    [ClientRpc]
    void ShowDoorFailPromptClientRpc(ulong targetClientId, ulong objectId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObject))
            return;

        DoorInteractable door = netObject.GetComponentInChildren<DoorInteractable>();

        if (door != null)
            door.ShowFailPromptLocal();
    }

    void OnEnable()
    {
        if (playerInput == null)
            return;

        if (!IsSpawned || IsOwner)
            input.Enable();
    }

    void OnDisable()
    {
        if (playerInput != null)
            input.Disable();
    }

    void Jump()
    {
        if (!HasControl()) return;

        if (isGrounded)
        {
            _playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);

            if (thirdPersonNetworkAnimator != null && IsSpawned)
                thirdPersonNetworkAnimator.SetTrigger(jumpTriggerParameter);
            else if (thirdPersonAnimator != null)
                thirdPersonAnimator.SetTrigger(jumpTriggerParameter);
        }
    }

    void AssignInputs()
    {
        input.Jump.performed += ctx =>
        {
            if (!HasControl()) return;
            Jump();
        };

        input.Attack.started += ctx =>
        {
            if (!HasControl()) return;
            Attack();
        };
    }

    public void ChangeAnimationState(string newState)
    {
        if (firstPersonAnimator == null) return;
        if (!firstPersonAnimator.gameObject.activeInHierarchy) return;
        if (currentAnimationState == newState) return;

        currentAnimationState = newState;
        firstPersonAnimator.CrossFadeInFixedTime(currentAnimationState, 0.2f);
    }

    void SetAnimations()
    {
        if (!attacking)
        {
            Vector2 moveInput = input.Movement.ReadValue<Vector2>();

            if (moveInput == Vector2.zero)
                ChangeAnimationState(IDLE);
            else
                ChangeAnimationState(WALK);
        }
    }

    public void Attack()
    {
        if (!HasControl()) return;
        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

        PlayRandomSound(swordSwingClips, swordSwingVolume);

        if (thirdPersonNetworkAnimator != null && IsSpawned)
            thirdPersonNetworkAnimator.SetTrigger(attackTriggerParameter);
        else if (thirdPersonAnimator != null)
            thirdPersonAnimator.SetTrigger(attackTriggerParameter);

        if (attackCount == 0)
        {
            ChangeAnimationState(ATTACK1);
            attackCount++;
        }
        else
        {
            ChangeAnimationState(ATTACK2);
            attackCount = 0;
        }
    }

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
    }

    void AttackRaycast()
    {
        if (!HasControl()) return;
        if (cam == null) return;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
            return;

        HitTarget(hit.point);

        EnemyHealth enemyHealth = hit.transform.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
            enemyHealth = hit.transform.GetComponentInParent<EnemyHealth>();

        if (enemyHealth != null)
            enemyHealth.RequestTakeDamage(attackDamage);
    }

    void HitTarget(Vector3 pos)
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(hitSound, hitSoundVolume);
        }

        if (hitEffect != null)
        {
            GameObject go = Instantiate(hitEffect, pos, Quaternion.identity);
            Destroy(go, 20f);
        }
    }

    void PlayRandomSound(AudioClip[] clips, float volume = 1f)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        if (clip == null) return;

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clip, volume);
    }

    void RefreshThirdPersonAnimator()
    {
        if (modelVisibility == null)
            return;

        if (modelVisibility.ActiveThirdPersonAnimator != null)
            thirdPersonAnimator = modelVisibility.ActiveThirdPersonAnimator;

        if (modelVisibility.ActiveThirdPersonNetworkAnimator != null)
            thirdPersonNetworkAnimator = modelVisibility.ActiveThirdPersonNetworkAnimator;
    }
}