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

    // 시트 애니메이션(animSprites/animSpriteRenderer/frameDuration) 은 CharacterBase 로 일반화됨.
    // 인스펙터의 「시트 애니메이션 (15장 = 5+5+5)」 섹션에 15장 슬라이스 할당.

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
        // [점진적 마이그레이션] 하드코딩 폴백 — CharacterData(SO) 미할당 시 그대로 사용됨.
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
        bulletSpeed = 500f;

        // SO 가 할당되어 있으면 위 값들을 덮어쓰며 적용.
        ApplyData();

        // Titan 고유 — 스핀업 시스템 (SO 가 처리하지 않는 특수 필드)
        minFireRate = 1f / 70f;
        maxFireRate = 1f / 3f;
        currentFireRate = maxFireRate;
        shotsFired = 0;
        nextTitanFireTime = 0;

        // (시트 애니메이션 SR 자동 탐색은 CharacterBase.ApplySprite 안에서 처리됨)

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
        base.OnDisable(); // ← 시트 애니메이션 코루틴 정리도 base 에서 처리됨
        InputManager.OnFireRelease -= ResetFireRate;
        StopAllSounds(); // ← 비활성화 시 모든 사운드 즉시 정지
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

    // 시트 애니메이션(ApplySprite 오버라이드, 시퀀스 선택, 코루틴) 은 CharacterBase 로 이전됨.
    // Titan 은 기본 시퀀스(15장 5+5+5)를 그대로 사용. 다른 시퀀스가 필요하면
    // GetTransitionSequence / GetLoopForState 오버라이드.
}