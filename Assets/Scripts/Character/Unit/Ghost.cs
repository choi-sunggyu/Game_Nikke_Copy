using System.Collections.Generic;
using UnityEngine;

public class Ghost : CharacterBase
{
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip reloadClip;
    [Tooltip("RL 차지 사운드 — LauncherCrossHair 가 차지 시작 시 호출")]
    [SerializeField] private AudioClip chargingClip;

    private CharacterManager characterManager;
    private WaveManager waveManager;

    private AudioSource singleShotSource;
    private AudioSource reloadSource;
    private AudioSource chargingSource;
    private bool _chargingPlayedThisCycle = false;

    public override void Initialize()
    {
        // [점진적 마이그레이션] 하드코딩 폴백 — CharacterData(SO) 미할당 시 이 값들이 그대로 사용됨.
        maxHp = 100;
        hp = maxHp;
        maxBulletCount = 120;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 1.0f;
        chargingBurstGauge = 5;
        burstCoolTime = 15.0f;
        skillCoolTime = 10.0f;
        attackDamage = 20;
        survive = true;
        fireRate = 1f / 20f;
        bulletSpeed = 500f;

        // SO 가 할당되어 있으면 위 값들을 덮어쓰며 적용 (없으면 ApplyData 가 no-op).
        ApplyData();

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 0.3f;

        reloadSource = gameObject.AddComponent<AudioSource>();
        reloadSource.loop = false;
        reloadSource.volume = 0.8f;

        // RL 차지 사운드 (LauncherCrossHair 가 차지 시작 시 호출)
        chargingSource = gameObject.AddComponent<AudioSource>();
        chargingSource.loop = false;
        chargingSource.volume = 0.9f;
    }

    /// <summary>LauncherCrossHair 가 차지 시작 시 호출. 한 사이클에 1회만 재생.</summary>
    public void PlayChargingSound()
    {
        if (!IsActiveCharacter) return;
        if (chargingClip == null) return;
        if (_chargingPlayedThisCycle) return;

        _chargingPlayedThisCycle = true;
        chargingSource.clip = chargingClip;
        chargingSource.Play();
    }

    /// <summary>LauncherCrossHair 가 차지 취소/완료 시 호출. 다음 사이클 준비.</summary>
    public void StopChargingSound()
    {
        if (chargingSource != null && chargingSource.isPlaying)
            chargingSource.Stop();
        _chargingPlayedThisCycle = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        //StopAllSounds(); // ← 비활성화 시 즉시 정지
    }

    void Start()
    {
        Initialize();
        characterManager = FindAnyObjectByType<CharacterManager>();
        waveManager = FindAnyObjectByType<WaveManager>();
    }

    public override void TryFire()
    {
        if (!survive) return;
        if (bulletCount <= 0) return;
        // RL 은 차지 시간이 사격 간격 역할 → fireRate 무관
        // 다른 무기는 수동 입력 매 프레임 발화를 막기 위해 fireRate 유지
        if (weaponType != WeaponType.RL && Time.time < NextFireTime) return;

        StopReload();
        StopReloadSound();
        ChangeState(CharacterState.Fire);

        bulletCount--;
        SetNextFireTime(Time.time + fireRate);

        InvokeBulletCountChanged(this, bulletCount);
        PlayFireSound();
        FireBullet();

        if (bulletCount == 0) TryReload();
    }

    public override void TryFireAtTarget(Vector3 worldTarget)
    {
        if (!survive) return;
        if (CurrentState == CharacterState.Reload) return;
        // RL: CharacterAI 의 launcherChargeTime 시뮬레이션이 사격 간격을 보장 → fireRate 추가 체크 불필요
        if (weaponType != WeaponType.RL && Time.time < NextFireTime) return;

        if (bulletCount <= 0)
        {
            TryReload();
            return;
        }

        ChangeState(CharacterState.Fire);
        bulletCount--;
        SetNextFireTime(Time.time + fireRate);

        InvokeBulletCountChanged(this, bulletCount);
        PlayFireSound();
        FireBulletAtTarget(worldTarget);

        if (bulletCount == 0) TryReload();
    }

    public override void TryReload()
    {
        if (CurrentState == CharacterState.Reload) return;
        base.TryReload();
        PlayReloadSound();
    }

    private void PlayFireSound()
    {
        if (!IsActiveCharacter) return;
        StopReloadSound();
        singleShotSource.PlayOneShot(singleShotClip);
    }

    private void PlayReloadSound()
    {
        if (!IsActiveCharacter) return;
        if (reloadClip == null) return;
        reloadSource.clip = reloadClip;
        reloadSource.Play();
    }

    private void StopReloadSound()
    {
        if (reloadSource != null && reloadSource.isPlaying)
            reloadSource.Stop();
    }

    public override void StopAllSounds()
    {
        singleShotSource?.Stop();
        reloadSource?.Stop();
        chargingSource?.Stop();
        _chargingPlayedThisCycle = false;
    }

    public override void UseSkill() 
    {
        // 아군 누적 탄환 400발 -> 버스트 게이지 23.12% 즉시 충전
        foreach(var ally in BattleManager.Instance.Team)
        {
            Debug.Log($"[Ghost] UseSkill 호출 / ally: {ally.gameObject.name}");
            ally.AddBurstGauge(23.12f);
        }        
    }

    public override void UseBurst()
    {
        if (waveManager == null || characterManager == null) return;

        UsedBurstThisCycle = true;

        // 플래시 이펙트
        var enemies = new List<EnemyBase>(waveManager.ActiveEnemies);
        FlashEffect.Instance?.TriggerEnemyFlash(enemies);

        // 전체 적 스턴
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive)
                enemy.ApplyStun(2f);
        }

        // 팀 전체 HP 20% 회복
        foreach (var character in characterManager.Characters)
        {
            if (character != null && character.IsAlive)
                character.Heal(character.MaxHp * 0.2f);
        }
    }
}