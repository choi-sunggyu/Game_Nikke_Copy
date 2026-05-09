using UnityEngine;

public class EnemyA : EnemyBase
{
    [SerializeField] private ObjectPool bulletPool;
    [SerializeField] private Transform muzzlePoint;
    private float nextAttackTime;

    public override void Initialize()
    {
        hp = 100f;
        maxHp = 100f;
        attackDamage = 10f;
        attackDelay = 2f;  // 2초마다 1발
        survive = true;
        nextAttackTime = 0f;
    }

    void Update()
    {
        if (!IsAlive) return;
        if (Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackDelay;
        }
    }

    public override void Attack()
    {
        CharacterBase target = GetTarget();
        if (target == null || !target.IsAlive) return;

        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        EnemyBulletBase bulletBase = bullet.GetComponent<EnemyBulletBase>();
        
        // Z 포함해서 방향 계산
        Vector3 direction = (target.transform.position - muzzlePoint.position).normalized;
        bulletBase.Init(attackDamage, 15f, direction);
    }

    public override void Move() { }
    public override void Jump() { }
}