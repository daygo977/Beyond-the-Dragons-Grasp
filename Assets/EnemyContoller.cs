using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Movement")]
    public float runSpeed = 3f;
    public float rotationSpeed = 8f;

    [Header("Timing")]
    public float screamDuration = 2f;
    public float attackDuration = 1.2f;
    public float damageReactionDuration = 0.6f;

    [Header("Attack Hitboxes")]
    public GameObject slash01Hitbox;
    public GameObject slash02Hitbox;
    public GameObject stabHitbox;

    [Header("Death Fade")]
    public float destroyAfterDeathDelay = 5f;
    public float fadeDuration = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] deathClips;
    public AudioClip[] roarClips;
    public AudioClip[] swingClips;
    [Range(0f, 1f)] public float deathVolume = 1f;
    [Range(0f, 1f)] public float roarVolume = 1f;
    [Range(0f, 1f)] public float swingVolume = 1f;

    private Animator animator;
    private CharacterController characterController;

    private bool hasDetectedPlayer;
    private bool hasFinishedScream;
    private bool isBusy;
    private bool isDead;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        DisableAllHitboxes();
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!hasDetectedPlayer && distanceToPlayer <= detectionRange)
        {
            StartCoroutine(ScreamRoutine());
            return;
        }

        if (!hasFinishedScream)
        {
            SetMovementAnimation(false, false);
            return;
        }

        if (isBusy)
        {
            SetMovementAnimation(false, false);
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            StartCoroutine(AttackRoutine());
        }
        else if (distanceToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Idle();
        }
    }

    IEnumerator ScreamRoutine()
    {
        hasDetectedPlayer = true;
        isBusy = true;

        SetMovementAnimation(false, false);

        PlayRandomSound(roarClips, roarVolume);

        if (animator != null)
            animator.SetTrigger("Scream");

        yield return new WaitForSeconds(screamDuration);

        hasFinishedScream = true;
        isBusy = false;
    }

    void ChasePlayer()
    {
        SetMovementAnimation(true, true);

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        Vector3 move = direction.normalized * runSpeed * Time.deltaTime;

        if (characterController != null)
            characterController.Move(move);
        else
            transform.position += move;
    }

    void Idle()
    {
        SetMovementAnimation(false, false);
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true;
        SetMovementAnimation(false, false);

        Vector3 lookTarget = player.position - transform.position;
        lookTarget.y = 0f;

        if (lookTarget.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookTarget);

        int attackChoice = Random.Range(0, 3);

        PlayRandomSound(swingClips, swingVolume);

        if (animator != null)
        {
            if (attackChoice == 0)
                animator.SetTrigger("Slash01");
            else if (attackChoice == 1)
                animator.SetTrigger("Slash02");
            else
                animator.SetTrigger("Stab");
        }

        yield return new WaitForSeconds(attackDuration);

        DisableAllHitboxes();
        isBusy = false;
    }

    public void TakeDamageReaction()
    {
        if (isDead) return;
        if (isBusy) return;

        StartCoroutine(TakeDamageRoutine());
    }

    IEnumerator TakeDamageRoutine()
    {
        isBusy = true;
        SetMovementAnimation(false, false);
        DisableAllHitboxes();

        if (animator != null)
            animator.SetTrigger("TakeDamage");

        yield return new WaitForSeconds(damageReactionDuration);

        isBusy = false;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        isBusy = true;

        DisableAllHitboxes();
        SetMovementAnimation(false, false);

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");
        }

        PlayRandomSound(deathClips, deathVolume);

        StartCoroutine(DeathFadeRoutine());
    }

    IEnumerator DeathFadeRoutine()
    {
        yield return new WaitForSeconds(destroyAfterDeathDelay);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);

            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void SetMovementAnimation(bool moving, bool running)
    {
        if (animator == null) return;

        animator.SetBool("IsMoving", moving);
        animator.SetBool("IsRunning", running);
    }

    void DisableAllHitboxes()
    {
        if (slash01Hitbox != null)
            slash01Hitbox.SetActive(false);

        if (slash02Hitbox != null)
            slash02Hitbox.SetActive(false);

        if (stabHitbox != null)
            stabHitbox.SetActive(false);
    }

    public void EnableSlash01Hitbox()
    {
        DisableAllHitboxes();

        if (slash01Hitbox != null)
            slash01Hitbox.SetActive(true);
    }

    public void EnableSlash02Hitbox()
    {
        DisableAllHitboxes();

        if (slash02Hitbox != null)
            slash02Hitbox.SetActive(true);
    }

    public void EnableStabHitbox()
    {
        DisableAllHitboxes();

        if (stabHitbox != null)
            stabHitbox.SetActive(true);
    }

    public void DisableHitboxes()
    {
        DisableAllHitboxes();
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
}