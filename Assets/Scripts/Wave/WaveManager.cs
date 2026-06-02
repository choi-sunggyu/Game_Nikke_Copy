using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
    [SerializeField] private float minEnemyDistance    = 3f;
    [SerializeField] private int   maxPlacementAttempts = 30;

    // ═══════════════════════════════════════════════════════
    //  이벤트
    // ═══════════════════════════════════════════════════════
    public static event Action OnStageClear;

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private WaveData        currentData;
    private int             currentWaveIndex = 0;
    private List<EnemyBase> activeEnemies    = new List<EnemyBase>();

    private const float waveClearDelay = 2f;
    private const float spawnInterval  = 2f;

    public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

    public enum Difficulty { Easy, Normal, Hard , Boss}

    // ═══════════════════════════════════════════════════════
    //  인트로 이벤트 구독
    // ═══════════════════════════════════════════════════════
    void OnEnable()
    {
        BattleIntroManager.OnBattleIntroComplete += OnIntroComplete;
    }

    void OnDisable()
    {
        BattleIntroManager.OnBattleIntroComplete -= OnIntroComplete;
    }

    /// <summary>
    /// 인트로 완료 시 호출 — 적 공격 AI 활성화
    /// </summary>
    void OnIntroComplete()
    {
        StartCoroutine(DelayedBattleStart());
    }

    IEnumerator DelayedBattleStart()
    {
        yield return new WaitForSeconds(2f);
        EnemyBase.BattleStarted = true;
    }

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    void Start()
    {
        // 인트로 중에는 적이 공격 안 함 (초기값 false)
        EnemyBase.BattleStarted = false;

        AudioManager.Instance.PlayBattleBGM();

        // ★ StartGame 한 번만 호출
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

        // 인트로 중에도 적 스폰은 시작 (단, 공격은 BattleStarted 플래그로 제어)
        StartCoroutine(RunWave());
    }

    // ═══════════════════════════════════════════════════════
    //  웨이브 진행
    // ═══════════════════════════════════════════════════════
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
                yield return new WaitForSeconds(waveClearDelay);
            }
        }

        Debug.Log("[WaveManager] 모든 웨이브 클리어!");
        OnStageClear?.Invoke();
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
        Vector3   targetPos    = GetNonOverlappingPosition();
        EnemyBase prefabEnemy  = prefab.GetComponent<EnemyBase>();
        Vector3   spawnPos;

        if (prefabEnemy is EnemyA)
        {
            float offScreenY = GetOffScreenY(targetPos.z);
            spawnPos = new Vector3(targetPos.x, offScreenY, targetPos.z);
        }
        else if (prefabEnemy is EnemyB)
        {
            float offScreenX = GetOffScreenX(targetPos.z);
            float side       = Random.value > 0.5f ? 1f : -1f;
            spawnPos         = new Vector3(offScreenX * side, targetPos.y, targetPos.z);
        }
        else
        {
            spawnPos = targetPos;
        }

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyBase  enemy = obj.GetComponent<EnemyBase>();

        if (enemy != null)
        {
            enemy.SetBulletPool(enemyBulletPool);
            enemy.SetTargetPosition(targetPos);
            enemy.OnDied += () => activeEnemies.Remove(enemy);
            activeEnemies.Add(enemy);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  위치 계산
    // ═══════════════════════════════════════════════════════
    Vector3 GetNonOverlappingPosition()
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 candidate   = GetRandomSpawnPosition();
            bool    overlapping = false;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector3.Distance(candidate, enemy.TargetPosition) < minEnemyDistance)
                {
                    overlapping = true;
                    break;
                }
            }

            if (!overlapping) return candidate;
        }

        Debug.LogWarning("[WaveManager] 겹치지 않는 위치를 찾지 못해 랜덤 배치");
        return GetRandomSpawnPosition();
    }

    Vector3 GetRandomSpawnPosition()
    {
        float z = Random.Range(minZ, maxZ);
        float t = (z - minZ) / (maxZ - minZ);

        float maxX = Mathf.Lerp(minXAtMinZ, maxXAtMaxZ, t);
        float x    = Random.Range(-maxX, maxX);

        float minY = Mathf.Lerp(minYAtMinZ, 0f, t);
        float maxY = Mathf.Lerp(minYAtMinZ, maxYAtMaxZ, t);
        float y    = Random.Range(minY, maxY);

        return new Vector3(x, y, z);
    }

    float GetOffScreenY(float z)
    {
        Camera  cam              = Camera.main;
        float   distanceFromCam  = z - cam.transform.position.z;
        Vector3 topWorld         = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, distanceFromCam));
        return topWorld.y + 5f;
    }

    float GetOffScreenX(float z)
    {
        Camera  cam              = Camera.main;
        float   distanceFromCam  = z - cam.transform.position.z;
        Vector3 rightWorld       = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, distanceFromCam));
        return Mathf.Abs(rightWorld.x) + 5f;
    }

    IEnumerator WaitForWaveClear()
    {
        while (true)
        {
            activeEnemies.RemoveAll(e => e == null || !e.IsAlive);
            if (activeEnemies.Count == 0) yield break;
            yield return new WaitForSeconds(0.5f);
        }
    }
}