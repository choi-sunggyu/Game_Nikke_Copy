using System.Collections;
using UnityEngine;

public class EnemyB : EnemyBase
{

    [Header("Spawn Animation")]
    [SerializeField] private float slideDuration = 1.0f; // 슬라이드 등장 소요 시간

    private float nextAttackTime;
    private float nextMoveTime;
    private bool isMoving;

    private Vector3 waypointA;
    private Vector3 waypointB;
    private Vector3 currentTarget;

    private float moveRange = 5f;
    private float moveSpeed = 2f;
    private float moveTilt = 30f;
    private float moveInterval = 10f;

    public override void Initialize()
    {
        hp = 150f;
        maxHp = 150f;
        attackDamage = 8f;
        attackDelay = 2f;
        survive = true;
        nextAttackTime = 0f;
        nextMoveTime = moveInterval;

        // 출현 연출 시작
        isSpawning = true;
        StartCoroutine(SpawnSlideRoutine());
    }

    IEnumerator SpawnSlideRoutine()
    {
        Vector3 startPos = transform.position; // 화면 밖 (WaveManager가 설정)
        Vector3 endPos = targetPosition;        // 목표 위치

        // EaseOut 슬라이드 (감속하며 진입)
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float t = elapsed / slideDuration;
            float easeT = 1f - (1f - t) * (1f - t); // EaseOut 커브
            transform.position = Vector3.Lerp(startPos, endPos, easeT);

            // 진입 방향으로 기울기 적용
            float dirX = endPos.x - startPos.x;
            float tiltAngle = dirX > 0 ? -moveTilt : moveTilt;
            float tiltFade = 1f - t; // 도착할수록 기울기 감소
            transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle * tiltFade);

            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;
        transform.rotation = Quaternion.identity;

        // Waypoint를 목표 위치 기준으로 설정
        waypointA = endPos + Vector3.left * moveRange;
        waypointB = endPos + Vector3.right * moveRange;
        currentTarget = waypointB;

        // 출현 완료
        isSpawning = false;
        nextAttackTime = Time.time + attackDelay;
        nextMoveTime = Time.time + moveInterval;
        Debug.Log($"[EnemyB] 등장 완료: {endPos}");
    }

    protected override void OnUpdate()
    {
        if (!IsAlive || isSpawning) return;

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
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackDelay;
            }
        }
    }

    protected override void OnStunned()
    {
        isMoving = false; // 이동 중단
        transform.rotation = Quaternion.identity; // 기울기 복원
    }

    void StartMove()
    {
        if(IsStunned) return; // 기절 중이면 이동하지 않음
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

        GameObject bullet = bulletPool.Get(MuzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        EnemyBulletBase bulletBase = bullet.GetComponent<EnemyBulletBase>();
        Vector3 direction = (target.transform.position - MuzzlePoint.position).normalized;
        bulletBase.Init(attackDamage, 15f, direction);
    }

    public override void Move() { }
    public override void Jump() { }

    public override void Die()
    {
        StopAllCoroutines();
        base.Die();
    }
}