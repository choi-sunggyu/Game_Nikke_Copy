using System.Collections.Generic;
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
        reloadTime = 1.0f;
        chargingBurstGauge = 5;
        burstCoolTime = 15.0f;
        skillCoolTime = 10.0f;
        attackDamage = 20;
        survive = true;
        fireRate = 1f / 20f;
        bulletSpeed = 500f;

        singleShotSource = gameObject.AddComponent<AudioSource>();
        singleShotSource.loop = false;
        singleShotSource.volume = 0.3f;

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
        var waveManager = FindAnyObjectByType<WaveManager>();
        var characterManager = FindAnyObjectByType<CharacterManager>();
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