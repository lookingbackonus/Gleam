using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DandelionSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public dandelionSeed seedPrefab;
    public int seedsPerGroup = 4;
    public float spawnHeight = 0.5f;
    public float circleRadius = 8f;

    [Header("스폰 타이밍")]
    public float spawnInterval = 3f;
    public float spawnStartDelay = 1f;
    public bool useAutoSpawn = true;

    [Header("바람 시스템")]
    public float windInterval = 5f;
    public float windDuration = 2f;
    public float windStartDelay = 3f;
    public float windStrengthMin = 1f;
    public float windStrengthMax = 3f;
    [Range(0f, 90f)]
    public float windDirectionVariation = 30f;

    [Header("무지개 시스템")]
    public int collisionThreshold = 10;
    public RainbowManager rainbowManager;

    [Header("패턴 설정")]
    public bool useRandomPatterns = true;
    public SpawnPattern[] randomPatternList = { SpawnPattern.Circle, SpawnPattern.Fan, SpawnPattern.Arc };

    public enum SpawnPattern { Fan, Arc, Circle }

    public System.Action<int> OnCollisionCountChanged;
    public System.Action OnThresholdReached;

    private Transform player;
    private List<dandelionSeed> currentSeeds = new List<dandelionSeed>();
    private int collisionCount = 0;
    private bool hasStartedSpawning = false;
    private bool windSystemStarted = false;
    private bool rainbowActivated = false;
    private SpawnPattern currentPattern = SpawnPattern.Circle;

    #region Unity Lifecycle
    void Start()
    {
        InitializePlayer();
        InitializeRainbowManager();
        OnCollisionCountChanged?.Invoke(collisionCount);
    }

    void Update()
    {
        CleanupDestroyedSeeds();
    }

    void OnDestroy()
    {
        StopWindSystem();

        if (player != null)
        {
            CollisionDetector detector = player.GetComponent<CollisionDetector>();
            if (detector != null)
            {
                detector.OnCollisionWithTag -= OnPlayerCollisionWithStage;
            }
        }
    }
    #endregion

    #region Initialization
    void InitializePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            SetupCollisionDetector(playerObj);
        }
        else
        {
            Debug.LogError("[DandelionSpawner] Player를 찾을 수 없습니다!");
        }
    }

    void InitializeRainbowManager()
    {
        if (rainbowManager == null)
        {
            rainbowManager = GameObject.FindFirstObjectByType<RainbowManager>();
            if (rainbowManager == null)
            {
                Debug.LogWarning("[DandelionSpawner] RainbowManager를 찾을 수 없습니다!");
            }
        }
    }

    void SetupCollisionDetector(GameObject playerObj)
    {
        CollisionDetector detector = playerObj.GetComponent<CollisionDetector>();
        if (detector == null)
        {
            detector = playerObj.AddComponent<CollisionDetector>();
        }

        detector.OnCollisionWithTag -= OnPlayerCollisionWithStage;
        detector.OnCollisionWithTag += OnPlayerCollisionWithStage;
    }
    #endregion

    #region Player Collision
    void OnPlayerCollisionWithStage(string tag)
    {
        if (tag == "DandelionStage" && !hasStartedSpawning)
        {
            StartSpawning();
        }
    }
    #endregion

    #region Spawning
    public void StartSpawning()
    {
        if (hasStartedSpawning) return;

        hasStartedSpawning = true;
        Debug.Log("[DandelionSpawner] 홀씨 스폰 시작");

        if (useAutoSpawn) StartAutoSpawn();
        if (!windSystemStarted) StartWindSystem();
    }

    void StartAutoSpawn()
    {
        if (IsInvoking(nameof(SpawnNextGroup))) return;

        Invoke(nameof(SpawnNextGroup), spawnStartDelay);
        InvokeRepeating(nameof(SpawnNextGroup), spawnStartDelay + spawnInterval, spawnInterval);
    }

    public void SpawnNextGroup()
    {
        if (player == null || !hasStartedSpawning || rainbowActivated) return;

        SelectRandomPattern();
        Vector3 spawnCenter = CalculateSpawnCenter();
        SpawnSeedGroup(spawnCenter);
    }

    void SelectRandomPattern()
    {
        if (useRandomPatterns && randomPatternList.Length > 0)
        {
            currentPattern = randomPatternList[Random.Range(0, randomPatternList.Length)];
        }
    }

    Vector3 CalculateSpawnCenter()
    {
        Vector3 spawnCenter = player.position;
        spawnCenter.y += spawnHeight;
        return spawnCenter;
    }

    void SpawnSeedGroup(Vector3 centerPosition)
    {
        for (int i = 0; i < seedsPerGroup; i++)
        {
            Vector3 spawnPos = CalculateSpawnPosition(centerPosition, i);
            GameObject seedObj = Instantiate(seedPrefab.gameObject, spawnPos, Quaternion.identity);

            dandelionSeed seed = seedObj.GetComponent<dandelionSeed>();
            if (seed != null)
            {
                currentSeeds.Add(seed);
                SetupSeedCollisionDetection(seedObj);
            }
        }
    }

    Vector3 CalculateSpawnPosition(Vector3 center, int index)
    {
        Vector3 position = currentPattern switch
        {
            SpawnPattern.Fan => CalculateFanPosition(center, index),
            SpawnPattern.Arc => CalculateArcPosition(center, index),
            SpawnPattern.Circle => CalculateCirclePosition(center, index),
            _ => center
        };

        return position;
    }

    Vector3 CalculateCirclePosition(Vector3 center, int index)
    {
        float angleStep = 360f / seedsPerGroup;
        float angle = angleStep * index * Mathf.Deg2Rad;

        Vector3 circlePos = new Vector3(
            Mathf.Cos(angle) * circleRadius,
            0,
            Mathf.Sin(angle) * circleRadius
        );

        return center + circlePos;
    }

    Vector3 CalculateFanPosition(Vector3 center, int index)
    {
        float fanAngle = 90f;
        float spacing = 4f;
        float angleStep = fanAngle / (seedsPerGroup - 1);
        float angle = (-fanAngle * 0.5f + angleStep * index) * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        return center + direction * spacing;
    }

    Vector3 CalculateArcPosition(Vector3 center, int index)
    {
        float fanAngle = 90f;
        float radius = 6f;
        float angleStep = fanAngle / (seedsPerGroup - 1);
        float angle = (-fanAngle * 0.5f + angleStep * index) * Mathf.Deg2Rad;

        Vector3 arcPos = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
        return center + arcPos;
    }
    #endregion

    #region Seed Collision
    void SetupSeedCollisionDetection(GameObject seedObj)
    {
        if (seedObj.tag == "Untagged")
            seedObj.tag = "DandelionSeed";

        if (seedObj.GetComponent<Collider>() == null)
        {
            SphereCollider collider = seedObj.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 2f;
        }
    }

    public void OnPlayerSeedCollision()
    {
        if (rainbowActivated) return;

        collisionCount++;
        OnCollisionCountChanged?.Invoke(collisionCount);

        if (collisionCount >= collisionThreshold)
        {
            TriggerRainbow();
        }
    }
    #endregion

    #region Rainbow Handling
    void TriggerRainbow()
    {
        OnThresholdReached?.Invoke();

        if (rainbowManager != null)
        {
            rainbowManager.ActivateRainbow();
            rainbowActivated = true;
            Debug.Log("[DandelionSpawner] 무지개 영구 활성화!");

            // 📌 즉시 모든 시스템 정지 및 홀씨 제거
            ImmediateCleanupForRainbow();
        }
        else
        {
            Debug.LogWarning("[DandelionSpawner] RainbowManager가 없어서 무지개를 활성화할 수 없습니다!");
        }
    }

    void ImmediateCleanupForRainbow()
    {
        Debug.Log("[DandelionSpawner] 무지개 연출을 위한 즉시 정리 시작");

        // 1. 모든 스폰 시스템 즉시 중지
        StopAutoSpawn();

        // 2. 바람 시스템 즉시 중지
        StopWindSystem();

        // 3. 현재 씬의 모든 홀씨 즉시 제거 (페이드 없이)
        ImmediateDestroyAllSeeds();

        Debug.Log("[DandelionSpawner] 무지개 연출을 위한 즉시 정리 완료");
    }

    void ImmediateDestroyAllSeeds()
    {
        // 현재 스포너가 관리하는 홀씨들 즉시 제거
        foreach (var seed in currentSeeds)
        {
            if (seed != null)
            {
                Destroy(seed.gameObject);
            }
        }
        currentSeeds.Clear();

        // 혹시 놓친 홀씨들이 있을 수 있으니 씬 전체에서 찾아서 제거
        dandelionSeed[] allSeedsInScene = FindObjectsByType<dandelionSeed>(FindObjectsSortMode.None);
        foreach (var seed in allSeedsInScene)
        {
            if (seed != null)
            {
                Destroy(seed.gameObject);
            }
        }

        Debug.Log($"[DandelionSpawner] 씬의 모든 홀씨 즉시 제거 완료 (총 {allSeedsInScene.Length}개)");
    }
    #endregion

    #region Wind System
    void StartWindSystem()
    {
        if (windSystemStarted) return;

        windSystemStarted = true;
        Invoke(nameof(TriggerWind), windStartDelay);
    }

    void TriggerWind()
    {
        // 무지개 활성화되면 바람 시스템 작동 안 함
        if (rainbowActivated) return;

        if (currentSeeds.Count == 0)
        {
            if (windSystemStarted && !rainbowActivated)
                Invoke(nameof(TriggerWind), windInterval);
            return;
        }

        Vector3 windDirection = GenerateWindDirection();
        float windStrength = Random.Range(windStrengthMin, windStrengthMax);

        ApplyWindToAll(windDirection, windStrength);

        Invoke(nameof(StopCurrentWind), windDuration);
        if (windSystemStarted && !rainbowActivated)
            Invoke(nameof(TriggerWind), windDuration + windInterval);
    }

    Vector3 GenerateWindDirection()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle)).normalized;
    }

    void ApplyWindToAll(Vector3 windDirection, float windStrength)
    {
        foreach (var seed in currentSeeds)
        {
            if (seed != null)
            {
                Vector3 individualWindDir = GenerateIndividualWindDirection(windDirection);
                seed.ApplyWind(individualWindDir, windStrength);
            }
        }
    }

    Vector3 GenerateIndividualWindDirection(Vector3 baseDirection)
    {
        float deviationAngle = Random.Range(-windDirectionVariation, windDirectionVariation) * Mathf.Deg2Rad;
        Vector3 perpendicular = Vector3.Cross(baseDirection, Vector3.up);

        Vector3 deviatedDirection = baseDirection * Mathf.Cos(deviationAngle) +
                                   perpendicular * Mathf.Sin(deviationAngle);

        return deviatedDirection.normalized;
    }

    void StopCurrentWind() { }

    public void StopWindSystem()
    {
        windSystemStarted = false;
        CancelInvoke(nameof(TriggerWind));
        CancelInvoke(nameof(StopCurrentWind));
        Debug.Log("[DandelionSpawner] 바람 시스템 완전 중지");
    }
    #endregion

    #region Utility
    void CleanupDestroyedSeeds()
    {
        // 무지개 활성화되면 정리도 안 함 (어차피 모든 홀씨가 제거됨)
        if (rainbowActivated) return;

        for (int i = currentSeeds.Count - 1; i >= 0; i--)
        {
            var seed = currentSeeds[i];
            if (seed == null || seed.transform.position.y < -10f)
            {
                if (seed != null) Destroy(seed.gameObject);
                currentSeeds.RemoveAt(i);
            }
        }
    }

    public void StopAutoSpawn()
    {
        CancelInvoke(nameof(SpawnNextGroup));
        useAutoSpawn = false;
        Debug.Log("[DandelionSpawner] 자동 스폰 완전 중지");
    }

    public void ClearAllSeeds()
    {
        foreach (var seed in currentSeeds)
        {
            if (seed != null) seed.ForceDestroy();
        }
        currentSeeds.Clear();
    }

    public int GetCurrentSeedCount() => currentSeeds.Count;
    public bool HasStartedSpawning() => hasStartedSpawning;
    public int GetCollisionCount() => collisionCount;
    public int GetCollisionThreshold() => collisionThreshold;
    public SpawnPattern GetCurrentPattern() => currentPattern;
    public void SetCollisionThreshold(int newThreshold) => collisionThreshold = Mathf.Max(1, newThreshold);
    public void ManualRainbowTrigger() => TriggerRainbow();
    public bool IsRainbowActivated() => rainbowActivated;

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector3 spawnCenter = CalculateSpawnCenter();

        if (currentPattern == SpawnPattern.Circle)
        {
            Gizmos.color = Color.cyan;
            DrawCircleGizmo(spawnCenter, circleRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnCenter, 0.5f);

        Gizmos.color = Color.green;
        for (int i = 0; i < seedsPerGroup; i++)
        {
            Vector3 pos = CalculateSpawnPosition(spawnCenter, i);
            Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }

    void DrawCircleGizmo(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = angleStep * i * Mathf.Deg2Rad;
            float angle2 = angleStep * (i + 1) * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Gizmos.DrawLine(point1, point2);
        }
    }
    #endregion
}