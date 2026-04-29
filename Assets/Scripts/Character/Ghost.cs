using UnityEngine;

public class Ghost : CharacterBase
{
    public override void Initialize()
    {
        // GDD 기준:
        // - 돌격 소총 : 탄창 30발
        // - 강제 리로드 시간: 2.0초
        // - 버스트 차징량: 낮음
        // - HP: 100
        // - attackDamage : 20
        maxHp = 100;
        hp = maxHp;
        maxBulletCount = 30;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 2.0f;
        chargingBurstGauge = 5;
        burstCoolTime = 15.0f;
        skillCoolTime = 10.0f;
        attackDamage = 20;
        survive = true;
    }

    public override void UseSkill()
    {
        // Ghost 스킬 사용 로직
    }

    public override void UseBurst()
    {
        // Ghost 버스트 사용 로직
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
