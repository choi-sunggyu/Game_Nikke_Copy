using UnityEngine;


public class Viper : CharacterBase
{
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip chargingClip;
    [SerializeField] private AudioClip reloadClip;

    private AudioSource singleShotSource;
    private AudioSource reloadSource;
    private AudioSource chargingSource;
    private bool hasPlayedCharging = false;

    public override void Initialize()
    {
        maxHp = 100;
        hp = maxHp;
        maxBulletCount = 5;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 2.5f;
        chargingBurstGauge = 20;
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
        InputManager.OnFireRelease += HandleFireRelease;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputManager.OnFireRelease -= HandleFireRelease;
        StopAllSounds(); // 비활성화 시 모든 사운드 즉시 정지
    }

    void Start()
    {
        Initialize();
    }

    public override void TryFire()
    {
        if (IsAlive && bulletCount > 0)
        {
            StopReload();
            StopReloadSound(); // ← 리로드음 중단
            PlayChargingSound();
            ChangeState(CharacterState.Fire);
        }
    }

    void HandleFireRelease()
    {
        hasPlayedCharging = false; // ← 클릭 해제 시 플래그 리셋

        if (IsAlive && CurrentState == CharacterState.Fire && bulletCount > 0)
        {
            bulletCount--;
            InvokeBulletCountChanged(this, bulletCount);
            PlayFireSound();
            FireBullet();

            if (bulletCount == 0) TryReload();
            else ChangeState(CharacterState.Idle);
        }
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
        Debug.Log($"{gameObject.name} TryReload 호출 / bulletCount: {bulletCount} / state: {CurrentState}");
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
    public override void UseBurst() { }
}