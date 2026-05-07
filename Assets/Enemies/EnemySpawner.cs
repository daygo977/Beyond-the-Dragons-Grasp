using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    [System.Serializable]
    public class EnemyWave
    {
        public string waveName = "Wave";
        public GameObject enemyPrefab;
        [Min(1)] public int enemyCount = 3;
        [Min(0f)] public float spawnDelayBetweenEnemies = 0.35f;
    }

    [Header("Player Detection")]
    public float activationRadius = 18f;
    public LayerMask playerLayers = ~0;
    public string playerTag = "Player";
    public float playerCheckInterval = 0.25f;
    public bool useTagFallbackIfOverlapFindsNone = true;

    [Header("Waves")]
    public List<EnemyWave> waves = new List<EnemyWave>();
    public bool loopWaves = false;
    public float timeBetweenWaves = 2f;
    public bool pauseSpawningWhenNoPlayersInside = true;

    [Header("Spawn Positions")]
    public Transform[] spawnPoints;
    public bool useRandomNavMeshPointIfNoSpawnPoints = true;
    public float randomSpawnRadius = 5f;
    public float navMeshSampleDistance = 3f;
    public int navMeshAttempts = 20;

    [Header("Enemy Setup")]
    public bool assignNearestPlayerToEnemyController = true;
    public bool rotateEnemyTowardNearestPlayer = true;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool showGizmos = true;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private bool playersInside;
    private Transform nearestPlayer;
    private int nextWaveIndex;
    private bool isSpawning;
    private bool hasSpawnedAnyWave;
    private float nextPlayerCheckTime;

    public int AliveEnemyCount => aliveEnemies.Count;
    public bool PlayersInside => playersInside;
    public int NextWaveIndex => nextWaveIndex;

    private void Update()
    {
        //Multiplayer edit
        if (!IsServer) return;

        if (Time.time >= nextPlayerCheckTime)
        {
            nextPlayerCheckTime = Time.time + playerCheckInterval;
            CheckForPlayersInside();
        }

        CleanAliveEnemyList();

        if (!isSpawning && playersInside && aliveEnemies.Count == 0 && HasAnotherWave())
        {
            StartCoroutine(SpawnNextWaveRoutine());
        }
    }

    private bool HasAnotherWave()
    {
        if (waves == null || waves.Count == 0)
            return false;

        return loopWaves || nextWaveIndex < waves.Count;
    }

    private IEnumerator SpawnNextWaveRoutine()
    {
        if (waves == null || waves.Count == 0)
            yield break;

        isSpawning = true;

        if (nextWaveIndex >= waves.Count)
        {
            if (loopWaves)
                nextWaveIndex = 0;
            else
            {
                isSpawning = false;
                yield break;
            }
        }

        if (hasSpawnedAnyWave && timeBetweenWaves > 0f)
        {
            float timer = 0f;

            while (timer < timeBetweenWaves)
            {
                if (!pauseSpawningWhenNoPlayersInside || playersInside)
                    timer += Time.deltaTime;

                yield return null;
            }
        }

        EnemyWave wave = waves[nextWaveIndex];

        if (debugLogs)
            Debug.Log($"{name} spawning {wave.waveName} with {wave.enemyCount} enemies.");

        for (int i = 0; i < wave.enemyCount; i++)
        {
            while (pauseSpawningWhenNoPlayersInside && !playersInside)
                yield return null;

            if (wave.enemyPrefab != null)
                SpawnEnemy(wave.enemyPrefab);
            else
                Debug.LogWarning($"{name} has a missing enemy prefab in {wave.waveName}.");

            if (wave.spawnDelayBetweenEnemies > 0f)
                yield return new WaitForSeconds(wave.spawnDelayBetweenEnemies);
        }

        hasSpawnedAnyWave = true;
        nextWaveIndex++;
        isSpawning = false;
    }
    
    //Mutltiplayer edit, new logic
    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (!IsServer) return;

        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation(spawnPosition);

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, spawnRotation);

        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError($"{enemyPrefab.name} is missing NetworkObject. Enemy cannot be network spawned.");
            Destroy(enemy);
            return;
        }

        networkObject.Spawn(true);

        aliveEnemies.Add(enemy);

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        if (assignNearestPlayerToEnemyController)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();

            if (enemyController != null)
                enemyController.player = nearestPlayer != null ? nearestPlayer : FindNearestPlayerByTagOnly();
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (spawnPoint == null)
                    continue;

                if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                    return hit.position;

                return spawnPoint.position;
            }
        }

        if (useRandomNavMeshPointIfNoSpawnPoints)
        {
            for (int i = 0; i < navMeshAttempts; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * randomSpawnRadius;
                Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                    return hit.position;
            }
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit centerHit, navMeshSampleDistance, NavMesh.AllAreas))
            return centerHit.position;

        return transform.position;
    }

    private Quaternion GetSpawnRotation(Vector3 spawnPosition)
    {
        if (!rotateEnemyTowardNearestPlayer || nearestPlayer == null)
            return transform.rotation;

        Vector3 direction = nearestPlayer.position - spawnPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return transform.rotation;

        return Quaternion.LookRotation(direction.normalized);
    }

    private void CleanAliveEnemyList()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = aliveEnemies[i];

            if (enemy == null)
            {
                aliveEnemies.RemoveAt(i);
                continue;
            }

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null && health.IsDead)
                aliveEnemies.RemoveAt(i);
        }
    }

    //Multiplayer edit, new logic
    private void CheckForPlayersInside()
    {
        playersInside = false;
        nearestPlayer = null;
        float nearestDistanceSquared = activationRadius * activationRadius;

        if (NetworkManager.Singleton != null)
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                Transform player = client.PlayerObject.transform;

                Health health = player.GetComponent<Health>();
                if (health != null && health.IsDead)
                    continue;

                float distanceSquared = (player.position - transform.position).sqrMagnitude;

                if (distanceSquared <= nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestPlayer = player;
                    playersInside = true;
                }
            }
        }

        if (!playersInside && useTagFallbackIfOverlapFindsNone)
        {
            Transform fallbackPlayer = FindNearestPlayerByTagOnly();

            if (fallbackPlayer != null)
            {
                float distance = Vector3.Distance(transform.position, fallbackPlayer.position);

                if (distance <= activationRadius)
                {
                    nearestPlayer = fallbackPlayer;
                    playersInside = true;
                }
            }
        }
    }

    private Transform FindTaggedParent(Transform start, string tagToFind)
    {
        if (string.IsNullOrWhiteSpace(tagToFind))
            return null;

        Transform current = start;
        while (current != null)
        {
            if (current.CompareTag(tagToFind))
                return current;

            current = current.parent;
        }

        return null;
    }

    private Transform FindNearestPlayerByTagOnly()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            return null;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        Transform nearest = null;
        float nearestDistanceSquared = Mathf.Infinity;

        foreach (GameObject playerObject in players)
        {
            float distanceSquared = (playerObject.transform.position - transform.position).sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearest = playerObject.transform;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, randomSpawnRadius);

        if (spawnPoints == null)
            return;

        Gizmos.color = Color.yellow;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            Gizmos.DrawSphere(spawnPoint.position, 0.25f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}