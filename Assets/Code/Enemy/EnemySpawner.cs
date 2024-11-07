using UnityEngine;
using System.Collections;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using static SwitchingWeapon;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyType
    {
        public string tag;
        public float spawnWeight;
        public int maxCount;
        public float cooldown;
        [System.NonSerialized]
        public int currentCount;
        [System.NonSerialized]
        public float lastSpawnTime;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Transform point;
        public float weight = 1f;
    }

    #region SerializeField
    [SerializeField] private EnemyType[] enemyTypes;
    [SerializeField] private SpawnPoint[] spawnPoints;
    [SerializeField] private float baseSpawnInterval = 5f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnRadius = 10f;
    #endregion

    #region private
    private float currentSpawnInterval;
    private int currentEnemyCount = 0;
    private float nextSpawnTime;
    private EnemyManager enemyManager;
    private NativeArray<float> spawnPointWeights;
    private NativeArray<float> enemyTypeWeights;
    private NativeArray<int> spawnResults;
    #endregion

    private void Awake()
    {
        enemyManager = FindObjectOfType<EnemyManager>();
        currentSpawnInterval = baseSpawnInterval;
        StartCoroutine(SpawnEnemyRoutine());
    }

    private void OnEnable()
    {
        InitializeNativeArrays();
        // Đăng ký các sự kiện
        GameEvents.OnAmmoPickup += OnAmmoPickedUp;
        GameEvents.OnHealthPickup += OnHealthPickedUp;
        GameEvents.OnPlayerHit += OnPlayerHit;
    }

    private void OnDisable()
    {
        DisposeNativeArrays();
        // Hủy đăng ký các sự kiện
        GameEvents.OnAmmoPickup -= OnAmmoPickedUp;
        GameEvents.OnHealthPickup -= OnHealthPickedUp;
        GameEvents.OnPlayerHit -= OnPlayerHit;
    }

    private void InitializeNativeArrays()
    {
        spawnPointWeights = new NativeArray<float>(spawnPoints.Length, Allocator.Persistent);
        enemyTypeWeights = new NativeArray<float>(enemyTypes.Length, Allocator.Persistent);
        spawnResults = new NativeArray<int>(2, Allocator.Persistent);

        for (int i = 0; i < spawnPoints.Length; i++)
            spawnPointWeights[i] = spawnPoints[i].weight;

        for (int i = 0; i < enemyTypes.Length; i++)
            enemyTypeWeights[i] = enemyTypes[i].spawnWeight;
    }

    private void DisposeNativeArrays()
    {
        if (spawnPointWeights.IsCreated) spawnPointWeights.Dispose();
        if (enemyTypeWeights.IsCreated) enemyTypeWeights.Dispose();
        if (spawnResults.IsCreated) spawnResults.Dispose();
    }

    private IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(currentSpawnInterval);

            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    [BurstCompile]
    private struct SpawnCalculationJob : IJob
    {
        [ReadOnly] public NativeArray<float> spawnPointWeights;
        [ReadOnly] public NativeArray<float> availableTypeWeights;
        public NativeArray<int> results;
        public Unity.Mathematics.Random random;

        public void Execute()
        {
            results[0] = CalculateWeightedIndex(spawnPointWeights, random.NextFloat());
            results[1] = CalculateWeightedIndex(availableTypeWeights, random.NextFloat());
        }

        private readonly int CalculateWeightedIndex(NativeArray<float> weights, float randomValue)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
                totalWeight += weights[i];

            float accumulator = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                accumulator += weights[i] / totalWeight;
                if (randomValue <= accumulator)
                    return i;
            }
            return weights.Length - 1;
        }
    }

    private void SpawnEnemy()
    {
        if (Time.time < nextSpawnTime || currentEnemyCount >= maxEnemies) return;

        // Cập nhật weights cho available types
        UpdateAvailableTypeWeights();

        var job = new SpawnCalculationJob
        {
            spawnPointWeights = spawnPointWeights,
            availableTypeWeights = enemyTypeWeights,
            results = spawnResults,
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks)
        };

        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();

        int selectedTypeIndex = spawnResults[1];
        if (CanSpawnEnemyType(selectedTypeIndex))
        {
            Transform spawnPoint = spawnPoints[spawnResults[0]].point;
            SpawnEnemyAtPosition(selectedTypeIndex, spawnPoint);
            UpdateSpawnTimers(selectedTypeIndex);
        }
    }

    private void UpdateAvailableTypeWeights()
    {
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            float weight = enemyTypes[i].currentCount < enemyTypes[i].maxCount &&
                          Time.time >= enemyTypes[i].lastSpawnTime + enemyTypes[i].cooldown
                          ? enemyTypes[i].spawnWeight : 0f;
            enemyTypeWeights[i] = weight;
        }
    }

    private bool CanSpawnEnemyType(int typeIndex)
    {
        return enemyTypes[typeIndex].currentCount < enemyTypes[typeIndex].maxCount &&
               Time.time >= enemyTypes[typeIndex].lastSpawnTime + enemyTypes[typeIndex].cooldown;
    }

    private void SpawnEnemyAtPosition(int typeIndex, Transform spawnPoint)
    {
        GameObject enemy = enemyManager.SpawnFromPool(
            enemyTypes[typeIndex].tag,
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (enemy != null)
        {
            currentEnemyCount++;
            enemyTypes[typeIndex].currentCount++;
        }
    }

    private void UpdateSpawnTimers(int typeIndex)
    {
        enemyTypes[typeIndex].lastSpawnTime = Time.time;
        nextSpawnTime = Time.time + baseSpawnInterval;
    }

    private void OnAmmoPickedUp(AmmoType ammoType, int amount)
    {
        // Tăng tốc độ sinh kẻ thù khi người chơi nhặt đạn
        currentSpawnInterval = Mathf.Max(currentSpawnInterval - 0.1f, 1f);
        Debug.Log($"Player is pick up Ammo! Spawn interval decreased to {currentSpawnInterval}");
    }

    private void OnHealthPickedUp(float amount)
    {
        // Giảm tốc độ sinh kẻ thù khi người chơi nhặt máu
        currentSpawnInterval = Mathf.Min(currentSpawnInterval + 0.2f, baseSpawnInterval * 2);
        Debug.Log($"Player is pick up Health! Spawn interval increased to {currentSpawnInterval}");
    }

    private void OnPlayerHit(float damage, float penetration)
    {
        // Tạm thời tăng tốc độ sinh kẻ thù khi người chơi bị thương
        StartCoroutine(TemporarilyIncreaseSpawnRate());
    }

    private IEnumerator TemporarilyIncreaseSpawnRate()
    {
        float originalInterval = currentSpawnInterval;
        currentSpawnInterval = Mathf.Max(currentSpawnInterval - 0.5f, 0.5f);
        Debug.Log($"Player hit! Spawn interval temporarily decreased to {currentSpawnInterval}");

        yield return new WaitForSeconds(5f);

        currentSpawnInterval = originalInterval;
        Debug.Log($"Spawn interval restored to {currentSpawnInterval}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint.point != null)
                {
                    Gizmos.DrawSphere(spawnPoint.point.position, 0.5f);
                }
            }
        }
        else
        {
            // Vẽ sphere cũ nếu không có điểm sinh nào được định nghĩa
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}