using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  데이터 SO — 능력치는 EnemyData 에서 일괄 주입 (인스펙터 하드코딩 X).
    //  InitBase 가 ApplyEnemyData 로 자동 적용 → 파생체 Initialize 는 특수 변수만 다룸.
    // ═══════════════════════════════════════════════════════
    [Header("── 데이터 SO ────────────────────")]
    [Tooltip("적 능력치 SO. 미할당 시 폴백 값(아래) 사용.")]
    [SerializeField] protected EnemyData data;

    [Header("── sound ─────────────────")]
    [SerializeField] public AudioClip dieSoundClip;

    private AudioSource dieSoundSource;

    // 능력치 변수 — ApplyEnemyData 가 SO 에서 채움. 인스펙터 노출 X (SerializeField 의도적으로 제거).
    // SO 가 누락되면 ApplyEnemyData 가 비활성화 처리하므로 폴백 입력은 더 이상 필요 없음.
    protected EnemyType enemyType = EnemyType.Normal;
    protected float hp;
    protected float maxHp;
    protected float attackDamage;
    protected float speed;
    protected float bulletSpeed = 15f;   // ApplyEnemyData 가 SO 에서 채움. 폴백 15.
    protected bool survive = true;
    protected float attackDelay;
    protected int   currentLayer;
    protected ObjectPool bulletPool;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject deathEffectPrefab;
    private SpriteRenderer spriteRenderer;
    protected ITargetStrategy targetStrategy;
    protected List<CharacterBase> characters;
    private Coroutine hitFlashCoroutine;
    private bool isStunned = false;
    private Coroutine stunCoroutine;
    public static bool BattleStarted = false;
    private EnemyHPBar _hpBar;

    // ═══════════════════════════════════════════════════════
    //  난이도 스케일링 — WaveManager.StartGame 이 한 번 세팅, 모든 적이 공유.
    //   Initialize() 가 maxHp 를 세팅한 직후 ApplyDifficultyScaling 이 곱셈 적용.
    //   적 스폰보다 먼저 StartGame 이 호출되므로 신규 스폰 적은 자동으로 배율 반영.
    // ═══════════════════════════════════════════════════════
    public static float HpMultiplier { get; set; } = 1f;

    // 출현 연출 관련
    protected Vector3 targetPosition;
    protected bool isSpawning = true; // 출현 연출 중 여부
    private bool lockZPosition;
    private float lockedZPosition;

    //프로퍼티
    public float Hp => hp;
    public bool IsAlive => survive;
    public Vector3 TargetPosition => targetPosition;
    public bool IsSpawning => isSpawning;
    public bool IsStunned => isStunned;
    public Transform MuzzlePoint => muzzlePoint;
    public EnemyType EnemyType => data != null ? data.enemyType : enemyType;
    public float MaxHp => maxHp;
    /// <summary>공중 적 여부 — WaveManager 스폰 영역 분기에 사용. SO 직접 참조 (Initialize 전에도 접근 가능).</summary>
    public bool IsAirborne => data != null && data.isAirborne;

    // 이벤트
    public event Action OnDied;
    public static event Action<EnemyBase> OnBossDefeated; // 보스 사망 이벤트
    public event Action<float, float> OnHpChanged;        // (currentHp, maxHp)

    /// <summary>
    /// 위험 데미지 공격(레이저/미사일 등)을 캐릭터에게 조준한 순간 발화.
    /// duration = 발사까지 남은 시간(초). BottomUI 가 이 시간 동안 경고 표시.
    /// </summary>
    public static event Action<CharacterBase, float> OnHighDamageTargeting;
    public static void RaiseHighDamageTargeting(CharacterBase target, float duration)
        => OnHighDamageTargeting?.Invoke(target, duration);

    // abstract 메서드
    public abstract void Initialize();
    public abstract void Attack();
    public abstract void Move();
    public abstract void Jump();

    // ═══════════════════════════════════════════════════════
    //  거리 구역 분류 — WaveManager 가 SetSpawnRange 로 minZ/maxZ 를 한 번 설정.
    //  GetDistanceZone() 은 targetPosition.z 기준 lazy 계산 + 캐싱.
    // ═══════════════════════════════════════════════════════
    private static float _spawnMinZ = 10f;
    private static float _spawnMaxZ = 50f;
    public  static void  SetSpawnRange(float minZ, float maxZ)
    {
        _spawnMinZ = minZ;
        _spawnMaxZ = maxZ;
    }

    private DistanceZone? _cachedZone;
    public  DistanceZone  GetDistanceZone()
    {
        if (_cachedZone.HasValue) return _cachedZone.Value;

        float range = _spawnMaxZ - _spawnMinZ;
        if (range <= 0f)
        {
            _cachedZone = DistanceZone.Mid;
            return DistanceZone.Mid;
        }

        float t = Mathf.InverseLerp(_spawnMinZ, _spawnMaxZ, targetPosition.z);
        DistanceZone zone = t < 1f / 3f ? DistanceZone.Close
                          : t < 2f / 3f ? DistanceZone.Mid
                                        : DistanceZone.Far;
        _cachedZone = zone;
        return zone;
    }

    // ═══════════════════════════════════════════════════════
    //  피해 처리 — 두 가지 진입점
    //   • TakeDamage(damage)            : 단순 피해. Viper 버스트, 적 충돌, 디버그 등.
    //   • TakeDamage(damage, weaponType): 사거리 보너스 적용 후 단순 피해 호출. 일반 사격.
    // ═══════════════════════════════════════════════════════
    public virtual void TakeDamage(float damage, WeaponType weaponType)
    {
        DistanceZone myZone = GetDistanceZone();
        float multiplier   = WeaponSpecs.GetDamageMultiplier(weaponType, myZone);
        TakeDamage(damage * multiplier);
    }

    public virtual void TakeDamage(float damage)
    {
        if (!survive) return;
        hp -= damage;
        hp  = Mathf.Max(hp, 0f);

        OnHpChanged?.Invoke(hp, maxHp);

        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlash());

        if (hp <= 0) Die();
    }

    public void ApplyStun(float duration)
    {
        if (!survive) return;
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        OnStunned();
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    protected virtual void OnStunned() { }

    private IEnumerator HitFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    public virtual void Die()
    {
        OnDied?.Invoke();
        survive      = false;

        // 보스 사망 시 별도 이벤트
        if (enemyType == EnemyType.Boss)
            OnBossDefeated?.Invoke(this);

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;
        Collider col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        // ─ Rigidbody 정지 — 사망 후 중력으로 떨어지지 않도록 그 자리에 고정 ─
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;   // 물리 시뮬 비활성 → 위치 변경 X
        }

        dieSoundSource = gameObject.AddComponent<AudioSource>();
        dieSoundSource.PlayOneShot(dieSoundClip);

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                deathEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(effect, 2f);
        }

        Destroy(gameObject, 0.3f);
    }

    protected CharacterBase GetTarget()
    {
        if (targetStrategy == null) return null; // 초기화 실패 (CharacterManager 부재)
        return targetStrategy.GetTarget() as CharacterBase; // 타겟 후보 0 명이면 null하여 ???
    }

    void Start()
    {
        InitBase();
        Initialize();
        ApplyDifficultyScaling(); // Initialize 가 세팅한 hp/maxHp 에 난이도 배율 적용
    }

    /// <summary>
    /// 난이도 배율 적용 — Initialize 에서 결정된 maxHp 에 곱셈.
    /// 보스/일반 차등 처리 필요하면 EnemyD 등이 override.
    /// </summary>
    protected virtual void ApplyDifficultyScaling()
    {
        if (HpMultiplier <= 0f || Mathf.Approximately(HpMultiplier, 1f)) return;
        maxHp *= HpMultiplier;
        hp     = maxHp; // 시작은 항상 풀 HP
    }

    void Update()
    {
        if (!BattleStarted) return;
        if (!survive) return;
        if (isStunned) return;
        OnUpdate();
    }

    void LateUpdate()
    {
        if (!lockZPosition) return;

        Vector3 position = transform.position;
        if (!Mathf.Approximately(position.z, lockedZPosition))
        {
            position.z = lockedZPosition;
            transform.position = position;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = position;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
            }
        }
    }

    protected virtual void OnUpdate() { }

    public void TryAttack()
    {
        if (!survive) return;
        if (isStunned) return;
        if (isSpawning) return;
        Attack();
    }

    public void SetBulletPool(ObjectPool pool)
    {
        bulletPool = pool;
    }

    /// <summary>
    /// EnemyData SO 의 능력치를 인스턴스에 주입.
    /// data == null 이면 비활성화 — 인스펙터 폴백 입력 경로는 제거됨.
    /// 호출 시점: InitBase 안 (Initialize 보다 먼저).
    /// </summary>
    protected void ApplyEnemyData()
    {
        if (data == null)
        {
            Debug.LogError($"[{name}] EnemyData SO 미할당 — 프리팹 인스펙터의 Data 슬롯을 채울 것. 비활성화.");
            gameObject.SetActive(false);
            return;
        }

        enemyType    = data.enemyType;
        maxHp        = data.maxHp;
        hp           = data.maxHp;
        attackDamage = data.attackDamage;
        attackDelay  = data.attackDelay;
        speed        = data.speed;
        bulletSpeed  = data.bulletSpeed;   // ← SO 값이 여기서 흐름
    }

    public void SetTargetPosition(Vector3 pos)
    {
        targetPosition = pos;
    }

    protected void BeginManualMovement()
    {
        lockZPosition = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    protected void CompleteManualMovement(Vector3 finalPosition)
    {
        transform.position = finalPosition;
        lockedZPosition = finalPosition.z;
        lockZPosition = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.position = finalPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (IsAirborne)
        {
            // 공중: Y 도 고정 → 명시 좌표 유지 (예: EnemyB 옆 이동 시 부유)
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotation;
            rb.isKinematic = false;
            rb.Sleep();
        }
        else
        {
            // 지상: Y 자유 → 중력이 BattleGround Collider 까지 자연 낙하 시킴
            // Sleep 호출 X — 중력 계속 작용
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotation;
            rb.isKinematic = false;
        }
    }

    private void InitBase()
    {
        survive       = true;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // SO 능력치 적용 (Initialize 보다 먼저 — 파생체가 값을 덮어쓸 수 있게)
        ApplyEnemyData();

        // HP바 초기화 (maxHp 가 결정된 후 호출 필요)
        _hpBar = GetComponentInChildren<EnemyHPBar>(true);
        if (_hpBar != null)
            _hpBar.Init(this);

        CharacterManager characterManager = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
        if (characterManager != null)
        {
            characters     = characterManager.Characters;
            targetStrategy = new RandomTargetStrategy( 
                characters.ConvertAll(c => (ITargetable)c)
            );
        }
        else
        {
            Debug.LogError("[EnemyBase] CharacterManager를 찾을 수 없습니다.");
        }
    }
}

public interface ITargetStrategy
{
    ITargetable GetTarget();
}
