using UnityEngine;

public class EnemyB : EnemyBase
{
    [SerializeField] private Transform muzzlePoint;

    private float nextAttackTime;
    private float nextMoveTime;
    private bool isMoving;

    private Vector3 waypointA;
    private Vector3 waypointB;
    private Vector3 currentTarget;

    private float moveRange = 5f;   // 좌우 이동 범위
    private float moveSpeed = 2f;   // 이동 속도
    private float moveTilt = 30f;   // 이동 시 기울기 (도)
    private float moveInterval = 10f; // 이동 간격 (초)

    public override void Initialize()
    {
        hp = 150f;
        maxHp = 150f;
        attackDamage = 8f;
        attackDelay = 2f;
        survive = true;
        nextAttackTime = 0f;
        nextMoveTime = moveInterval;

        // Waypoint 자동 생성 (좌우)
        waypointA = transform.position + Vector3.left * moveRange;
        waypointB = transform.position + Vector3.right * moveRange;
        currentTarget = waypointB; // 첫 이동 목표
    }

    void Update()
    {
        if (!IsAlive) return;

        // 이동 타이머
        if (Time.time >= nextMoveTime && !isMoving)
        {
            StartMove();
        }

        if (isMoving)
        {
            MoveToTarget();
        }
        else
        {
            // 이동 중이 아닐 때만 공격
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackDelay;
            }
        }
    }

    void StartMove()
    {
        isMoving = true;
        // 반대편 Waypoint로 타겟 전환
        currentTarget = (currentTarget == waypointA) ? waypointB : waypointA;
    }

    void MoveToTarget()
    {
        // 이동 방향에 따라 기울기 방향 결정
        float dirX = currentTarget.x - transform.position.x;
        float tilt = dirX > 0 ? -moveTilt : moveTilt; // 이동 방향으로 기울임

        // 기울기 적용
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, tilt),
            Time.deltaTime * 10f
        );

        // 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget,
            moveSpeed * Time.deltaTime
        );

        // 도착 전 기울기 적용
        tilt = Mathf.Lerp(tilt, 0f, Time.deltaTime * 5f); // 도착할수록 기울기 복원
        if (Vector3.Distance(transform.position, currentTarget) > 0.3f)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0f, 0f, tilt),
                Time.deltaTime * 10f
            );
        }

        // 도착 체크
        if (Vector3.Distance(transform.position, currentTarget) < 0.05f)
        {
            transform.position = currentTarget;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f); // 기울기 복원
            isMoving = false;
            nextMoveTime = Time.time + moveInterval;
            nextAttackTime = Time.time + attackDelay; // 도착 후 공격 딜레이
        }
    }

    public override void Attack()
    {
        CharacterBase target = GetTarget();
        if (target == null || !target.IsAlive) return;

        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        EnemyBulletBase bulletBase = bullet.GetComponent<EnemyBulletBase>();
        Vector3 direction = (target.transform.position - muzzlePoint.position).normalized;
        bulletBase.Init(attackDamage, 15f, direction);
    }

    public override void Move() { }
    public override void Jump() { }
}