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

    [SerializeField] private AudioClip singleShotClip;
    [SerializeField] private AudioClip spinUpClip;
    [SerializeField] private AudioClip fireLoopClip;
    [SerializeField] private AudioClip reloadClip;

    private AudioSource singleShotSource;
    private AudioSource spinUpSource;
    private AudioSource loopSource;
    private AudioSource reloadSource;

    private const int LoopStartShots = 5;

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

    public override void UseSkill() { }
    public override void UseBurst() { }
}