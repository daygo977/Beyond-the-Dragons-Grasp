using UnityEngine;

public class CharacterAnimationTester : MonoBehaviour
{
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;
    public bool moveObject = true;

    [Header("Ground")]
    public bool isGrounded = true;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f;
        if (Input.GetKey(KeyCode.S)) moveY -= 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;

        Vector2 moveInput = new Vector2(moveX, moveY);

        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        bool isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);

        if (animator != null)
        {
            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsRunning", isRunning);
            animator.SetBool("IsGrounded", isGrounded);

            if (Input.GetKeyDown(KeyCode.Space))
                animator.SetTrigger("Jump");

            if (Input.GetKeyDown(KeyCode.P))
                animator.SetTrigger("Attack");
        }

        if (moveObject && isMoving)
        {
            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            float speed = isRunning ? runSpeed : moveSpeed;

            transform.position += moveDirection * speed * Time.deltaTime;

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }
}