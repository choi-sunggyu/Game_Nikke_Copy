using UnityEngine;

public class Viper : CharacterBase
{
    public override void Initialize()
    {
        // Viper 스탯 채워봐
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
