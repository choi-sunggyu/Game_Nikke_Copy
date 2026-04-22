using UnityEngine;

public class Titan : CharacterBase
{
    public override void Initialize()
    {
        // GDD 기준:
        // - 미니건 : 탄창 400발
        // - 강제 리로드 시간: 5.0초
        // - 버스트 차징량: 중간
        // - HP: 200
        // - attackDamage : 10
        maxHp = 200;
        hp = maxHp;
        maxBulletCount = 400;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 5.0f;
        chargingBurstGauge = 10;
        burstCoolTime = 20.0f;
        skillCoolTime = 10.0f;
        attackDamage = 10;
    }

    public override void UseSkill()
    {
        // Viper의 스킬 사용 로직
    }

    public override void UseBurst()
    {
        // Viper의 버스트 사용 로직
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
