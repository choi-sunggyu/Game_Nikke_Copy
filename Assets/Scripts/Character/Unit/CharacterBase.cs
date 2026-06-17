using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour
{
    [Header("── 데이터 (ScriptableObject) ─────")]
    [Tooltip("능력치/스프라이트 정체성 데이터. 할당하면 Initialize() 안의 ApplyData() 로 자동 적용됨.")]
    [SerializeField] protected CharacterData data;
    public CharacterData Data => data;

    [Header("── 시트 애니메이션 (15장 = 5+5+5) ─")]
    [Tooltip("애니메이션을 적용할 SpriteRenderer. 비우면 GetComponent 로 자동 탐색.")]
    [SerializeField] protected SpriteRenderer animSpriteRenderer;
    [Tooltip("Sprite Editor 로 자른 15장 슬라이스. 인덱스 0~4: idle→shoot, 5~9: shoot→reload, 10~14: reload→idle")]
    [SerializeField] protected Sprite[] animSprites;
    [Tooltip("한 프레임당 표시 시간 (초). 20fps = 0.05")]
    [SerializeField] protected float frameDuration = 0.05f;

    // ─ 기본 시퀀스 (15장 5+5+5 구조 기준) ─────────────────────
    //   서브클래스가 다른 시퀀스를 원하면 GetTransitionSequence / GetLoopForState 오버라이드
    protected static readonly int[] DEFAULT_IDLE_LOOP       = { 14 };               // ReloadToIdle 마지막
    protected static readonly int[] DEFAULT_SHOOT_LOOP      = { 4, 5 };             // 사격 자세 ↔ 사격 직후 (왕복)
    protected static readonly int[] DEFAULT_RELOAD_LOOP     = { 9 };                // ShootToReload 마지막
    protected static readonly int[] DEFAULT_IDLE_TO_SHOOT   = { 0, 1, 2, 3, 4 };    // 5장
    protected static readonly int[] DEFAULT_SHOOT_TO_RELOAD = { 5, 6, 7, 8, 9 };    // 5장
    protected static readonly int[] DEFAULT_RELOAD_TO_IDLE  = { 10, 11, 12, 13, 14 }; // 5장

    private CharacterState? animPrevState = null;
    private Coroutine        animCoroutine;
    public CameraShake cameraShake;

    [Header("변경 값")]
    [SerializeField] protected float hp;
    [SerializeField] protected float maxHp;
    [SerializeField] protected int maxBulletCount;
    [SerializeField] protected int bulletCount;
    [SerializeField] protected float shield;
    [SerializeField] protected float maxShield;
    [SerializeField] protected bool buff;
    [SerializeField] protected bool debuff;
    [SerializeField] protected Transform muzzlePoint;  // 총구 위치
    [SerializeField] protected ObjectPool bulletPool;   // 총알 풀
    
    [SerializeField] protected float skillCoolTime;
    [SerializeField] protected float burstCoolTime;
    [SerializeField] protected float reloadTime;
    [SerializeField] protected bool survive;
    [SerializeField] protected float chargingBurstGauge;
    [SerializeField] protected float attackDamage;
    [SerializeField] protected Sprite idleSprite;   // 대기 이미지
    [SerializeField] protected Sprite shootSprite;  // 사격 이미지
    [SerializeField] protected Sprite reloadSprite; // 리로딩 이미지
    [SerializeField] protected float fireRate; // 발사 딜레이
    [SerializeField] protected CrossHairBase crossHair;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] protected float bulletSpeed;
    [SerializeField] protected int burstNumber; // Ghost=1, Titan=2, Viper=3
    [SerializeField] protected WeaponType weaponType = WeaponType.AR; // 사거리 보너스 계산용 (CharacterData 가 덮어씀)
    public WeaponType WeaponType => weaponType;
    [SerializeField] protected float burstCutsceneDuration = 0f; // 버스트 컷씬 지속 시간 (초) - 이 시간 동안 플레이어 조작 잠금, AI는 TryFireAtTarget로 공격
    [SerializeField] private Sprite characterPortrait;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] protected float criticalRate;
    [SerializeField] protected float criticalMultiplier = 1.5f;
    [Header("── 캐릭터 이미지 ─────────────────")]
    [SerializeField] private Sprite characterSprite;
    protected float bonusCriticalRate;
    public bool UsedBurstThisCycle { get; set; }
    private float nextFireTime;
    private CharacterState currentState { get; set; }
    private SpriteRenderer spriteRenderer;
    protected float attackDamageMultiplier = 1f;
    protected float criticalRateMultiplier = 1f;

    // 프로퍼티
    public float CriticalRate => (criticalRate + bonusCriticalRate) * criticalRateMultiplier;
    public float FinalAttackDamage =>
        attackDamage * attackDamageMultiplier;

    public bool IsAlive => survive;
    public float HpRatio => hp / maxHp;
    public float MaxHp => maxHp;
    public int MaxBulletCount => maxBulletCount;
    public int CurrentBulletCount => bulletCount;
    public float ShieldRatio => shield / maxShield;
    public float NextFireTime => nextFireTime;
    public CharacterState CurrentState => currentState;
    public CrossHairBase CrossHair => crossHair;
    public int BurstNumber => burstNumber;
    public float BurstCutsceneDuration => burstCutsceneDuration;
    public float BurstCoolTime => burstCoolTime;
    public Sprite CharacterPortrait => characterPortrait;
    public float AttackDamageMultiplier => attackDamageMultiplier;
    protected void SetNextFireTime(float time) => nextFireTime = time;
    public float TotalDamageDealt { get; private set; } = 0f;
    public bool IsActiveCharacter { get; set; }
    public Transform MuzzlePoint => muzzlePoint;
    public LayerMask EnemyLayer => enemyLayer;    
    public Sprite CharacterSprite => characterSprite;

    // sender: 이벤트를 발생시킨 캐릭터, count: 탄 수
    public static event Action<CharacterBase, int> OnBulletCountChanged;
    public static event Action<CharacterBase> OnStatChanged;
    public static event Action<CharacterBase, float> OnReloadProgress; // UI용 이벤트 (0~1)
    public static event Action<CharacterBase> OnCharacterDied;
    public static event Action<CharacterBase, int> OnBulletConsumed;

    private Coroutine reloadCoroutine;
    private bool coverReloadLocked = false; // 일시적 강제 엄폐(스페이스) 잠금: true 동안 사격 입력으로 리로드가 취소되지 않음
    public bool IsCoverReloadLocked => coverReloadLocked;

    public abstract void Initialize();
    public abstract void UseSkill();
    public abstract void UseBurst();
    public static event Action<CharacterBase> OnForcedReloadStart;
    public static event Action<CharacterBase> OnForcedReloadEnd;

    /// <summary>
    /// CharacterData(SO)의 값으로 자신의 능력치/스프라이트를 채워 넣는다.
    /// 캐릭터별 Initialize() 의 첫 줄에서 호출하면 됨.
    /// data 가 비어있으면 인스펙터에 직접 입력된 값이 그대로 유지됨 (점진적 마이그레이션).
    /// </summary>
    protected void ApplyData()
    {
        if (data == null) return;

        // 체력 / 방어
        maxHp     = data.maxHp;
        hp        = maxHp;
        maxShield = data.maxShield;
        shield    = maxShield;

        // 사격
        maxBulletCount = data.maxBulletCount;
        bulletCount    = maxBulletCount;
        reloadTime     = data.reloadTime;
        attackDamage   = data.attackDamage;
        fireRate       = data.fireRate;
        bulletSpeed    = data.bulletSpeed;

        // 버스트 / 스킬
        chargingBurstGauge    = data.chargingBurstGauge;
        burstCoolTime         = data.burstCoolTime;
        skillCoolTime         = data.skillCoolTime;
        burstCutsceneDuration = data.burstCutsceneDuration;
        burstNumber           = data.burstNumber;
        weaponType            = data.weaponType;

        // 크리티컬
        criticalRate       = data.criticalRate;
        criticalMultiplier = data.criticalMultiplier;

        // 시각 (SO 에 비어있는 슬롯은 인스펙터 직접 할당값 유지)
        if (data.idleSprite        != null) idleSprite        = data.idleSprite;
        if (data.shootSprite       != null) shootSprite       = data.shootSprite;
        if (data.reloadSprite      != null) reloadSprite      = data.reloadSprite;
        if (data.characterPortrait != null) characterPortrait = data.characterPortrait;
        if (data.characterSprite   != null) characterSprite   = data.characterSprite;

        survive = true;
    }

    public static void InvokeBulletCountChanged(CharacterBase sender, int count)
    {
        OnBulletCountChanged?.Invoke(sender, count);
    }

    public void TakeDamage(float damage)
    {
        if (!survive) return;

        if (buff) damage *= 0.75f;
        if (debuff) damage *= 1.25f;

        bool isBlock = false;

        // 1. idle일 때는 쉴드가 먼저 깎이고, 쉴드가 깨지면 남은 데미지가 hp에 적용
        // 2. fire일 때는 바로 hp에 데미지 적용
        // 3. 리로딩 중일 때는 fire와 동일하게 shield에 데미지 적용 (리로딩이 엄폐 상태라고 가정)
        switch(currentState)
        {
            case CharacterState.Idle:
            case CharacterState.Reload:
                if(shield > 0) //쉴드가 남아 있음
                {
                    isBlock = true;
                    shield -= damage;
                    //체력 감소
                    if(shield < 0) //쉴드 깨짐 남은 데미지 받음
                    {
                        hp += shield;
                        shield = 0;
                    }
                }
                else //쉴드 깨짐
                {
                    hp -= damage;
                }
                break;
            case CharacterState.Fire:
                hp -= damage;
                break;
        }

        BurstGaugeManager.Instance?.AddGauge(damage * 0.1f);

        if (IsActiveCharacter)
            DamagePopupManager.Instance?.ShowPlayerDamage(damage, transform.position, isBlock);
        
        //사망 여부   
        if(hp <= 0)
        {
            survive = false;
            OnCharacterDied?.Invoke(this);
        }
        OnStatChanged?.Invoke(this);
    }    

    public virtual void TryFire()
    {
        // 사격 조건 체크
        // 강제 엄폐 리로딩 중이면 사격은 물론 StopReload(리로드 취소)까지 막아야 한다.
        // 그래서 마우스를 계속 누르고 있어도 강제 리로딩이 끊기지 않는다.
        if (coverReloadLocked) return;

        if (survive)
        {
            if (bulletCount > 0 && Time.time >= nextFireTime) //강제 리로딩 중이 아니고 탄창이 남아 있는 경우에만 사격
            {
                StopReload();
                ChangeState(CharacterState.Fire);

                bulletCount--;

                // 사격 시 카메라 흔들림
                StartCoroutine(cameraShake.Shake(0.15f, 0.2f));
                
                OnBulletConsumed?.Invoke(this, 1);
                nextFireTime = Time.time + fireRate;

                OnBulletCountChanged?.Invoke(this, bulletCount);
                FireBullet();

                // 사격 로직 (예: 총알 발사, 애니메이션 재생 등)
                if(bulletCount == 0) //탄창이 다 떨어졌으면 강제 리로딩 상태로 전환
                {
                    TryReload();
                }
            }
            else // 강제 리로딩 중이거나 탄창이 없는 경우 사격 불가
            {
                // bulletCount가 0인 경우는 여기서 TryReload를 하지 않고 다른 곳에서 처리 중일 것임
            }
        }
    }

    private Dictionary<string, Coroutine> activeCriticalBuffs = new();

    public void ApplyDamageBuff(float multiplier, float duration, string buffId)
    {
        StartCoroutine(DamageBuffCoroutine(multiplier, duration, buffId));
    }

    private Dictionary<string, Coroutine> activeBuffs = new Dictionary<string, Coroutine>();

    private IEnumerator DamageBuffCoroutine(float multiplier, float duration, string buffId)
    {
        if (activeBuffs.ContainsKey(buffId) && activeBuffs[buffId] != null)
        {
            StopCoroutine(activeBuffs[buffId]);
            attackDamageMultiplier /= multiplier;
        }

        attackDamageMultiplier *= multiplier;

        Coroutine c = StartCoroutine(BuffTimer(multiplier, duration, buffId));
        activeBuffs[buffId] = c; // ← BuffTimer가 즉시 완료되면 이미 Remove된 후 저장될 수 있음
        yield break;
    }

    private IEnumerator BuffTimer(float multiplier, float duration, string buffId)
    {
        yield return new WaitForSeconds(duration);
        attackDamageMultiplier /= multiplier;
        if (activeBuffs.ContainsKey(buffId)) // BuffTimer 안에서 Remove 전 Contanins 체크
            activeBuffs.Remove(buffId);
    }

    public (float damage, bool isCritical) CalculateDamage(float baseDamage)
    {
        bool  isCritical  = UnityEngine.Random.value < CriticalRate;
        float finalDamage = isCritical
            ? baseDamage * criticalMultiplier
            : baseDamage;

        return (finalDamage, isCritical);
    }

    protected virtual void FireBullet()
    {
        if (bulletPool == null || muzzlePoint == null) return;

        // Step 1: 카메라 → 크로스헤어 방향으로 worldTarget 결정
        Ray camRay = Camera.main.ScreenPointToRay(crossHair.CrossHairPosition);
        Vector3 worldTarget;

        if (Physics.Raycast(camRay, out RaycastHit camHit, 1000f, enemyLayer))
        {
            worldTarget = camHit.point; // 적에 맞으면 그 지점
        }
        else
        {
            worldTarget = camRay.GetPoint(1000f); // 허공이면 최대 사거리 지점
        }

        // Step 2: muzzlePoint → worldTarget 방향으로 총알 발사
        Vector3 fireDir = (worldTarget - muzzlePoint.position).normalized;

        // ── SG(샷건): 산탄 발사 분기 — 1발 트리거로 N개 탄환이 약간 분산되어 발사 ──
        if (weaponType == WeaponType.SG)
        {
            SpawnShotgunSpread(fireDir);
            return;
        }

        // ── 그 외 무기: 단발 발사 ──
        SpawnSingleBullet(muzzlePoint.position, fireDir, FinalAttackDamage);
    }

    // 단일 탄환 생성 헬퍼 (단발 무기 + 산탄 펠릿 공용)
    private void SpawnSingleBullet(Vector3 originPos, Vector3 dir, float damage)
    {
        GameObject bullet = bulletPool.Get(originPos, Quaternion.identity);
        if (bullet == null) return;

        BulletBase bulletBase = bullet.GetComponent<BulletBase>();
        bulletBase.Init(this, damage, bulletSpeed, dir, chargingBurstGauge);
    }

    // SG 산탄: cone 분포로 N개 펠릿 발사. 탄당 데미지는 attackDamage/N 으로 분산.
    private void SpawnShotgunSpread(Vector3 baseDir)
    {
        float perPelletDamage = FinalAttackDamage / WeaponSpecs.SG_PELLET_COUNT;
        for (int i = 0; i < WeaponSpecs.SG_PELLET_COUNT; i++)
        {
            Vector3 spreadDir = ApplyConeSpread(baseDir, WeaponSpecs.SG_SPREAD_ANGLE);
            SpawnSingleBullet(muzzlePoint.position, spreadDir, perPelletDamage);
        }
    }

    // baseDir 을 중심으로 ±maxAngleDeg cone 내 무작위 방향 반환
    private Vector3 ApplyConeSpread(Vector3 baseDir, float maxAngleDeg)
    {
        // 무작위 yaw / pitch 각도
        float yaw   = UnityEngine.Random.Range(-maxAngleDeg, maxAngleDeg);
        float pitch = UnityEngine.Random.Range(-maxAngleDeg, maxAngleDeg);

        // baseDir 기준 회전 — 카메라 정렬이 아닌 월드 yaw/pitch 적용 (단순화)
        Quaternion spreadRot = Quaternion.AngleAxis(yaw, Vector3.up)
                             * Quaternion.AngleAxis(pitch, Vector3.right);
        return spreadRot * baseDir;
    }

    public void ApplyCriticalRateBuff(
        float amount,
        float duration,
        string buffId)
    {
        StartCoroutine(
            CriticalRateBuffCoroutine(
                amount,
                duration,
                buffId));
    }

    private IEnumerator CriticalRateBuffCoroutine(
        float amount,
        float duration,
        string buffId)
    {
        if(activeCriticalBuffs.ContainsKey(buffId))
        {
            StopCoroutine(activeCriticalBuffs[buffId]);

            bonusCriticalRate -= amount;
        }

        bonusCriticalRate += amount;

        Coroutine c =
            StartCoroutine(
                CriticalRateBuffTimer(
                    amount,
                    duration,
                    buffId));

        activeCriticalBuffs[buffId] = c;

        yield break;
    }

    private IEnumerator CriticalRateBuffTimer(
        float amount,
        float duration,
        string buffId)
    {
        yield return new WaitForSeconds(duration);

        bonusCriticalRate -= amount;

        activeCriticalBuffs.Remove(buffId);
    }

    protected void StopReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
            OnReloadProgress?.Invoke(this, -1f); // ← 취소 신호 추가
        }
        coverReloadLocked = false;
    }

    // 일시적 강제 엄폐: 스페이스 입력 시 현재 캐릭터가 사격 중이라도 강제로 리로딩에 들어간다.
    // 리로드가 끝나면 잠금이 풀리고, 마우스를 계속 누르고 있었다면 자연스럽게 사격이 재개된다.
    public void ForceCoverReload()
    {
        if (!survive) return;
        if (coverReloadLocked) return;            // 이미 강제 엄폐 중 → 중복 방지
        if (bulletCount == maxBulletCount) return; // 탄창이 가득 차 있으면 리로드할 게 없음

        StopReload();              // 진행 중이던 일반 리로드 코루틴 정리(여기서 잠금이 false로 내려감)
        coverReloadLocked = true;  // 그 다음에 잠근다 (순서 중요)

        // 탄창 절반 이하면 리로드 타임 +1초 (TryReload와 동일한 규칙)
        float actualReloadTime = ((float)bulletCount / maxBulletCount) <= 0.5f
            ? reloadTime + 1f
            : reloadTime;

        reloadCoroutine = StartCoroutine(ReloadDelay(true, actualReloadTime));
    }
    public virtual void TryReload()
    {
        // 호출 전 survive 체크가 보장되므로 중복 체크 생략
        // 리로딩 조건 체크
        if(bulletCount == maxBulletCount)
        {
            ChangeState(CharacterState.Idle);
            return;
        }
        if(currentState == CharacterState.Reload) return;

        bool isForced = (bulletCount == 0);

        // 탄창 절반 이하면 리로드 타임 +1초
        float actualReloadTime = ((float)bulletCount / maxBulletCount) <= 0.5f
            ? reloadTime + 1f
            : reloadTime;

        reloadCoroutine = StartCoroutine(ReloadDelay(isForced, actualReloadTime));
    }

    private IEnumerator ReloadDelay(bool isForced = false, float duration = 0f)
    {
        ChangeState(CharacterState.Reload);

        if(isForced) OnForcedReloadStart?.Invoke(this);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            OnReloadProgress?.Invoke(this, elapsed / duration); // ← UI용 이벤트
            yield return null;
        }

        bulletCount = maxBulletCount;
        ChangeState(CharacterState.Idle);
        coverReloadLocked = false; // 리로드 정상 완료 → 엄폐 잠금 해제, 사격 재개 가능

        OnBulletCountChanged?.Invoke(this, bulletCount);

        if(isForced) OnForcedReloadEnd?.Invoke(this);
        
        OnReloadComplete();
    }

    protected virtual void OnReloadComplete() { }

    public Vector3 GetWorldTargetFromScreenPos(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, enemyLayer))
            return hit.point;

        return ray.GetPoint(1000f);
    }

    // 상태 변경의 진입점. 서브클래스가 sprite 처리 방식을 바꾸려면 ApplySprite 만 오버라이드.
    public virtual void ChangeState(CharacterState newState)
    {
        currentState = newState;
        ApplySprite(newState);
    }

    // 상태별 sprite 적용. 두 가지 경로:
    //   1) animSprites 가 할당되어 있으면 → 시트 시퀀스 애니메이션 (전환 → 본 상태 루프)
    //   2) 비어있으면 → 단일 sprite 시스템 (idleSprite/shootSprite/reloadSprite) 폴백
    // 서브클래스가 더 특수한 처리가 필요하면 오버라이드 가능.
    protected virtual void ApplySprite(CharacterState state)
    {
        // 시트 애니메이션 경로 (animSprites 우선)
        if (animSprites != null && animSprites.Length > 0)
        {
            if (animSpriteRenderer == null)
                animSpriteRenderer = GetComponent<SpriteRenderer>();

            if (animSpriteRenderer != null)
            {
                PlaySpriteSequence(state);
                return;
            }
        }

        // 단일 sprite 폴백 (기존 동작)
        switch (state)
        {
            case CharacterState.Idle:
                spriteRenderer.sprite = idleSprite;
                break;
            case CharacterState.Fire:
                spriteRenderer.sprite = shootSprite;
                break;
            case CharacterState.Reload:
                spriteRenderer.sprite = reloadSprite;
                break;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  시트 시퀀스 애니메이션 — 코루틴 기반 전환→루프 재생
    // ═══════════════════════════════════════════════════════
    private void PlaySpriteSequence(CharacterState state)
    {
        // Fire 는 매 발마다 사격 모션을 1회 새로 재생해야 하므로 가드 우회.
        // Idle / Reload 는 무한 루프 중이므로 같은 상태 재진입 시 재시작 비용 절약.
        bool isFire = state == CharacterState.Fire;
        if (!isFire && animPrevState == state && animCoroutine != null) return;

        int[] transition = GetTransitionSequence(animPrevState, state);
        int[] loop       = GetLoopForState(state);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        // Fire 는 ShootLoop 1회 재생 후 정지 (사격 후 자세 유지).
        // Idle / Reload 는 무한 반복.
        animCoroutine = StartCoroutine(PlayTransitionAndLoop(transition, loop, loopForever: !isFire));

        animPrevState = state;
    }

    /// <summary>
    /// 상태 전환 시 재생할 시퀀스. 정의되지 않은 전환은 null → 즉시 본 상태 루프 진입.
    /// 서브클래스가 다른 시트 구조를 쓰면 오버라이드.
    /// </summary>
    protected virtual int[] GetTransitionSequence(CharacterState? from, CharacterState to)
    {
        if (from == CharacterState.Idle   && to == CharacterState.Fire)   return DEFAULT_IDLE_TO_SHOOT;
        if (from == CharacterState.Fire   && to == CharacterState.Reload) return DEFAULT_SHOOT_TO_RELOAD;
        if (from == CharacterState.Reload && to == CharacterState.Idle)   return DEFAULT_RELOAD_TO_IDLE;
        return null;
    }

    /// <summary>본 상태의 무한 루프 시퀀스. 서브클래스가 다른 시트 구조를 쓰면 오버라이드.</summary>
    protected virtual int[] GetLoopForState(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Idle:   return DEFAULT_IDLE_LOOP;
            case CharacterState.Fire:   return DEFAULT_SHOOT_LOOP;
            case CharacterState.Reload: return DEFAULT_RELOAD_LOOP;
            default: return null;
        }
    }

    private IEnumerator PlayTransitionAndLoop(int[] transition, int[] loop, bool loopForever = true)
    {
        // ── 전환 시퀀스 (있을 때만, 1회 재생) ──
        if (transition != null)
        {
            for (int i = 0; i < transition.Length; i++)
            {
                int idx = transition[i];
                if (idx >= 0 && idx < animSprites.Length)
                    animSpriteRenderer.sprite = animSprites[idx];
                yield return new WaitForSeconds(frameDuration);
            }
        }

        // ── 본 상태 루프 ──
        //   loopForever = true  → Idle / Reload (무한 반복)
        //   loopForever = false → Fire (1회만 재생 후 정지, 마지막 프레임에 머무름)
        if (loop == null || loop.Length == 0) yield break;

        do
        {
            for (int i = 0; i < loop.Length; i++)
            {
                int idx = loop[i];
                if (idx >= 0 && idx < animSprites.Length)
                    animSpriteRenderer.sprite = animSprites[idx];
                yield return new WaitForSeconds(frameDuration);
            }
        } while (loopForever);

        // 1회 재생 후 종료. 마지막 sprite 가 화면에 그대로 유지됨.
        animCoroutine = null;
    }

    /// <summary>OnDisable 등에서 호출 — 시트 애니메이션 코루틴 정리.</summary>
    protected void StopSpriteAnimation()
    {
        if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
        animPrevState = null;
    }

    public void AddDamageRecord(float damage)
    {
        TotalDamageDealt += damage;
    }

    // 스테이지 시작 시 초기화
    public void ResetDamageRecord()
    {
        TotalDamageDealt = 0f;
    }

    void Awake()
    {
        //SpriteRenderer 같은 컴포넌트 참조는 Awake에서 처리
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        Initialize();
    }

    protected virtual void OnEnable() {}

    protected virtual void OnDisable()
    {
        // 시트 애니메이션 코루틴 정리 (서브클래스가 base.OnDisable() 호출 시 자동 처리)
        StopSpriteAnimation();
    }

    // 캐릭터 AI용 사격 (CrossHair 우회)
    public virtual void TryFireAtTarget(Vector3 worldTarget)
    {
        if (!survive) return;

        // 쿨타임 중 → Idle
        if (Time.time < nextFireTime)
            return;

        // 탄창 없음 → 리로드
        if (bulletCount <= 0)
        {
            TryReload();
            return;
        }

        // 사격
        ChangeState(CharacterState.Fire);
        bulletCount--;
        OnBulletConsumed?.Invoke(this, 1);
        nextFireTime = Time.time + fireRate;
        OnBulletCountChanged?.Invoke(this, bulletCount);
        FireBulletAtTarget(worldTarget);

        if (bulletCount == 0) TryReload();
    }

    public void Heal(float amount)
    {
        if (!survive) return;
        hp = Mathf.Min(hp + amount, maxHp);
        OnStatChanged?.Invoke(this);
    }

    public void AddBurstGauge(float amount)
    {
        chargingBurstGauge =
            Mathf.Min(chargingBurstGauge + amount, 100f);
    }

    public virtual void OnStopFiring() { }

    protected virtual void FireBulletAtTarget(Vector3 worldTarget)
    {
        if (bulletPool == null || muzzlePoint == null) return;

        Vector3 fireDir = (worldTarget - muzzlePoint.position).normalized;

        // SG: 산탄 발사 (AI 도 동일 패턴 적용)
        if (weaponType == WeaponType.SG)
        {
            SpawnShotgunSpread(fireDir);
            return;
        }

        SpawnSingleBullet(muzzlePoint.position, fireDir, FinalAttackDamage);
    }

    public virtual void StopAllSounds() { }
}
