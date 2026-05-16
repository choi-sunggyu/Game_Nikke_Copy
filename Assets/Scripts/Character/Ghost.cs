using UnityEngine;

public class Ghost : CharacterBase
{
    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip reloadClip;

    private AudioSource singleShotSource;
    private AudioSource reloadSource;

    public override void Initialize()
    {
        maxHp = 100;
        hp = maxHp;
        maxBulletCount = 120;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 2.0f;
        chargingBurstGauge = 5;
        burstCoolTime = 15.0f;
        skillCoolTime = 10.0f;
        attackDamage = 20;
        survive = true;
        fireRate = 1f / 12f;
        bulletSpeed = 500f;

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 0.5f;

        reloadSource = gameObject.AddComponent<AudioSource>();
        reloadSource.loop = false;
        reloadSource.volume = 0.8f;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        //StopAllSounds(); // ← 비활성화 시 즉시 정지
    }

    public override void TryFire()
    {
        if (!survive) return;
        if (bulletCount <= 0) return;
        if (Time.time < NextFireTime) return;

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
    public override void UseBurst() { }
}