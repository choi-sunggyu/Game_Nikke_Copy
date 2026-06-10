using UnityEngine;
public class DummyEnemy : EnemyBase
{
    public override void Attack()
    {
    }

    public override void Initialize()
    {
    }

    public override void Jump()
    {
    }

    public override void Move()
    {
    }

    public override void TakeDamage(float damage)
    {
        Debug.Log($"[DummyEnemy] 피격! 데미지: {damage}");
        Die(); // 즉시 사망 처리 (데미지와 상관없이)
    }

    public override void Die()
    {
        Debug.Log("[DummyEnemy] 사망!");
    }
}