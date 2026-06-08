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
    [Header("── 프로그레스 타이밍 ──────────")]
    [SerializeField] private float totalProgressDuration = 60f;

    // ═══════════════════════════════════════════════════════
    //  이벤트
    // ═══════════════════════════════════════════════════════
    public static event Action OnStageClear;
    public static event Action OnElitePhaseStart;   // 엘리트 웨이브 시작
    public static event Action OnEliteDefeated;     // 엘리트 처치 완료

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private WaveData        currentData;
    private int             currentWaveIndex = 0;
    private List<EnemyBase> activeEnemies    = new List<EnemyBase>();

    private const float waveClearDelay = 2f;
    private const float spawnInterval  = 2f;
    private int _totalNormalEnemies  = 0;
    private int _killedNormalEnemies = 0;
    private float _progressTimer    = 0f;  // 경과 시간
    private bool  _progressRunning  = false;
    private int   _normalWaveCount  = 0;
    private float _nextCheckpoint   = 0f; // 다음 정지 체크포인트 (0~1)
    private int   _nextWaveToSpawn  = 0;

    public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

    public enum Difficulty { Easy, Normal, Hard , Boss}

    // ═══════════════════════════════════════════════════════
    //  프로그레스 노출 (TopUI용)
    // ═══════════════════════════════════════════════════════
    public float WaveProgress    { get; private set; } // 0 ~ 1
    public bool  IsWaveBlocked   { get; private set; } // 적 미처치 정지 여부
    public bool  IsElitePhase    { get; private set; } // 엘리트 단계 여부
    public int   TotalWaveCount  => currentData != null ? currentData.waves.Count : 0;
    public int   CurrentWaveIndex => currentWaveIndex;


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

        BattleManager.Instance.InitBulletConsumption(); // 총알 소비 초기화

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
        WaveProgress     = 0f;
        IsWaveBlocked    = false;
        _progressTimer   = 0f;
        _progressRunning = false;
        _nextWaveToSpawn = 0;

        // 일반 웨이브 수 계산
        _normalWaveCount = 0;
        foreach (var w in currentData.waves)
            if (!w.isEliteWave) _normalWaveCount++;

        // 첫 번째 체크포인트 설정 (1번 웨이브 처리 완료 시점)
        _nextCheckpoint = _normalWaveCount > 0
            ? 1f / _normalWaveCount
            : 1f;

        StartCoroutine(RunWave());

        if (currentData.waves.Count > 0 && !currentData.waves[0].isEliteWave)
            StartCoroutine(SpawnWave(currentData.waves[0]));
    }

    void Update()
    {
        if (!_progressRunning || IsElitePhase) return;

        float targetProgress = _nextCheckpoint;

        // 체크포인트 직전까지 채워짐
        if (WaveProgress < targetProgress - 0.01f)
        {
            _progressTimer += Time.deltaTime;
            WaveProgress = Mathf.Clamp(
                _progressTimer / totalProgressDuration,
                0f,
                targetProgress - 0.01f  // 체크포인트 바로 앞에서 멈춤
            );
            IsWaveBlocked = false;
        }
        else
        {
            // 체크포인트 도달 — 적 남아있으면 정지
            activeEnemies.RemoveAll(e => e == null || !e.IsAlive);
            IsWaveBlocked = activeEnemies.Count > 0;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  웨이브 진행
    // ═══════════════════════════════════════════════════════
    IEnumerator RunWave()
    {
        int normalWaveCount = 0;
        foreach (var w in currentData.waves)
            if (!w.isEliteWave) normalWaveCount++;

        int normalWaveCleared = 0;

        while (currentWaveIndex < currentData.waves.Count)
        {
            WaveData.Wave wave = currentData.waves[currentWaveIndex];

            if (wave.isEliteWave)
            {
                WaveProgress = 1f;
                IsElitePhase = true;
                OnElitePhaseStart?.Invoke();

                yield return StartCoroutine(SpawnWave(wave));
                yield return StartCoroutine(WaitForWaveClear());

                OnEliteDefeated?.Invoke();
            }
            else
            {
                yield return StartCoroutine(SpawnWave(wave));
                yield return StartCoroutine(WaitForWaveClearWithBlock());

                normalWaveCleared++;

                bool nextIsElite = (currentWaveIndex + 1 < currentData.waves.Count)
                                && currentData.waves[currentWaveIndex + 1].isEliteWave;

                // ← WaveProgress는 목표값만 설정, 시각적 이동은 WaveProgressBar가 담당
                WaveProgress = nextIsElite
                    ? 0.99f
                    : (float)normalWaveCleared / normalWaveCount;
            }

            currentWaveIndex++;

            if (currentWaveIndex < currentData.waves.Count
                && !currentData.waves[currentWaveIndex].isEliteWave)
                yield return new WaitForSeconds(waveClearDelay);
        }

        OnStageClear?.Invoke();
    }

    IEnumerator WaitForWaveClearWithBlock()
    {
        while (true)
        {
            activeEnemies.RemoveAll(e => e == null || !e.IsAlive);

            if (activeEnemies.Count == 0)
            {
                IsWaveBlocked = false;
                yield break;
            }

            // 적이 남아있으면 블록 상태
            IsWaveBlocked = true;
            yield return new WaitForSeconds(0.5f);
        }
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
        Vector3   targetPos   = GetNonOverlappingPosition();
        EnemyBase prefabEnemy = prefab.GetComponent<EnemyBase>();
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

        GameObject obj   = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyBase  enemy = obj.GetComponent<EnemyBase>();

        if (enemy != null)
        {
            enemy.SetBulletPool(enemyBulletPool);
            enemy.SetTargetPosition(targetPos);
            enemy.OnDied += () =>
            {
                activeEnemies.Remove(enemy);

                // 일반 적 처치 시 프로그레스 실시간 갱신
                if (!IsElitePhase && _totalNormalEnemies > 0)
                {
                    _killedNormalEnemies++;
                    WaveProgress = Mathf.Clamp01(
                        (float)_killedNormalEnemies / _totalNormalEnemies
                    );
                }
            };
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