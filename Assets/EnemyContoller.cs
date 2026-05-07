using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Movement")]
    public float runSpeed = 3f;
    public float rotationSpeed = 8f;

    [Header("Patrol")]
    public bool patrolWhenIdle = true;
    public float patrolRadius = 10f;
    public float patrolPointReachDistance = 1f;
    public float patrolWaitTime = 1f;
    public int patrolPointAttempts = 20;

    [Header("Detection")]
    public bool ignoreHeightForDetection = true;
    public float loseInterestRange = 18f;
    public bool debugDetection = false;

    [Header("NavMesh Safety")]
    public float patrolSampleDistance = 4f;
    public float minPatrolPointDistance = 2f;
    public float minWallDistance = 0.7f;
    public float chaseRepathRate = 0.2f;
    public float stuckCheckInterval = 0.5f;
    public float stuckDistance = 0.05f;
    public float stuckTime = 1.5f;
    public bool debugNavMesh = false;

    [Header("Timing")]
    public float screamDuration = 2f;
    public float attackDuration = 1.2f;
    public float damageReactionDuration = 0.6f;

    [Header("Animator Parameters")]
    public string movingBool = "IsMoving";
    public string runningBool = "IsRunning";
    public string deadBool = "IsDead";
    public string screamTrigger = "Scream";
    public string slash01Trigger = "Slash01";
    public string slash02Trigger = "Slash02";
    public string stabTrigger = "Stab";
    public string damageTrigger = "TakeDamage";
    public string dieTrigger = "Die";

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
    private NavMeshAgent agent;

    private bool hasDetectedPlayer;
    private bool hasFinishedScream;
    private bool isActing;
    private bool isReacting;
    private bool isDead;

    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;
    private float patrolWaitTimer;
    private float nextDebugTime;
    private float nextChaseRepathTime;

    private Coroutine actionCoroutine;
    private Coroutine damageCoroutine;

    private Vector3 lastStuckCheckPosition;
    private float nextStuckCheckTime;
    private float stuckTimer;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        characterController = GetComponent<CharacterController>();

        AcquirePlayer();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ConfigureAgent();
        DisableAllHitboxes();
        ResetStuckCheck();
    }

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0.1f, detectionRange);
        attackRange = Mathf.Clamp(attackRange, 0.1f, detectionRange);
        loseInterestRange = Mathf.Max(detectionRange, loseInterestRange);
        patrolRadius = Mathf.Max(0.1f, patrolRadius);
        patrolPointReachDistance = Mathf.Max(0.05f, patrolPointReachDistance);
        patrolPointAttempts = Mathf.Max(1, patrolPointAttempts);
        patrolSampleDistance = Mathf.Max(0.1f, patrolSampleDistance);
        minPatrolPointDistance = Mathf.Max(0f, minPatrolPointDistance);
        minWallDistance = Mathf.Max(0f, minWallDistance);
        chaseRepathRate = Mathf.Max(0.05f, chaseRepathRate);
        stuckCheckInterval = Mathf.Max(0.1f, stuckCheckInterval);
        stuckDistance = Mathf.Max(0.001f, stuckDistance);
        stuckTime = Mathf.Max(stuckCheckInterval, stuckTime);
    }

    private void Update()
    {
        if (isDead) return;

        if (player == null)
            AcquirePlayer();

        ConfigureAgent();

        if (isReacting)
        {
            StopAgent(false);
            SetMovementAnimation(false, false);
            return;
        }

        if (player == null)
        {
            PatrolOrIdle();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        bool playerInDetectionRange = distanceToPlayer <= detectionRange;
        bool playerInAttackRange = distanceToPlayer <= attackRange;
        bool shouldKeepChasing = hasDetectedPlayer && distanceToPlayer <= loseInterestRange;

        if (debugDetection && Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + 0.5f;
            Debug.Log($"{name} distance to player: {distanceToPlayer:F2}. Detection: {detectionRange:F2}. Attack: {attackRange:F2}. Detected: {hasDetectedPlayer}.");
        }

        if (isActing)
        {
            StopAgent(false);
            SetMovementAnimation(false, false);
            return;
        }

        if (!hasDetectedPlayer)
        {
            if (playerInDetectionRange)
            {
                StartAction(ScreamRoutine());
                return;
            }

            PatrolOrIdle();
            return;
        }

        if (!hasFinishedScream)
        {
            StopAgent(false);
            SetMovementAnimation(false, false);
            return;
        }

        if (playerInAttackRange)
        {
            StartAction(AttackRoutine());
            return;
        }

        if (shouldKeepChasing || playerInDetectionRange)
        {
            ChasePlayer();
            return;
        }

        hasDetectedPlayer = false;
        hasFinishedScream = false;
        PatrolOrIdle();
    }

    private void AcquirePlayer()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return;
        }

        GameObject namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
            player = namedPlayer.transform;
    }

    private float GetDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        Vector3 enemyPosition = transform.position;
        Vector3 playerPosition = player.position;

        if (ignoreHeightForDetection)
        {
            enemyPosition.y = 0f;
            playerPosition.y = 0f;
        }

        return Vector3.Distance(enemyPosition, playerPosition);
    }

    private void ConfigureAgent()
    {
        if (agent == null) return;

        agent.speed = runSpeed;
        agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.85f);
        agent.updateRotation = false;
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void StopAgent(bool clearPath)
    {
        if (!CanUseAgent()) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (clearPath)
            agent.ResetPath();

        ResetStuckCheck();
    }

    private void StartAction(IEnumerator routine)
    {
        if (actionCoroutine != null)
            StopCoroutine(actionCoroutine);

        actionCoroutine = StartCoroutine(routine);
    }

    private void CancelCurrentAction()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        isActing = false;
        DisableAllHitboxes();
    }

    private IEnumerator ScreamRoutine()
    {
        hasDetectedPlayer = true;
        hasFinishedScream = false;
        isActing = true;

        StopAgent(true);
        SetMovementAnimation(false, false);

        if (player != null)
            RotateTowards(player.position);

        PlayRandomSound(roarClips, roarVolume);
        SetAnimatorTrigger(screamTrigger);

        yield return new WaitForSeconds(screamDuration);

        hasFinishedScream = true;
        isActing = false;
        actionCoroutine = null;
    }

    private void ChasePlayer()
    {
        hasPatrolPoint = false;
        SetMovementAnimation(true, true);

        if (CanUseAgent())
        {
            agent.isStopped = false;

            if (Time.time >= nextChaseRepathTime || !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                nextChaseRepathTime = Time.time + chaseRepathRate;
                SetDestinationNearPlayer();
            }

            Vector3 lookPoint = agent.steeringTarget;
            if (lookPoint == Vector3.zero && player != null)
                lookPoint = player.position;

            RotateTowards(lookPoint);
            MonitorStuck(false);
            return;
        }

        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        RotateTowards(player.position);
        Vector3 move = direction.normalized * runSpeed * Time.deltaTime;

        if (characterController != null)
            characterController.Move(move);
        else
            transform.position += move;
    }

    private void SetDestinationNearPlayer()
    {
        if (!CanUseAgent() || player == null)
            return;

        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        agent.SetDestination(player.position);
    }

    private void PatrolOrIdle()
    {
        if (!patrolWhenIdle || !CanUseAgent())
        {
            Idle();
            return;
        }

        PerformPatrol();
    }

    private void PerformPatrol()
    {
        if (!hasPatrolPoint)
        {
            SetMovementAnimation(false, false);
            StopAgent(false);

            if (patrolWaitTimer > 0f)
            {
                patrolWaitTimer -= Time.deltaTime;
                return;
            }

            if (!TryFindPatrolPoint())
            {
                Idle();
                return;
            }
        }

        SetMovementAnimation(true, true);
        agent.isStopped = false;

        if (!agent.hasPath || agent.destination != currentPatrolPoint)
            agent.SetDestination(currentPatrolPoint);

        Vector3 lookPoint = agent.steeringTarget;
        if (lookPoint != Vector3.zero)
            RotateTowards(lookPoint);

        MonitorStuck(true);

        if (!agent.pathPending && agent.remainingDistance <= patrolPointReachDistance)
        {
            ClearPatrolPointAndWait();
        }
    }

    private bool TryFindPatrolPoint()
    {
        if (!CanUseAgent())
            return false;

        for (int i = 0; i < patrolPointAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolSampleDistance, NavMesh.AllAreas))
                continue;

            if (Vector3.Distance(transform.position, hit.position) < minPatrolPointDistance)
                continue;

            if (!IsSafeNavMeshPoint(hit.position))
                continue;

            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;

            currentPatrolPoint = hit.position;
            hasPatrolPoint = true;
            ResetStuckCheck();
            return true;
        }

        if (debugNavMesh)
            Debug.LogWarning($"{name} could not find a safe patrol point. Try increasing Patrol Radius/Sample Distance or rebaking the NavMesh.");

        return false;
    }

    private bool IsSafeNavMeshPoint(Vector3 point)
    {
        if (minWallDistance <= 0f)
            return true;

        if (NavMesh.FindClosestEdge(point, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            if (edgeHit.distance < minWallDistance)
                return false;
        }

        return true;
    }

    private void ClearPatrolPointAndWait()
    {
        hasPatrolPoint = false;
        patrolWaitTimer = patrolWaitTime;
        StopAgent(true);
        SetMovementAnimation(false, false);
    }

    private void Idle()
    {
        StopAgent(false);
        SetMovementAnimation(false, false);
    }

    private IEnumerator AttackRoutine()
    {
        isActing = true;
        StopAgent(true);
        SetMovementAnimation(false, false);

        if (player != null)
            RotateTowards(player.position);

        int attackChoice = Random.Range(0, 3);
        PlayRandomSound(swingClips, swingVolume);

        if (attackChoice == 0)
            SetAnimatorTrigger(slash01Trigger);
        else if (attackChoice == 1)
            SetAnimatorTrigger(slash02Trigger);
        else
            SetAnimatorTrigger(stabTrigger);

        yield return new WaitForSeconds(attackDuration);

        DisableAllHitboxes();
        isActing = false;
        actionCoroutine = null;
    }

    public void TakeDamageReaction()
    {
        if (isDead) return;

        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        damageCoroutine = StartCoroutine(TakeDamageRoutine());
    }

    private IEnumerator TakeDamageRoutine()
    {
        CancelCurrentAction();

        isReacting = true;
        hasDetectedPlayer = true;
        hasFinishedScream = true;

        StopAgent(true);
        SetMovementAnimation(false, false);
        DisableAllHitboxes();

        SetAnimatorTrigger(damageTrigger);

        yield return new WaitForSeconds(damageReactionDuration);

        isReacting = false;
        damageCoroutine = null;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        isReacting = false;
        CancelCurrentAction();

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        StopAgent(true);
        if (agent != null)
            agent.enabled = false;

        DisableAllHitboxes();
        SetMovementAnimation(false, false);
        SetAnimatorBool(deadBool, true);
        SetAnimatorTrigger(dieTrigger);

        PlayRandomSound(deathClips, deathVolume);

        StartCoroutine(DeathFadeRoutine());
    }

    private IEnumerator DeathFadeRoutine()
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
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void MonitorStuck(bool patrolMode)
    {
        if (!CanUseAgent() || agent.isStopped || !agent.hasPath)
        {
            ResetStuckCheck();
            return;
        }

        if (Time.time < nextStuckCheckTime)
            return;

        float moved = Vector3.Distance(transform.position, lastStuckCheckPosition);
        bool tryingToMove = agent.remainingDistance > agent.stoppingDistance + 0.2f || agent.desiredVelocity.sqrMagnitude > 0.01f;

        if (tryingToMove && moved < stuckDistance)
            stuckTimer += stuckCheckInterval;
        else
            stuckTimer = 0f;

        lastStuckCheckPosition = transform.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;

        if (stuckTimer < stuckTime)
            return;

        if (debugNavMesh)
            Debug.LogWarning($"{name} looked stuck. Repathing.");

        stuckTimer = 0f;

        if (patrolMode)
            ClearPatrolPointAndWait();
        else
        {
            agent.ResetPath();
            nextChaseRepathTime = 0f;
        }
    }

    private void ResetStuckCheck()
    {
        lastStuckCheckPosition = transform.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
        stuckTimer = 0f;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void SetMovementAnimation(bool moving, bool running)
    {
        SetAnimatorBool(movingBool, moving);
        SetAnimatorBool(runningBool, running);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            animator.SetBool(parameterName, value);
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(parameterName);
        else if (debugNavMesh)
            Debug.LogWarning($"{name} animator does not have trigger parameter '{parameterName}'.");
    }

    private void DisableAllHitboxes()
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

    private void PlayRandomSound(AudioClip[] clips, float volume = 1f)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clip, volume);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, loseInterestRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        if (hasPatrolPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentPatrolPoint, 0.25f);
        }
    }
}