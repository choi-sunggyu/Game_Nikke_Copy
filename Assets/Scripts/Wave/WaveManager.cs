using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private ObjectPool enemyBulletPool;
    [SerializeField] private WaveData easyData;
    [SerializeField] private WaveData normalData;
    [SerializeField] private WaveData hardData;

    [Header("Spawn Range")]
    [SerializeField] private float minZ = 10f;
    [SerializeField] private float maxZ = 50f;
    [SerializeField] private float minXAtMinZ = 13f;
    [SerializeField] private float maxXAtMaxZ = 30f;
    [SerializeField] private float minYAtMinZ = -2f;
    [SerializeField] private float maxYAtMaxZ = 10f;

    [Header("Overlap Prevention")]
    [SerializeField] private float minEnemyDistance = 3f; // 적 간 최소 거리
    [SerializeField] private int maxPlacementAttempts = 30; // 겹침 방지 최대 시도 횟수

    private WaveData currentData;
    private int currentWaveIndex = 0;
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();

    private float waveClearDelay = 2f;
    private float spawnInterval = 2f;

    public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

    public enum Difficulty { Easy, Normal, Hard }

    void Start()
    {
        StartGame(GameSettings.SelectedDifficulty);
    }

    public void StartGame(Difficulty difficulty)
    {
        currentData = difficulty switch
        {
            Difficulty.Easy   => easyData,
            Difficulty.Normal => normalData,
            Difficulty.Hard   => hardData,
            _                 => easyData
        };

        currentWaveIndex = 0;
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        while (currentWaveIndex < currentData.waves.Count)
        {
            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} 시작");

            yield return StartCoroutine(SpawnWave(currentData.waves[currentWaveIndex]));

            yield return StartCoroutine(WaitForWaveClear());

            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} 클리어");

            currentWaveIndex++;

            if (currentWaveIndex < currentData.waves.Count)
            {
                Debug.Log($"[WaveManager] 다음 웨이브까지 {waveClearDelay}초 대기");
                yield return new WaitForSeconds(waveClearDelay);
            }
        }

        Debug.Log("[WaveManager] 모든 웨이브 클리어!");
    }

    IEnumerator SpawnWave(WaveData.Wave wave)
    {
        foreach (var spawnInfo in wave.enemies)
        {
            for (int i = 0; i < spawnInfo.count; i++)
            {
                SpawnEnemy(spawnInfo.enemyPrefab);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    void SpawnEnemy(GameObject prefab)
    {
        Vector3 targetPos = GetNonOverlappingPosition();

        // 적 타입에 따라 초기 위치 결정
        Vector3 spawnPos;
        EnemyBase prefabEnemy = prefab.GetComponent<EnemyBase>();

        if (prefabEnemy is EnemyA)
        {
            // EnemyA: 화면 위에서 낙하
            float offScreenY = GetOffScreenY(targetPos.z);
            spawnPos = new Vector3(targetPos.x, offScreenY, targetPos.z);
        }
        else if (prefabEnemy is EnemyB)
        {
            // EnemyB: 좌/우 랜덤 등장
            float offScreenX = GetOffScreenX(targetPos.z);
            float side = Random.value > 0.5f ? 1f : -1f;
            spawnPos = new Vector3(offScreenX * side, targetPos.y, targetPos.z);
        }
        else
        {
            // EnemyC 등 다른 타입: 목표 위치에 바로 생성
            spawnPos = targetPos;
        }

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        enemy.OnDied += () => activeEnemies.Remove(enemy);
        if (enemy != null)
        {
            enemy.SetBulletPool(enemyBulletPool);
            enemy.SetTargetPosition(targetPos); // 목표 위치 전달
            activeEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Z에 비례하는 범위 내 랜덤 위치를 생성하되, 기존 적과 겹치지 않는 위치를 반환
    /// </summary>
    Vector3 GetNonOverlappingPosition()
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GetRandomSpawnPosition();

            bool overlapping = false;
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                // 목표 위치 기준으로 거리 비교
                if (Vector3.Distance(candidate, enemy.TargetPosition) < minEnemyDistance)
                {
                    overlapping = true;
                    break;
                }
            }

            if (!overlapping)
                return candidate;
        }

        // 최대 시도 초과 시 그냥 랜덤 위치 반환
        Debug.LogWarning("[WaveManager] 겹치지 않는 위치를 찾지 못해 랜덤 배치");
        return GetRandomSpawnPosition();
    }

    /// <summary>
    /// Z=10~50 범위에서 Z에 비례한 X, Y 범위 내 랜덤 위치 계산
    /// </summary>
    Vector3 GetRandomSpawnPosition()
    {
        float z = Random.Range(minZ, maxZ);
        float t = (z - minZ) / (maxZ - minZ); // 0~1 보간값

        // Z에 비례하여 X 범위 확대
        float maxX = Mathf.Lerp(minXAtMinZ, maxXAtMaxZ, t);
        float x = Random.Range(-maxX, maxX);

        // Z에 비례하여 Y 범위 확대
        float minY = Mathf.Lerp(minYAtMinZ, 0f, t);
        float maxY = Mathf.Lerp(minYAtMinZ, maxYAtMaxZ, t);
        float y = Random.Range(minY, maxY);

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 해당 Z에서 화면 상단 밖 Y 좌표 계산
    /// </summary>
    float GetOffScreenY(float z)
    {
        Camera cam = Camera.main;
        float distanceFromCam = z - cam.transform.position.z;
        Vector3 topWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, distanceFromCam));
        return topWorld.y + 5f;
    }

    /// <summary>
    /// 해당 Z에서 화면 측면 밖 X 좌표 계산
    /// </summary>
    float GetOffScreenX(float z)
    {
        Camera cam = Camera.main;
        float distanceFromCam = z - cam.transform.position.z;
        Vector3 rightWorld = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, distanceFromCam));
        return Mathf.Abs(rightWorld.x) + 5f;
    }

    IEnumerator WaitForWaveClear()
    {
        while (true)
        {
            activeEnemies.RemoveAll(e => e == null || !e.IsAlive);

            if (activeEnemies.Count == 0)
                yield break;

            yield return new WaitForSeconds(0.5f);
        }
    }
}