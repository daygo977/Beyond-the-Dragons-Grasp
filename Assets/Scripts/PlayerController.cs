using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;
using UnityEngineInternal;

public class PlayerController : NetworkBehaviour
{
    PlayerInput playerInput;
    PlayerInput.MainActions input;

    CharacterController controller;
    Animator animator;
    AudioSource audioSource;
    AudioListener audioListener;

    [Header("Controller")]
    public float moveSpeed = 3.5f;
    public float sprintSpeed = 6.0f;
    public float gravity = -9.8f;
    public float jumpHeight = 1.0f;

    Vector3 _playerVelocity;
    bool isGrounded;
    bool isSprinting;

    [Header("Camera")]
    public Camera cam;
    public float sensitivity = 0.1f;

    float xRotation = 0f;

    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public TextMeshProUGUI interactText;

    IInteractable currentInteractable;

    [Header("Attacking")]
    public float attackDistance = 3f;
    public float attackDelay = 0.4f;
    public float attackSpeed = 1f;
    public int attackDamage = 1;
    public LayerMask attackLayer;

    public GameObject hitEffect;
    public AudioClip swordSwing;
    public AudioClip hitSound;

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
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (cam != null)
            audioListener = cam.GetComponent<AudioListener>();

        playerInput = new PlayerInput();
        input = playerInput.Main;

        AssignInputs();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (interactText != null)
            interactText.text = "";
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            input.Enable();

            if (cam != null)
                cam.enabled = true;

            if (audioListener != null)
                audioListener.enabled = false;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
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
        if (!IsOwner) return;

        isGrounded = controller.isGrounded;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            _playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(_playerVelocity * Time.deltaTime);

            if (interactText != null)
                interactText.text = "";

            currentInteractable = null;
            return;
        }

        Vector2 moveInput = input.Movement.ReadValue<Vector2>();
        Vector2 lookInput = input.Look.ReadValue<Vector2>();

        isSprinting = Keyboard.current != null &&
                      Keyboard.current.leftShiftKey.isPressed &&
                      moveInput.y > 0f;

        MoveInput(moveInput);
        LookInput(lookInput);

        CheckForInteractable();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }

        SetAnimations();
    }

    void MoveInput(Vector2 inputValue)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = inputValue.x;
        moveDirection.z = inputValue.y;

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        controller.Move(transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime);

        if (isGrounded && _playerVelocity.y < 0f)
        {
            _playerVelocity.y = -2f;
        }

        _playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(_playerVelocity * Time.deltaTime);
    }

    void LookInput(Vector2 inputValue)
    {
        float mouseX = inputValue.x;
        float mouseY = inputValue.y;

        xRotation -= mouseY * sensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cam != null)
            cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX * sensitivity);
    }

    void CheckForInteractable()
    {
        currentInteractable = null;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactLayer))
        {
            MonoBehaviour[] behaviours = hit.collider.GetComponents<MonoBehaviour>();

            foreach (var behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    currentInteractable = interactable;

                    if (interactText != null)
                        interactText.text = currentInteractable.GetPromptText();

                    return;
                }
            }
        }

        if (interactText != null)
            interactText.text = "";
    }

    void TryInteract()
    {
        if (!IsOwner) return;
        
        if (currentInteractable != null)
        {
            currentInteractable.Interact();

            if (interactText != null)
                interactText.text = currentInteractable.GetPromptText();
        }
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Jump()
    {
        if (isGrounded)
            _playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
    }

    void AssignInputs()
    {
        input.Jump.performed += ctx =>
        {
            if (!IsOwner) return;
            Jump();
        };

        input.Attack.started += ctx =>
        {
            if (!IsOwner) return;
            Attack();
        };
    }

    public void ChangeAnimationState(string newState)
    {
        if (currentAnimationState == newState) return;

        currentAnimationState = newState;
        animator.CrossFadeInFixedTime(currentAnimationState, 0.2f);
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
        if (!IsOwner) return;
        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(swordSwing);

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
        if (!IsOwner)
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
        {
            HitTarget(hit.point);

            if (hit.transform.TryGetComponent<Health>(out Health target))
            {
                target.TakeDamage(attackDamage);
            }
        }
    }

    void HitTarget(Vector3 pos)
    {
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(hitSound);

        GameObject go = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(go, 20f);
    }
}