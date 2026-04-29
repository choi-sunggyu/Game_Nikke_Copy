using System;
using UnityEngine;

public class Viper : CharacterBase
{
    public override void Initialize()
    {
        // GDD 기준:
        // - 저격소총: 탄창 5발
        // - 강제 리로드 시간: 3.0초
        // - 버스트 차징량: 높음
        // - HP: 100
        // - attackDamage : 50
        maxHp = 100;
        hp = maxHp;
        maxBulletCount = 5;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 3.0f;
        chargingBurstGauge = 20;
        burstCoolTime = 20.0f;
        skillCoolTime = 10.0f;
        attackDamage = 50;
        survive = true;
    }

    public void OnEnable()
    {
        InputManager.OnFireRelease += HandleFireRelease;
    }

    public override void UseSkill()
    {
        // Viper의 스킬 사용 로직
    }

    public override void UseBurst()
    {
        // Viper의 버스트 사용 로직
    }

    public override void TryFire()
    {
        // 조준만 (스프라이트 shoot으로)
        // 실제 사격은 HandleFireRelease에서
        if(IsAlive && bulletCount > 0)
        {
            StopReload();
            ChangeState(CharacterState.Fire);
        }
        else if(bulletCount == 0)
        {
            Debug.Log("탄창이 없습니다. 리로딩 중입니다.");
        }
    }

    void HandleFireRelease()
    {
        if(IsAlive && CurrentState == CharacterState.Fire && bulletCount > 0)
        {
            bulletCount--;
            if(bulletCount == 0)
            {
                TryReload();  // 강제 리로딩
            }
            else
            {
                ChangeState(CharacterState.Idle);
            }
        }
        Debug.Log($"터치 해제로 저격 소총 사격 / survive: {survive} / state: {CurrentState} / bullet: {bulletCount}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDisable()
    {
        // OnFireRelease 해제
        InputManager.OnFireRelease -= HandleFireRelease;
    }
}
