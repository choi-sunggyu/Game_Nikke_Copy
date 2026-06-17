using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Astro — 3버스트, SG(NOVA-12 Gravity Shotgun) 캐릭터.
///
/// 소속:    Eclipse Union
/// 클래스:  Shotgunner
/// 역할:    Close-Range DPS / Crowd Control
/// 슬로건:  "Gravity never lies. It pulls everything back to the truth."
///
/// 무기:    NOVA-12 — 12Ga High-Density Slug, 유효 사거리 12m, Gravity Manipulation
/// 스킬:
///   PASSIVE   Zero-G Adaptation  — 이동 시 짧은 시간 동안 이동속도/회피율 증가 (TODO)
///   SKILL 1   Gravity Collapse   — 전방 적 끌어당기고 이속/방어력 감소 (TODO)
///   SKILL 2   Singularity Blast  — 중력 핵 폭발, 끌어당긴 적에게 강타 (TODO)
///   ULTIMATE  Supernova          — 소형 인공 태양, 광역 지속 피해 + 화상 (UseBurst 에서 구현)
/// </summary>
public class Astro : CharacterBase
{
    [Header("── Astro 전용 ─────────────────────")]
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private GameObject supernovaPrefab; // ULTIMATE 이펙트 (옵션)

    private AudioSource singleShotSource;
    private AudioSource reloadSource;
    private WaveManager waveManager;

    // ULTIMATE 파라미터
    private const float SUPERNOVA_DURATION       = 5.0f; // 지속 시간 (초)
    private const float SUPERNOVA_TICK_INTERVAL  = 0.5f; // 틱 간격
    private const float SUPERNOVA_TICK_MULTIPLIER = 2.0f; // 발당 데미지 배율 (틱당)

    public override void Initialize()
    {
        // [점진적 마이그레이션] 하드코딩 폴백 — CharacterData(SO) 미할당 시 그대로 사용됨.
        maxHp              = 1300;
        hp                 = maxHp;
        maxBulletCount     = 8;
        bulletCount        = maxBulletCount;
        maxShield          = 700;
        shield             = maxShield;
        reloadTime         = 2.5f;
        chargingBurstGauge = 60;
        burstCoolTime      = 20.0f;
        skillCoolTime      = 10.0f;
        attackDamage       = 150;
        survive            = true;
        fireRate           = 1.0f;   // 단발 1초 간격
        bulletSpeed        = 600f;
        weaponType         = WeaponType.SG;
        burstNumber        = 3;       // ★ 3버스트

        // SO 가 할당되어 있으면 위 값들을 덮어쓰며 적용.
        ApplyData();

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 0.7f;

        reloadSource = gameObject.AddComponent<AudioSource>();
        reloadSource.loop = false;
        reloadSource.volume = 0.8f;
    }

    void Start()
    {
        Initialize();
        waveManager = FindAnyObjectByType<WaveManager>();
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
    /// ULTIMATE — Supernova
    /// 화면 중앙에 인공 태양 생성. 5초 동안 0.5초마다 살아있는 모든 적에게 광역 피해.
    /// 총 10틱 × (attackDamage × 2.0 배율) = 큰 데미지.
    /// </summary>
    public override void UseBurst()
    {
        UsedBurstThisCycle = true;
        if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager == null) return;

        // 이펙트 (화면 중앙)
        if (supernovaPrefab != null)
        {
            Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 25f);
            Vector3 centerWorld  = Camera.main.ScreenToWorldPoint(centerScreen);
            Instantiate(supernovaPrefab, centerWorld, Quaternion.identity);
        }

        StartCoroutine(SupernovaRoutine());
    }

    private IEnumerator SupernovaRoutine()
    {
        float elapsed = 0f;
        float tickDamage = attackDamage * attackDamageMultiplier * SUPERNOVA_TICK_MULTIPLIER;

        while (elapsed < SUPERNOVA_DURATION)
        {
            // 매 틱마다 살아있는 적 스냅샷 후 데미지 적용 (열거 중 변경 방지)
            var snapshot = new List<EnemyBase>(waveManager.ActiveEnemies);
            foreach (var enemy in snapshot)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                enemy.TakeDamage(tickDamage); // 거리 무관 단순 피해 (광역 ULTIMATE)
            }

            elapsed += SUPERNOVA_TICK_INTERVAL;
            yield return new WaitForSeconds(SUPERNOVA_TICK_INTERVAL);
        }
    }
}