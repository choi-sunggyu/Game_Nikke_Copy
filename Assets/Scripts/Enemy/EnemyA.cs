using UnityEngine;

public class EnemyA : EnemyBase // 원거리 적
{
    public override void Attack()
    {
        throw new System.NotImplementedException();
    }

    public override void Initialize()
    {
        hp = 100f;
        maxHp = 100f;
        attackDamage = 10f;
        speed = 0f; // 원거리 적이므로 이동하지 않음
        survive = true;
        attackDelay = 5f;
        currentLayer = 20;
    }

    public override void Jump()
    {
        // EnemyA는 점프하지 않음
        throw new System.NotImplementedException();
    }

    public override void Move()
    {
        // EnemyA는 이동하지 않음
        throw new System.NotImplementedException();
    }

    void Start()
    {
        Initialize();
    }
    void Update()
    {
        
    }
}
