using UnityEngine;

public class Ghost : CharacterBase
{
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
        fireRate = 1f / 12f;  // 초당 12발
        bulletSpeed = 500f;
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
