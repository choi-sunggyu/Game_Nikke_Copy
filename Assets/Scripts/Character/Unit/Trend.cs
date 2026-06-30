using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trend — 2버스트, AR(LIVE-MAKER Social Media Assault Rifle) 캐릭터.
///
/// 소속:    Eclipse Union
/// 클래스:  Supporter / Buffer
/// 역할:    Team Buffer (아군 전체 강화)
/// 슬로건:  "Likes are power. I make the world trend."
///
/// 무기:    LIVE-MAKER — 5.56mm Trend Core, 유효 사거리 500m, Viral Uplink System
/// 스킬:
///   PASSIVE   Influencer      — 전투 시작 시 아군 전체 공격력 소폭 증가 (TODO)
///   SKILL 1   Viral Shot      — 적 전체에 '바이럴 표식', 표식 적에게 가하는 피해 증가 (TODO)
///   SKILL 2   Hashtag Boost   — 아군 전체 공격력 / 치명타 확률 증가 (TODO)
///   ULTIMATE  Trending Now    — 홀로그램 무대 생성, 아군 전체 공격력/치명타/이동속도 대폭 증가 (UseBurst)
/// </summary>
public class Trend : CharacterBase
{
    [Header("── Trend 전용 ─────────────────────")]
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private GameObject trendingStagePrefab; // ULTIMATE 홀로그램 무대 이펙트 (옵션)

    private AudioSource singleShotSource;
    private AudioSource reloadSource;
    private CharacterManager characterManager;

    // ULTIMATE 파라미터 (Trending Now)
    private const string TREND_BURST_DAMAGE_BUFF_ID   = "Trend_BurstDamageBuff";
    private const string TREND_BURST_CRIT_BUFF_ID     = "Trend_BurstCritBuff";
    private const float  BURST_DAMAGE_MULTIPLIER      = 1.5f; // ×50% 공격력
    private const float  BURST_CRIT_RATE_BONUS        = 0.2f; // +20% 치명타 확률
    private const float  BURST_DURATION               = 10.0f;

    public override void Initialize()
    {
        // [점진적 마이그레이션] 하드코딩 폴백 — CharacterData(SO) 미할당 시 그대로 사용됨.
        maxHp              = 1100;
        hp                 = maxHp;
        maxBulletCount     = 30;
        bulletCount        = maxBulletCount;
        maxShield          = 500;
        shield             = maxShield;
        reloadTime         = 1.0f;
        chargingBurstGauge = 15;
        burstCoolTime      = 18.0f;
        skillCoolTime      = 10.0f;
        attackDamage       = 20;     // 지원형이라 데미지 약간 낮음
        survive            = true;
        fireRate           = 0.08f;  // AR 표준
        bulletSpeed        = 500f;
        weaponType         = WeaponType.AR;
        burstNumber        = 2;       // ★ 2버스트

        // SO 가 할당되어 있으면 위 값들을 덮어쓰며 적용.
        ApplyData();

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 0.4f;

        reloadSource = gameObject.AddComponent<AudioSource>();
        reloadSource.loop = false;
        reloadSource.volume = 0.7f;
    }

    void Start()
    {
        Initialize();
        characterManager = FindAnyObjectByType<CharacterManager>();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllSounds();
    }

    public override void TryFire()
    {
        if (!IsAlive) return;
        if (bulletCount <= 0) return;
        if (Time.time < NextFireTime) return;
        if (IsCoverReloadLocked) return;

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
        if (Time.time < NextFireTime) return;

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
    }

    public override void UseSkill() { }

    /// <summary>
    /// ULTIMATE — Trending Now
    /// 홀로그램 무대 생성. 아군 전체에게 10초간:
    ///   • 공격력 ×1.5 (ApplyDamageBuff)
    ///   • 치명타 확률 +20% (ApplyCriticalRateBuff)
    /// </summary>
    public override void UseBurst()
    {
        UsedBurstThisCycle = true;
        if (characterManager == null) characterManager = FindAnyObjectByType<CharacterManager>();
        if (characterManager == null) return;

        // 이펙트 (자기 위치에 홀로그램 무대)
        if (trendingStagePrefab != null)
        {
            GameObject stage = Instantiate(trendingStagePrefab, transform.position, Quaternion.identity);
            stage.transform.SetParent(transform);
        }

        // 아군 전체 버프 — 살아있는 캐릭터에게만
        foreach (var ally in characterManager.Characters)
        {
            if (ally == null || !ally.IsAlive) continue;

            ally.ApplyDamageBuff(
                BURST_DAMAGE_MULTIPLIER,
                BURST_DURATION,
                TREND_BURST_DAMAGE_BUFF_ID);

            ally.ApplyCriticalRateBuff(
                BURST_CRIT_RATE_BONUS,
                BURST_DURATION,
                TREND_BURST_CRIT_BUFF_ID);
        }

        Debug.Log("[Trend UseBurst] Trending Now! 아군 전체 공격력 ×1.5 + 치명타 +20% (10초)");
    }
}
