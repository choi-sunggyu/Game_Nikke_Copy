using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyD — 미사일을 발사하는 보스.
///
/// 능력치는 EnemyData SO 에서 읽어옴 (hp, dmg, missile* 등).
///
/// 공격 패턴 3종 — 순차 또는 가중치 랜덤으로 실행:
///   1. 일반 사격     : EnemyBase 의 muzzlePoint 에서 EnemyBulletBase 1발
///   2. 미사일 일제 사격: muzzlePoint1~4 에서 3D 미사일 동시 발사 (호밍)
///   3. 점프 워프      : 화면 위로 사라졌다가 랜덤 위치로 낙하
///
/// 옆 이동은 EnemyB 의 waypoint 왕복 로직을 차용 — 일정 간격마다 좌우로 슬라이드.
/// </summary>
public class EnemyD : EnemyBase
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    // EnemyData SO 슬롯은 EnemyBase 에 protected EnemyData data 로 정의됨.
    // 인스펙터에서 EnemyD_Boss.asset 을 base 의 Data 슬롯에 드래그.

    [Header("── 미사일 발사 위치 (3D) ─────────")]
    [SerializeField] private Transform muzzlePoint1;
    [SerializeField] private Transform muzzlePoint2;
    [SerializeField] private Transform muzzlePoint3;
    [SerializeField] private Transform muzzlePoint4;

    [Header("── 미사일 풀 ────────────────────")]
    [Tooltip("씬 직접 배치 시에만 사용. 런타임 스폰은 WaveManager 가 SetMissilePool 로 주입.")]
    [SerializeField] private ObjectPool missilePool;

    /// <summary>
    /// 외부에서 미사일 풀 주입 — WaveManager 가 보스 스폰 직후 호출.
    /// 인스펙터 직접 할당과 주입 둘 다 허용 (인스펙터가 비어있을 때만 덮어쓰지 않고, 항상 덮어씀 → SSoT 는 주입자).
    /// </summary>
    public void SetMissilePool(ObjectPool pool)
    {
        missilePool = pool;
    }

    [Header("── 이동 패턴 ────────────────────")]
    [Tooltip("점프 인터벌 — 이 범위에서 랜덤으로 다음 점프 예약")]
    [SerializeField] private Vector2 jumpIntervalRange = new Vector2(6f, 10f);

    [Tooltip("옆 이동 인터벌 — 이 범위에서 랜덤으로 다음 옆 이동 예약")]
    [SerializeField] private Vector2 lateralIntervalRange = new Vector2(3f, 5f);

    [Tooltip("옆 이동 시 좌/우로 움직이는 거리")]
    [SerializeField] private float lateralRange = 4f;

    // ═══════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════
    private float nextBulletTime;
    private float nextMissileTime;
    private float nextJumpTime;
    private float nextLateralTime;

    private bool  isJumping  = false;
    private bool  isLateral  = false;

    // 옆 이동 waypoint — 점프 후 매번 재계산
    private Vector3 waypointA;
    private Vector3 waypointB;
    private Vector3 currentLateralTarget;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    public override void Initialize()
    {
        // 능력치(hp/maxHp/attackDamage/attackDelay/speed/enemyType)는
        // EnemyBase.InitBase 의 ApplyEnemyData 가 SO 에서 일괄 주입함.
        // 여기서는 보스 고유 셋업만 처리.

        nextBulletTime  = Time.time + attackDelay;
        nextMissileTime = Time.time + (data != null ? data.missileDelay : 5f);

        // 시작 위치를 중심으로 좌우 waypoint 설정
        Vector3 center = transform.position;
        waypointA = center + Vector3.left  * lateralRange;
        waypointB = center + Vector3.right * lateralRange;
        currentLateralTarget = waypointB;

        ScheduleNextJump();
        ScheduleNextLateral();
    }

    // ═══════════════════════════════════════════════════════
    //  업데이트 — 점프/옆이동이 우선, 그 외 시간엔 사격
    // ═══════════════════════════════════════════════════════
    protected override void OnUpdate()
    {
        if (!IsAlive) return;

        // 점프 중에는 다른 행동 금지
        if (isJumping) return;

        // 옆 이동 트리거
        if (Time.time >= nextLateralTime && !isLateral)
            StartCoroutine(LateralRoutine());

        // 점프 트리거 (옆 이동 중이 아닐 때만)
        if (Time.time >= nextJumpTime && !isLateral)
        {
            StartCoroutine(JumpRoutine());
            return; // 점프 직후엔 다른 행동 스킵
        }

        // ── 사격 (옆 이동 중에도 발사 가능 — 보스 압박감) ──
        if (Time.time >= nextBulletTime)
        {
            Attack();
            nextBulletTime = Time.time + attackDelay;
        }

        // ── 미사일 일제 사격 ──
        if (data != null && data.missileDelay > 0f && Time.time >= nextMissileTime)
        {
            Missile();
            nextMissileTime = Time.time + data.missileDelay;
        }
    }

    protected override void OnStunned()
    {
        StopCoroutine(nameof(JumpRoutine));
        StopCoroutine(nameof(LateralRoutine));
        isJumping = false;
        isLateral = false;
    }

    // ═══════════════════════════════════════════════════════
    //  공격 — 일반 총알 (EnemyBase 의 muzzlePoint 사용)
    // ═══════════════════════════════════════════════════════
    public override void Attack()
    {
        CharacterBase target = GetTarget();
        if (target == null || !target.IsAlive) return;
        if (bulletPool == null) return;

        GameObject bullet = bulletPool.Get(MuzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        EnemyBulletBase bulletBase = bullet.GetComponent<EnemyBulletBase>();
        Vector3 direction = (target.transform.position - MuzzlePoint.position).normalized;
        float bulletSpeed = data != null ? data.bulletSpeed : 15f;
        bulletBase.Init(attackDamage, bulletSpeed, direction);
    }

    // ═══════════════════════════════════════════════════════
    //  미사일 일제 사격 — muzzlePoint1~4 동시 발사
    // ═══════════════════════════════════════════════════════
    public void Missile()
    {
        if (missilePool == null)
        {
            Debug.LogWarning("[EnemyD] missilePool 미할당 — 미사일 발사 불가");
            return;
        }

        CharacterBase target = GetTarget();
        if (target == null || !target.IsAlive) return;

        Transform[] muzzles = { muzzlePoint1, muzzlePoint2, muzzlePoint3, muzzlePoint4 };

        // missileCountPerSalvo 만큼만 발사 (최대 4)
        int count = data != null ? Mathf.Min(data.missileCountPerSalvo, muzzles.Length) : muzzles.Length;
        float missileSpeed    = data != null ? data.missileSpeed    : 10f;
        float missileDamage   = data != null ? data.missileDamage   : 15f;
        float missileHoming   = data != null ? data.missileHoming   : 0.5f;

        for (int i = 0; i < count; i++)
        {
            Transform muzzle = muzzles[i];
            if (muzzle == null) continue;

            GameObject missile = missilePool.Get(muzzle.position, muzzle.rotation);
            if (missile == null) continue;

            EnemyMissileBase mb = missile.GetComponent<EnemyMissileBase>();
            if (mb == null) continue;

            mb.Init(missileDamage, missileSpeed, target.transform, missileHoming);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  옆 이동 — EnemyB 의 waypoint 왕복 패턴 차용
    // ═══════════════════════════════════════════════════════
    IEnumerator LateralRoutine()
    {
        isLateral = true;

        // 다음 타겟 결정 (반대편 waypoint 로)
        currentLateralTarget = (currentLateralTarget == waypointA) ? waypointB : waypointA;

        // 일정 속도로 이동
        while (Vector3.Distance(transform.position, currentLateralTarget) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                currentLateralTarget,
                speed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = currentLateralTarget;
        isLateral = false;
        ScheduleNextLateral();
    }

    // ═══════════════════════════════════════════════════════
    //  점프 워프 — 화면 위로 사라졌다가 랜덤 착지
    // ═══════════════════════════════════════════════════════
    IEnumerator JumpRoutine()
    {
        isJumping = true;

        // 1. 현재 Z 기준 화면 밖 Y 계산 후 위로 사라짐
        float currentOffScreenY = GetOffScreenY(transform.position.z);
        Vector3 exitPos = new Vector3(transform.position.x, currentOffScreenY, transform.position.z);
        yield return StartCoroutine(MoveToPosition(transform.position, exitPos, 0.4f));

        // 2. 착지 위치 결정
        Vector3 landPos = GetRandomLandPosition();

        // 3. 착지 Z 기준 화면 밖 Y 계산 후 대기
        float landOffScreenY = GetOffScreenY(landPos.z);
        transform.position = new Vector3(landPos.x, landOffScreenY, landPos.z);
        yield return new WaitForSeconds(0.2f);

        // 4. 착지 위치로 떨어짐
        yield return StartCoroutine(MoveToPosition(transform.position, landPos, 0.4f));

        // 5. 착지 완료 — 옆 이동 waypoint 재계산
        Vector3 center = transform.position;
        waypointA = center + Vector3.left  * lateralRange;
        waypointB = center + Vector3.right * lateralRange;
        currentLateralTarget = waypointB;

        isJumping        = false;
        nextBulletTime   = Time.time + 0.5f;
        nextMissileTime  = Time.time + 1.0f; // 착지 후 잠시 후 미사일
        ScheduleNextJump();
        ScheduleNextLateral();
    }

    // ═══════════════════════════════════════════════════════
    //  위치 / 스케줄 헬퍼
    // ═══════════════════════════════════════════════════════
    float GetOffScreenY(float z)
    {
        Camera cam              = Camera.main;
        float  distanceFromCam  = z - cam.transform.position.z;
        Vector3 topWorld        = cam.ViewportToWorldPoint(
            new Vector3(0.5f, 1f, distanceFromCam)
        );
        return topWorld.y + 3f;
    }

    Vector3 GetRandomLandPosition()
    {
        float z = Random.Range(10f, 50f);
        float t = (z - 10f) / (50f - 10f);

        float maxX = Mathf.Lerp(13f, 30f, t);
        float x    = Random.Range(-maxX, maxX);

        float minY = Mathf.Lerp(-2f, 0f, t);
        float maxY = Mathf.Lerp(-2f, 10f, t);
        float y    = Random.Range(minY, maxY);

        return new Vector3(x, y, z);
    }

    IEnumerator MoveToPosition(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = to;
    }

    void ScheduleNextJump()
    {
        nextJumpTime = Time.time + Random.Range(jumpIntervalRange.x, jumpIntervalRange.y);
    }

    void ScheduleNextLateral()
    {
        nextLateralTime = Time.time + Random.Range(lateralIntervalRange.x, lateralIntervalRange.y);
    }

    // ═══════════════════════════════════════════════════════
    //  추상 메서드 — 사용하지 않음 (Routine 으로 처리)
    // ═══════════════════════════════════════════════════════
    public override void Move() { }
    public override void Jump() { }

    public override void Die()
    {
        StopAllCoroutines();
        base.Die();
    }
}
