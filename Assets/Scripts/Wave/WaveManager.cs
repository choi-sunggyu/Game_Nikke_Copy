using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private ObjectPool enemyBulletPool;
    [Tooltip("EnemyD (보스) 미사일 풀. 보스 스폰 시 자동 주입.")]
    [SerializeField] private ObjectPool enemyMissilePool;

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

    [Header("── 새 Queue 시스템 ──────────")]
    [Tooltip("Queue 에 채울 일반 적 프리팹들 (EnemyA/B). EnemyC(Elite)/EnemyD(Boss)는 별도 슬롯 사용.")]
    [SerializeField] private GameObject[] regularEnemyPrefabs;
    [Tooltip("Elite 적 프리팹. EnemyC 처럼 일반보다 등장 비율이 낮은 강한 적.")]
    [SerializeField] private GameObject   elitePrefab;
    [Tooltip("Elite 등장 확률 (0~1). 0.1 = 10%. 기본 0.1")]
    [Range(0f, 1f)]
    [SerializeField] private float        eliteSpawnRatio = 0.1f;
    [Tooltip("마지막에 등장하는 보스 1마리. EnemyD 프리팹 할당.")]
    [SerializeField] private GameObject   bossPrefab;
    [Tooltip("일반 적 모두 처치 후 보스 등장까지 대기 시간 (초)")]
    [SerializeField] private float bossSpawnDelay = 1.5f;
    [Tooltip("게임 한 판에 등장할 총 적 수")]
    [SerializeField] private int totalEnemyTarget = 60;
    [Tooltip("그룹 등장 후 다음 그룹까지 대기 시간 (적 모두 처치 시 즉시 다음 그룹)")]
    [SerializeField] private float groupWaitDuration = 2f;
    [Tooltip("쪼르르 패턴(Lateral/DualSide/TopRandom) 의 시간차")]
    [SerializeField] private float trickleDelay = 0.3f;

    [Header("── 프로그레스 ────────────────")]
    [Tooltip("게임 시작 → 보스 등장까지의 총 시간 (초). progress 가 이 시간 동안 0→1 로 일정 속도 진행.")]
    [SerializeField] private float stageDuration = 75f;

    // ═══════════════════════════════════════════════════════
    //  이벤트
    // ═══════════════════════════════════════════════════════
    public static event Action OnStageClear;
    public static event Action OnElitePhaseStart;   // 엘리트 웨이브 시작
    public static event Action OnEliteDefeated;     // 엘리트 처치 완료
    public static event Action<EnemyBase> OnBossPhaseStart; // 보스 등장 시 보스 참조 전달
    public static event Action            OnBossDefeated;   // 보스 처치 → 즉시 승리

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();

    // 시간 기반 progress (일정 속도)
    private float _stageElapsed = 0f;
    private bool  _stageProgressRunning = false;

    public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

    // 외부(GameSettings/MainMenu) 가 참조하는 enum — 매핑은 큐 시스템 안에서 (현재는 enemy 체력 분기로 처리 예정)
    public enum Difficulty { Easy, Normal, Hard , Boss}

    // ═══════════════════════════════════════════════════════
    //  프로그레스 노출 (TopUI용)
    // ═══════════════════════════════════════════════════════
    public float WaveProgress    { get; private set; } // 0 ~ 1
    public bool  IsWaveBlocked   { get; private set; } // 적 미처치 시 progress 멈춤 신호 (WaveProgressBar 사용)


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

        // 적 거리 구역(Close/Mid/Far) 분류 기준을 EnemyBase 에 전달.
        EnemyBase.SetSpawnRange(minZ, maxZ);

        BattleManager.Instance.InitBulletConsumption(); // 총알 소비 초기화

        AudioManager.Instance.PlayBattleBGM();

        // ★ StartGame 한 번만 호출
        StartGame(GameSettings.SelectedDifficulty);
    }

    /// <summary>
    /// 게임 시작 — 큐 시스템 한 경로로 단일화.
    /// Difficulty 는 EnemyBase.HpMultiplier 정적 변수에 매핑되어 모든 적의 maxHp 에 곱셈 적용.
    /// 적 스폰은 StartGame 호출 이후 이루어지므로 신규 적은 자동 배율 반영.
    /// </summary>
    public void StartGame(Difficulty difficulty)
    {
        // ── 난이도 → HP 배율 매핑 ──
        EnemyBase.HpMultiplier = difficulty switch
        {
            Difficulty.Easy   => 1.0f,
            Difficulty.Normal => 1.5f,
            Difficulty.Hard   => 2.5f,
            Difficulty.Boss   => 4.0f,
            _                 => 1.0f
        };
        Debug.Log($"[WaveManager] 난이도 {difficulty} → HpMultiplier {EnemyBase.HpMultiplier}");

        WaveProgress          = 0f;
        IsWaveBlocked         = false;
        _stageElapsed         = 0f;
        _stageProgressRunning = true;  // ← Update 가 progress 갱신 시작

        StartCoroutine(RunWaveQueue());
    }

    /// <summary>
    /// 시간 기반 progress — stageDuration 동안 0→1 일정 속도.
    /// 보스 등장 직전에 _stageProgressRunning = false 로 멈춤.
    /// </summary>
    void Update()
    {
        if (!_stageProgressRunning) return;

        _stageElapsed += Time.deltaTime;
        WaveProgress   = Mathf.Clamp01(_stageElapsed / stageDuration);
    }

    // 잔여 적 처치 대기 — 큐 시스템 그룹 사이 / 보스 단계에서 사용
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

            // 적이 남아있으면 블록 상태 — WaveProgressBar 가 진행 멈춤 표시
            IsWaveBlocked = true;
            yield return new WaitForSeconds(0.5f);
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

            // 보스 등장 이벤트
            if (enemy.EnemyType == EnemyType.Boss)
            {
                // EnemyD 라면 미사일 풀 주입 — 프리팹/씬 결합 차단 (씬 풀 → 프리팹 보스로 전달)
                if (enemy is EnemyD bossD)
                {
                    if (enemyMissilePool != null) bossD.SetMissilePool(enemyMissilePool);
                    else                          Debug.LogWarning("[WaveManager] enemyMissilePool 미할당 — 보스가 미사일을 발사할 수 없음");
                }

                OnBossPhaseStart?.Invoke(enemy);
                enemy.OnDied += () =>
                {
                    activeEnemies.Remove(enemy);
                    // 보스 사망 → 즉시 클리어
                    OnEliteDefeated?.Invoke();
                };
            }
            else
            {
                enemy.OnDied += () => activeEnemies.Remove(enemy);
            }

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

    // ═══════════════════════════════════════════════════════
    //  ★ 새 Queue 기반 스폰 시스템
    //   - Ｎ마리 적을 6종 패턴으로 분할해 큐에 적재
    //   - 그룹 등장 후 2초 또는 모든 적 처치 시 다음 그룹
    //   - WaveProgress = 처치된 적 / 전체 타겟
    // ═══════════════════════════════════════════════════════
    private Queue<SpawnGroup> _spawnQueue = new Queue<SpawnGroup>();
    private int _enemiesKilled = 0;

    void GenerateSpawnQueue()
    {
        _spawnQueue.Clear();
        _enemiesKilled = 0;

        if (regularEnemyPrefabs == null || regularEnemyPrefabs.Length == 0)
        {
            Debug.LogError("[WaveManager] regularEnemyPrefabs 미할당 — 인스펙터에 EnemyA/B/C 프리팹 드래그 필요.");
            return;
        }

        int remaining = totalEnemyTarget;
        int safety    = 200; // 무한 루프 방지 (보스만 가득한 슬롯일 때)
        while (remaining > 0 && safety-- > 0)
        {
            SpawnPattern pattern = PickRandomPattern();
            int count            = Mathf.Min(GetPatternCount(pattern), remaining);
            GameObject prefab    = PickRandomPrefab();

            // ⚠ 안전망 — regularEnemyPrefabs 에 보스가 잘못 들어가 있어도 큐에서 배제
            EnemyBase eb = prefab.GetComponent<EnemyBase>();
            if (eb != null && eb.EnemyType == EnemyType.Boss)
            {
                Debug.LogWarning($"[WaveManager] regularEnemyPrefabs 에 보스({prefab.name}) 가 포함됨 — 큐에서 제외. bossPrefab 슬롯으로 옮길 것.");
                continue;
            }

            _spawnQueue.Enqueue(new SpawnGroup(pattern, prefab, count, trickleDelay));
            remaining -= count;
        }
    }

    /// <summary>
    /// 큐에 들어갈 적 프리팹 1개 선택.
    /// Elite 가 등록돼 있으면 eliteSpawnRatio 확률로 Elite, 나머지는 일반 균등.
    /// </summary>
    GameObject PickRandomPrefab()
    {
        if (elitePrefab != null && Random.value < eliteSpawnRatio)
            return elitePrefab;
        return regularEnemyPrefabs[Random.Range(0, regularEnemyPrefabs.Length)];

        Debug.Log($"[WaveManager] 스폰 큐 생성 — {_spawnQueue.Count} 그룹, 총 {totalEnemyTarget} 마리");
    }

    SpawnPattern PickRandomPattern()
    {
        float r = Random.value;
        if (r < 0.25f) return SpawnPattern.Single;       // 25% — 단독
        if (r < 0.45f) return SpawnPattern.Trio;         // 20% — 3마리 동시
        if (r < 0.60f) return SpawnPattern.LateralLeft;  // 15% — 왼쪽 쪼르르
        if (r < 0.75f) return SpawnPattern.LateralRight; // 15% — 오른쪽 쪼르르
        if (r < 0.88f) return SpawnPattern.DualSide;     // 13% — 양쪽 동시
        return SpawnPattern.TopRandom;                   // 12% — 위 쪼르르
    }

    int GetPatternCount(SpawnPattern p)
    {
        switch (p)
        {
            case SpawnPattern.Single:       return 1;
            case SpawnPattern.Trio:         return 3;
            case SpawnPattern.LateralLeft:  return Random.Range(2, 4); // 2~3
            case SpawnPattern.LateralRight: return Random.Range(2, 4);
            case SpawnPattern.DualSide:     return Random.Range(4, 7); // 4~6 (양쪽 합쳐서)
            case SpawnPattern.TopRandom:    return Random.Range(2, 5); // 2~4
            default:                        return 1;
        }
    }

    IEnumerator RunWaveQueue()
    {
        GenerateSpawnQueue();
        int totalGroups = _spawnQueue.Count;
        int processedGroups = 0;

        while (_spawnQueue.Count > 0)
        {
            SpawnGroup group = _spawnQueue.Dequeue();
            yield return StartCoroutine(SpawnGroupRoutine(group));
            processedGroups++;

            // ── 다음 그룹까지 대기: 2초 또는 모든 적 처치 (둘 중 먼저 일어나는 것) ──
            // WaveProgress 는 Update 가 시간 기반으로 갱신 — 여기서 손대지 않음.
            float elapsed = 0f;
            while (elapsed < groupWaitDuration)
            {
                activeEnemies.RemoveAll(e => e == null || !e.IsAlive);
                if (activeEnemies.Count == 0) break; // 모두 처치 → 즉시 다음 그룹
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 마지막 그룹 후 잔여 적 처치 대기
        yield return StartCoroutine(WaitForWaveClearWithBlock());

        // 시간 기반 progress 종료 → 1f 로 고정 (이후 Update 가 손대지 않음)
        _stageProgressRunning = false;
        WaveProgress          = 1f;

        // ── 보스 단계 ──
        if (bossPrefab != null)
        {
            yield return new WaitForSeconds(bossSpawnDelay);
            SpawnEnemy(bossPrefab);
            // 보스 처치 대기 — OnEliteDefeated/OnStageClear 는 보스 OnDied 람다에서 발화하므로 추가 처리 불필요.
            yield return StartCoroutine(WaitForWaveClearWithBlock());
        }
        else
        {
            Debug.LogWarning("[WaveManager] bossPrefab 미할당 — 보스 단계 건너뜀");
        }

        OnStageClear?.Invoke();
    }

    IEnumerator SpawnGroupRoutine(SpawnGroup group)
    {
        switch (group.Pattern)
        {
            case SpawnPattern.Single:
                SpawnEnemy(group.EnemyPrefab);
                break;

            case SpawnPattern.Trio:
                for (int i = 0; i < group.Count; i++)
                    SpawnEnemy(group.EnemyPrefab);
                break;

            case SpawnPattern.LateralLeft:
                for (int i = 0; i < group.Count; i++)
                {
                    SpawnEnemyFromSide(group.EnemyPrefab, leftSide: true);
                    yield return new WaitForSeconds(group.TrickleDelay);
                }
                break;

            case SpawnPattern.LateralRight:
                for (int i = 0; i < group.Count; i++)
                {
                    SpawnEnemyFromSide(group.EnemyPrefab, leftSide: false);
                    yield return new WaitForSeconds(group.TrickleDelay);
                }
                break;

            case SpawnPattern.DualSide:
                int half     = group.Count / 2;
                int rightCnt = group.Count - half;
                // 양쪽 동시 코루틴 시작
                StartCoroutine(SpawnSideTrickle(group.EnemyPrefab, true,  half,     group.TrickleDelay));
                StartCoroutine(SpawnSideTrickle(group.EnemyPrefab, false, rightCnt, group.TrickleDelay));
                // 둘 중 긴 쪽이 끝날 때까지 대기
                yield return new WaitForSeconds(Mathf.Max(half, rightCnt) * group.TrickleDelay);
                break;

            case SpawnPattern.TopRandom:
                for (int i = 0; i < group.Count; i++)
                {
                    SpawnEnemyFromTop(group.EnemyPrefab);
                    yield return new WaitForSeconds(group.TrickleDelay);
                }
                break;
        }
    }

    IEnumerator SpawnSideTrickle(GameObject prefab, bool leftSide, int count, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemyFromSide(prefab, leftSide);
            yield return new WaitForSeconds(delay);
        }
    }

    // ─ 측면(좌/우)에서 스폰 ─ EnemyB 의 옆 등장 흐름을 명시적으로 호출
    void SpawnEnemyFromSide(GameObject prefab, bool leftSide)
    {
        Vector3 targetPos  = GetNonOverlappingPosition();
        float   offScreenX = GetOffScreenX(targetPos.z);
        Vector3 spawnPos   = new Vector3(offScreenX * (leftSide ? -1f : 1f), targetPos.y, targetPos.z);
        InstantiateEnemyAt(prefab, spawnPos, targetPos);
    }

    // ─ 상단에서 스폰 ─ EnemyA 의 낙하 흐름을 명시적으로 호출
    void SpawnEnemyFromTop(GameObject prefab)
    {
        Vector3 targetPos  = GetNonOverlappingPosition();
        float   offScreenY = GetOffScreenY(targetPos.z);
        // X 좌표 살짝 랜덤화 (쪼르르 효과)
        float   randomX    = targetPos.x + Random.Range(-2f, 2f);
        Vector3 spawnPos   = new Vector3(randomX, offScreenY, targetPos.z);
        InstantiateEnemyAt(prefab, spawnPos, targetPos);
    }

    // ─ 명시 위치 인스턴스화 + 이벤트 구독 (SpawnEnemy 의 핵심 로직 재사용) ─
    void InstantiateEnemyAt(GameObject prefab, Vector3 spawnPos, Vector3 targetPos)
    {
        GameObject obj   = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyBase  enemy = obj.GetComponent<EnemyBase>();
        if (enemy == null) return;

        enemy.SetBulletPool(enemyBulletPool);
        enemy.SetTargetPosition(targetPos);

        // 처치 카운트 + 활성 리스트 정리
        enemy.OnDied += () =>
        {
            activeEnemies.Remove(enemy);
            _enemiesKilled++;
        };
        activeEnemies.Add(enemy);
    }
}