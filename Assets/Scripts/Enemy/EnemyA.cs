using UnityEngine;

public class EnemyA : EnemyBase
{
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

        Vector3 direction = (target.transform.position - muzzlePoint.position).normalized;
        Debug.Log($"[EnemyA] bulletPool: {bulletPool} / muzzlePoint: {muzzlePoint}");
        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        EnemyBulletBase bulletBase = bullet.GetComponent<EnemyBulletBase>();
        bulletBase.Init(attackDamage, 15f, direction);
    }

    public override void Move() { }
    public override void Jump() { }
}