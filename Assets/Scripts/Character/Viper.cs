using System.Collections.Generic;
using UnityEngine;


public class Viper : CharacterBase
{
    [Header("차지 설정")]
    [SerializeField] private float maxChargeTime = 1.13f;
    private float chargeStartTime = -1f;
    private bool isCharging = false;

    [Header("소리 설정")]
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip chargingClip;
    [SerializeField] private AudioClip reloadClip;

    [Header("빔 설정")]
    [SerializeField] private GameObject viperBeamPrefab;

    private AudioSource singleShotSource;
    private AudioSource reloadSource;
    private AudioSource chargingSource;
    private bool hasPlayedCharging = false;
    private bool isFireHeld = false;

    public override void Initialize()
    {
        maxHp = 100;
        hp = maxHp;
        maxBulletCount = 5;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 1.0f;
        chargingBurstGauge = 20; // 집중사격 게이지 충전량
        burstCoolTime = 20.0f;
        skillCoolTime = 10.0f;
        attackDamage = 50;
        survive = true;
        bulletSpeed = 800f;

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 1.2f;

        reloadSource = gameObject.AddComponent<AudioSource>();
        reloadSource.loop = false;
        reloadSource.volume = 1.2f;

        chargingSource = gameObject.AddComponent<AudioSource>();
        chargingSource.loop = false;
        chargingSource.volume = 1.0f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        InputManager.OnFirePress   += HandleFirePress;
        InputManager.OnFireRelease += HandleFireRelease;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputManager.OnFirePress   -= HandleFirePress;
        InputManager.OnFireRelease -= HandleFireRelease;
        StopAllSounds(); // 비활성화 시 모든 사운드 즉시 정지
    }

    private void HandleFirePress()
    {
        isFireHeld = true; // ← 클릭 시작 시 기록
    }

    void Start()
    {
        Initialize();
    }

    public override void TryFire()
    {
        if (IsCoverReloadLocked) return;
        if (!IsAlive || bulletCount <= 0) return;

        // 이미 차지 중이면 상태 유지만 (chargeStartTime 갱신 금지)
        if (isCharging)
        {
            ChangeState(CharacterState.Fire);
            return;
        }

        // 첫 클릭 프레임에만 실행
        StopReload();
        StopReloadSound();
        PlayChargingSound();
        ChangeState(CharacterState.Fire);
        chargeStartTime = Time.time; // ← 최초 1회만 기록
        isCharging = true;
    }

    void HandleFireRelease()
    {
        isFireHeld = false;
        hasPlayedCharging = false; // ← 클릭 해제 시 플래그 리셋

        if (IsAlive && CurrentState == CharacterState.Fire && bulletCount > 0)
        {
            // 차지 비율 계산
            float chargeRatio = isCharging
                ? Mathf.Clamp01((Time.time - chargeStartTime) / maxChargeTime)
                : 0f;
            isCharging = false;

            bulletCount--;
            InvokeBulletCountChanged(this, bulletCount);
            PlayFireSound();
            FireChargedBullet(chargeRatio);

            if (bulletCount == 0) TryReload();
            else ChangeState(CharacterState.Idle);
        }
    }

    private void FireChargedBullet(float chargeRatio)
    {
        if (bulletPool == null || muzzlePoint == null) return;

        Ray camRay = Camera.main.ScreenPointToRay(crossHair.CrossHairPosition);
        Vector3 worldTarget = Physics.Raycast(camRay, out RaycastHit hit, 1000f)
            ? hit.point
            : camRay.GetPoint(1000f);

        Vector3 fireDir = (worldTarget - muzzlePoint.position).normalized;
        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        // 차지 100% = 150%, 0% = 0% (선형 보간)
        float chargedDamage = attackDamage * attackDamageMultiplier * Mathf.Lerp(0f, 1.5f, chargeRatio);

        bullet.GetComponent<BulletBase>()?.Init(chargedDamage, bulletSpeed, fireDir, chargingBurstGauge);
    }

    protected override void OnReloadComplete()
    {
        if (isFireHeld && IsAlive && bulletCount > 0)
            ChangeState(CharacterState.Fire); // ← 리로드 후 클릭 중이면 스코프 표시
    }

    // AI용
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
        else ChangeState(CharacterState.Idle);
    }

    public override void TryReload()
    {
        //Debug.Log($"{gameObject.name} TryReload 호출 / bulletCount: {bulletCount} / state: {CurrentState}");
        if (CurrentState == CharacterState.Reload) return;
        base.TryReload();
        PlayReloadSound();
    }

    private void PlayFireSound()
    {
        if (!IsActiveCharacter) return;
        StopAllSounds();
        singleShotSource.PlayOneShot(singleShotClip);
    }

    private void PlayReloadSound()
    {
        if (!IsActiveCharacter) return; // ← 비활성 캐릭터 사운드 차단
        if (reloadClip == null) return;
        reloadSource.clip = reloadClip;
        reloadSource.Play();
    }

    private void PlayChargingSound()
    {
        if (!IsActiveCharacter) return;
        if (chargingClip == null) return;
        if (hasPlayedCharging) return; // ← 이번 클릭에서 이미 재생했으면 스킵
        hasPlayedCharging = true;
        chargingSource.clip = chargingClip;
        chargingSource.Play();
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
    }

    public void AIFire()
    {
        HandleFireRelease();
    }

    public override void UseSkill() { }
    public override void UseBurst()
    {
        Debug.Log($"[Viper UseBurst] 호출 횟수 체크");

        //웨이브 매니저게에서 살아있는 적들 중 HP가 가장 높은 적을 찾아 집중사격 발사
        var waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager == null) return; // 웨이브 매니저가 없으면 스킬 사용 불가

        // HP 가장 높은 적 탐색
        EnemyBase target = null;
        float highestHp = float.MinValue;
        List<EnemyBase> topTargets = new List<EnemyBase>();

        foreach (var enemy in waveManager.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            if (enemy.Hp > highestHp)
            {
                highestHp = enemy.Hp;
                topTargets.Clear();
                topTargets.Add(enemy);
            }
            else if (Mathf.Approximately(enemy.Hp, highestHp))
            {
                topTargets.Add(enemy);
            }
        }

        if (topTargets.Count > 0)
            target = topTargets[Random.Range(0, topTargets.Count)];

        Debug.Log($"[Viper UseBurst] target: {target?.name} / topTargets: {topTargets.Count}");

        if (target == null) return;

        float burstDamage = attackDamage * attackDamageMultiplier * 20f;
        Debug.Log($"[Viper UseBurst] burstDamage: {burstDamage}");
        target.TakeDamage(burstDamage);

        Debug.Log($"[Viper UseBurst] 빔 생성 직전 / muzzlePoint: {muzzlePoint.position} / target: {target.transform.position}");
        GameObject beam = Instantiate(viperBeamPrefab, muzzlePoint.position, Quaternion.identity);
        beam.GetComponent<ViperBeamEffect>()?.Fire(muzzlePoint.position, target.transform.position);
        Debug.Log($"[Viper UseBurst] 빔 생성 완료");
    }
}