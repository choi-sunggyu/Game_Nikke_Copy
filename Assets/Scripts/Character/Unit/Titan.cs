using System.Collections;
using UnityEngine;

public class Titan : CharacterBase
{
    private float currentFireRate;
    private float minFireRate;
    private float maxFireRate;
    private int shotsFired;
    private float nextTitanFireTime;
    private bool isLooping = false;
    private const string TitanBuffId = "Titan_DamageBuff";
    private const float buffDuration = 10f;
    private const float buffMultiplier = 1.2f;
    private const float starDamageRatio = 0.4f;
    private const float starFireInterval = 0.25f;
    private const float starSpeed = 100f;
    private const string TitanBuff40 =
        "Titan_40";

    private const string TitanBuff20 =
        "Titan_20";

    private CharacterManager characterManager;
    private WaveManager waveManager;

    [Header("── 사운드 클립 ─────────────────────")]
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip spinUpClip;
    [SerializeField] private AudioClip fireLoopClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private GameObject buffStarPrefab;    // 버프 별 스프라이트 프리팹
    [SerializeField] private GameObject attackStarPrefab;  // 공격 별 프리팹

    [Header("── 스프라이트 애니메이션 ──────────")]
    [Tooltip("Titan 본체의 SpriteRenderer. 비워두면 GetComponent 로 자동 탐색")]
    [SerializeField] private SpriteRenderer titanSpriteRenderer;
    [Tooltip("Sprite Editor 로 잘라낸 10개 슬라이스 (프레임 01 → 인덱스 0, 프레임 10 → 인덱스 9)")]
    [SerializeField] private Sprite[] animSprites;
    [Tooltip("한 프레임당 표시 시간 (20fps = 0.05s)")]
    [SerializeField] private float frameDuration = 0.05f;

    // 시퀀스 정의 (배열 인덱스 = 프레임 번호 - 1)
    private static readonly int[] IdleLoop      = { 9 };                          // 10
    private static readonly int[] ShootLoop     = { 0, 1 };                       // 1 ↔ 2
    private static readonly int[] ReloadLoop    = { 7 };                          // 7
    private static readonly int[] IdleToShoot   = { 9, 5, 4, 3, 2, 0, 1 };        // 10 → 6 → 5 → 4 → 3 → 1 → 2
    private static readonly int[] ShootToReload = { 0, 1, 2, 3, 4, 5, 6, 7 };     // 1/2 → 3 → 4 → 5 → 6 → 7 → 8
    private static readonly int[] ReloadToIdle  = { 7, 8, 9 };                    // 8 → 9 → 10

    private CharacterState? animPrevState = null;
    private Coroutine        animCoroutine;

    private AudioSource singleShotSource;
    private AudioSource spinUpSource;
    private AudioSource loopSource;
    private AudioSource reloadSource;

    private const int LoopStartShots = 5;

    void Start()
    {
        Initialize();
        characterManager = FindAnyObjectByType<CharacterManager>();
        waveManager = FindAnyObjectByType<WaveManager>();
    }

    public override void Initialize()
    {
        maxHp = 200;
        hp = maxHp;
        maxBulletCount = 400;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 1.5f;
        chargingBurstGauge = 10;
        burstCoolTime = 20.0f;
        skillCoolTime = 10.0f;
        attackDamage = 10;
        survive = true;
        minFireRate = 1f / 70f;
        maxFireRate = 1f / 3f;
        currentFireRate = maxFireRate;
        shotsFired = 0;
        nextTitanFireTime = 0;
        bulletSpeed = 500f;

        // 스프라이트 애니메이션용 SR 자동 탐색
        if (titanSpriteRenderer == null)
            titanSpriteRenderer = GetComponent<SpriteRenderer>();

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 0.5f;

        spinUpSource = gameObject.AddComponent<AudioSource>();
        spinUpSource.clip = spinUpClip;
        spinUpSource.loop = true;
        spinUpSource.volume = 0.3f;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.clip = fireLoopClip;
        loopSource.loop = true;
        loopSource.volume = 0.3f;

        reloadSource = gameObject.AddComponent<AudioSource>();
        reloadSource.clip = reloadClip;
        reloadSource.loop = false;
        reloadSource.volume = 1.2f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        InputManager.OnFireRelease += ResetFireRate;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputManager.OnFireRelease -= ResetFireRate;
        StopAllSounds(); // ← 비활성화 시 모든 사운드 즉시 정지

        // 애니메이션 코루틴 정리
        if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
        animPrevState = null;
    }

    public override void TryFire()
    {
        if (!survive) return;
        if (bulletCount == 0) return;
        if (Time.time < nextTitanFireTime) return;

        StopReload();
        StopReloadSound(); // ← 리로드음 중단
        ChangeState(CharacterState.Fire);
        ProcessShot();
        FireBullet();
    }

    public override void TryFireAtTarget(Vector3 worldTarget)
    {
        if (!survive) return;
        if (CurrentState == CharacterState.Reload) return;
        if (Time.time < nextTitanFireTime) return;

        if (bulletCount <= 0)
        {
            TryReload();
            return;
        }

        ChangeState(CharacterState.Fire);
        ProcessShot();
        FireBulletAtTarget(worldTarget);
    }

    private void ProcessShot()
    {
        bulletCount--;
        shotsFired++;

        currentFireRate = Mathf.Lerp(maxFireRate, minFireRate,
                          Mathf.Clamp01(shotsFired / (float)LoopStartShots));
        nextTitanFireTime = Time.time + currentFireRate;

        InvokeBulletCountChanged(this, bulletCount);
        PlayFireSound();

        if (bulletCount == 0) TryReload();
    }

    private void PlayFireSound()
    {
        if (!IsActiveCharacter) return; // ← 비활성 캐릭터 차단

        if (shotsFired <= LoopStartShots)
        {
            if (!spinUpSource.isPlaying)
                spinUpSource.Play();

            float t = Mathf.Clamp01(shotsFired / (float)LoopStartShots);
            singleShotSource.pitch = Mathf.Lerp(0.5f, 1.0f, t);
            singleShotSource.PlayOneShot(singleShotClip);
        }
        else if (!isLooping)
        {
            spinUpSource.Stop();
            singleShotSource.Stop();
            loopSource.Play();
            isLooping = true;
        }
    }

    public override void TryReload()
    {
        if (CurrentState == CharacterState.Reload) return;
        base.TryReload();
        PlayReloadSound();
    }

    private void PlayReloadSound()
    {
        if (!IsActiveCharacter) return; // ← 비활성 캐릭터 차단
        if (reloadClip == null) return;
        StopAllSounds(); // ← 발사음과 리로드음이 겹치지 않도록 모든 사운드 중단
        reloadSource.clip = reloadClip;
        reloadSource.Play();
    }

    private void StopReloadSound()
    {
        if (reloadSource != null && reloadSource.isPlaying)
            reloadSource.Stop();
    }

    private void StopFireSound()
    {
        isLooping = false;
        spinUpSource?.Stop();
        singleShotSource?.Stop();
        loopSource?.Stop();
    }

    public override void StopAllSounds()
    {
        StopFireSound();
        StopReloadSound();
    }

    void ResetFireRate()
    {
        StopAllSounds();
        currentFireRate = maxFireRate;
        shotsFired = 0;
        nextTitanFireTime = 0f;
    }

    public override void OnStopFiring()
    {
        ResetFireRate();
    }

    private IEnumerator StarAttackRoutine()
    {
        float elapsed = 0f;

        while (elapsed < buffDuration)
        {
            elapsed += starFireInterval;
            yield return new WaitForSeconds(starFireInterval);

            var enemies = waveManager.ActiveEnemies;
            if (enemies == null || enemies.Count == 0) continue;

            // 무작위 적 선택
            EnemyBase target = enemies[Random.Range(0, enemies.Count)];
            if (target == null || !target.IsAlive) continue;

            // 캐릭터 주변 랜덤 위치에서 별 생성
            Vector3 spawnOffset = (Vector3)Random.insideUnitCircle * 1.5f;
            Vector3 spawnPos = transform.position + spawnOffset;

            GameObject star = Instantiate(attackStarPrefab, spawnPos, Quaternion.identity);
            float starDamage = attackDamage * attackDamageMultiplier * starDamageRatio;
            star.GetComponent<AttackStar>()?.Init(starDamage, starSpeed, target);
        }
    }

    public override void UseSkill()
    {
        foreach(var ally in BattleManager.Instance.Team)
        {
            if(ally == null || !ally.IsAlive)
                continue;

            if(ally.UsedBurstThisCycle)
            {
                Debug.Log($"[Titan] UseSkill - 버스트 사용한 아군에게 40% 버프 적용 / ally: {ally.gameObject.name}");
                ally.ApplyDamageBuff(
                    1.4f,
                    15f,
                    TitanBuff40);
            }
            else
            {
                Debug.Log($"[Titan] UseSkill - 버스트 안 쓴 아군에게 20% 버프 적용 / ally: {ally.gameObject.name}");
                ally.ApplyDamageBuff(
                    1.2f,
                    15f,
                    TitanBuff20);
            }
        }
    }
    public override void UseBurst()
    {
        if (waveManager == null || characterManager == null) return;

        UsedBurstThisCycle = true;
        // 팀 전체 공격력 버프 + 별 이펙트
        foreach (var character in characterManager.Characters)
        {
            if (character == null || !character.IsAlive) continue;

            character.ApplyDamageBuff(buffMultiplier, buffDuration, TitanBuffId);
            GameObject star = Instantiate(buffStarPrefab, character.transform.position, Quaternion.identity);
            star.transform.SetParent(character.transform);
            star.GetComponent<BuffStarEffect>()?.Show(buffDuration);
        }

        // 연속 공격 코루틴 시작
        StartCoroutine(StarAttackRoutine());
    }

    // ═══════════════════════════════════════════════════════
    //  스프라이트 시퀀스 애니메이션
    //  - 본 상태(idle/shoot/reload) 진입 시 ChangeState → ApplySprite 가 호출되면
    //    이전 상태와의 전환 시퀀스를 먼저 1회 재생한 뒤 본 상태 루프를 무한 재생.
    //  - 정의되지 않은 전환은 즉시 본 상태로 점프 (예: bullet 가득 상태에서 Shoot→Idle 등 드문 케이스)
    // ═══════════════════════════════════════════════════════
    protected override void ApplySprite(CharacterState state)
    {
        // 스프라이트 배열 미할당 시 부모의 단일-스프라이트 시스템으로 폴백
        if (animSprites == null || animSprites.Length == 0)
        {
            base.ApplySprite(state);
            return;
        }
        if (titanSpriteRenderer == null)
        {
            Debug.LogWarning("[Titan] titanSpriteRenderer 가 비어있음 → ApplySprite 스킵");
            return;
        }
        // 같은 상태로 재진입하는 경우 코루틴 재시작 비용 절약
        if (animPrevState == state && animCoroutine != null) return;

        int[] transition = GetTransitionSequence(animPrevState, state);
        int[] loop       = GetLoopForState(state);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(PlayTransitionAndLoop(transition, loop));

        animPrevState = state;
    }

    private int[] GetTransitionSequence(CharacterState? from, CharacterState to)
    {
        if (from == CharacterState.Idle   && to == CharacterState.Fire)   return IdleToShoot;
        if (from == CharacterState.Fire   && to == CharacterState.Reload) return ShootToReload;
        if (from == CharacterState.Reload && to == CharacterState.Idle)   return ReloadToIdle;
        return null; // 정의되지 않은 전환 → 즉시 본 상태
    }

    private int[] GetLoopForState(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Idle:   return IdleLoop;
            case CharacterState.Fire:   return ShootLoop;
            case CharacterState.Reload: return ReloadLoop;
            default: return null;
        }
    }

    private IEnumerator PlayTransitionAndLoop(int[] transition, int[] loop)
    {
        // ── 전환 시퀀스 (있을 때만, 1회 재생) ──
        if (transition != null)
        {
            for (int i = 0; i < transition.Length; i++)
            {
                int idx = transition[i];
                if (idx >= 0 && idx < animSprites.Length)
                    titanSpriteRenderer.sprite = animSprites[idx];
                yield return new WaitForSeconds(frameDuration);
            }
        }

        // ── 본 상태 루프 (무한) ──
        if (loop == null || loop.Length == 0) yield break;

        while (true)
        {
            for (int i = 0; i < loop.Length; i++)
            {
                int idx = loop[i];
                if (idx >= 0 && idx < animSprites.Length)
                    titanSpriteRenderer.sprite = animSprites[idx];
                yield return new WaitForSeconds(frameDuration);
            }
        }
    }
}