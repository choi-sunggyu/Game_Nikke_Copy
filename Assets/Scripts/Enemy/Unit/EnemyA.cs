using System.Collections;
using UnityEngine;

public class EnemyA : EnemyBase
{

    [Header("Spawn Animation")]
    [Tooltip("Gravity 자연 낙하 대기 시간 (초). 이 시간 후 안착으로 간주.")]
    [SerializeField] private float fallDuration = 1.5f;
    [Tooltip("옆 등장 시 안착 후 X 이동에 걸리는 시간 (초)")]
    [SerializeField] private float sideMoveDuration = 0.5f;

    [Header("Laser Attack")]
    [SerializeField] private float warningDuration = 1.5f; // 경고 지속 시간
    [SerializeField] private float laserDuration = 0.5f;   // 레이저 지속 시간
    [SerializeField] private float warningRadius = 1.0f;   // Circle 최대 반지름
    [SerializeField] private int circleSegments = 32;      // Circle 부드러움
    [SerializeField] private LayerMask characterLayer;     // 레이저 충돌 레이어

    private float nextAttackTime;
    private LineRenderer warningCircle;
    private LineRenderer laserLine;
    private bool isAttacking = false;

    public override void Initialize()
    {
        // 능력치(hp/maxHp/attackDamage/attackDelay)는 EnemyBase.InitBase 의 ApplyEnemyData 가 SO 에서 주입함.
        // 여기서는 적 고유 셋업만 처리.
        nextAttackTime = 0f;

        SetupLineRenderers();

        isSpawning = true;
        StartCoroutine(SpawnFallRoutine());
    }

    private void SetupLineRenderers()
    {
        // 경고 Circle → 자식 오브젝트에 추가
        GameObject warningObj = new GameObject("WarningCircle");
        warningObj.transform.SetParent(transform);
        warningObj.transform.localPosition = Vector3.zero;
        warningCircle = warningObj.AddComponent<LineRenderer>();
        warningCircle.loop = true;
        warningCircle.positionCount = circleSegments + 1;
        warningCircle.startWidth = 0.05f;
        warningCircle.endWidth = 0.05f;
        warningCircle.startColor = Color.red;
        warningCircle.endColor = Color.red;
        warningCircle.enabled = false;
        warningCircle.useWorldSpace = true;

        // 레이저 빔 → 별도 자식 오브젝트에 추가
        GameObject laserObj = new GameObject("LaserLine");
        laserObj.transform.SetParent(transform);
        laserObj.transform.localPosition = Vector3.zero;
        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.1f;
        laserLine.endWidth = 0.05f;
        laserLine.startColor = Color.red;
        laserLine.endColor = new Color(1f, 0.5f, 0f);
        laserLine.enabled = false;
        laserLine.useWorldSpace = true;
    }

    IEnumerator SpawnFallRoutine()
    {
        Vector3 spawnStart  = transform.position;
        bool    isFromSide  = Mathf.Abs(spawnStart.x - targetPosition.x) > 1f;

        // ─ 1단계: Gravity 자연 낙하 (X/Z 잠금, Y 만 떨어짐) ─
        // Rigidbody 안전망 — 미할당 / useGravity off 모두 강제 보정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass       = 1f;
            Debug.LogWarning("[EnemyA] Rigidbody 미할당 — 동적 추가. 프리팹에 명시 추가 권장.");
        }
        else if (!rb.useGravity)
        {
            rb.useGravity = true;
        }

        rb.isKinematic     = false;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // X 도 함께 잠금 — "먼저 떨어진 후" 옆 이동 순서 보장
        rb.constraints = RigidbodyConstraints.FreezePositionX
                       | RigidbodyConstraints.FreezePositionZ
                       | RigidbodyConstraints.FreezeRotation;

        // 자연 낙하 시간 — BattleGround Collider 가 받아 안착
        yield return new WaitForSeconds(fallDuration);

        // ─ 2단계: 옆 등장이면 안착 위치에서 X 만 안쪽으로 이동 ─
        if (isFromSide)
        {
            // X 잠금 해제하기 전에 Lerp 로 직접 이동 (수동 제어)
            BeginManualMovement();
            Vector3 currentPos = transform.position;
            Vector3 sideTarget = new Vector3(targetPosition.x, currentPos.y, currentPos.z);

            float elapsed = 0f;
            while (elapsed < sideMoveDuration)
            {
                float t = elapsed / sideMoveDuration;
                transform.position = Vector3.Lerp(currentPos, sideTarget, Mathf.SmoothStep(0f, 1f, t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            CompleteManualMovement(sideTarget);
        }
        else if (rb != null)
        {
            // 위 등장: X freeze 해제 — 이후 물리/AI 자유 이동
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        isSpawning = false;
        nextAttackTime = Time.time + attackDelay;
    }

    protected override void OnUpdate()
    {
        if (!IsAlive || isSpawning || isAttacking) return;

        if (Time.time >= nextAttackTime)
        {
            StartCoroutine(LaserAttackRoutine());
        }
    }

    protected override void OnStunned()
    {
        StopCoroutine(nameof(LaserAttackRoutine));
        isAttacking = false;
        warningCircle.enabled = false;
        laserLine.enabled = false;
    }

    public override void Attack()
    {
        // 레이저로 대체되므로 LaserAttackRoutine에서 처리
    }

    private IEnumerator LaserAttackRoutine()
    {
        isAttacking = true;

        CharacterBase target = GetTarget();
        if (target == null || !target.IsAlive)
        {
            isAttacking = false;
            nextAttackTime = Time.time + attackDelay;
            yield break;
        }

        // ── BottomUI 경고 발화 — 캐릭터가 엄폐 가능한 시간 = warningDuration ──
        RaiseHighDamageTargeting(target, warningDuration);

        // ── 1단계: 경고 Circle (점점 좁아짐) ──
        warningCircle.enabled = true;
        float elapsed = 0f;

        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / warningDuration;

            // 반지름이 warningRadius → 0으로 줄어듦
            float currentRadius = Mathf.Lerp(warningRadius, 0f, t);
            DrawCircle(warningCircle, MuzzlePoint.position, currentRadius);

            // 색상 흰색 → 빨강으로 변화
            Color c = Color.Lerp(Color.white, Color.red, t);
            warningCircle.startColor = c;
            warningCircle.endColor = c;

            yield return null;
        }

        warningCircle.enabled = false;

        // ── 2단계: 레이저 발사 ──
        target = GetTarget(); // 타겟 재확인
        if (target == null || !target.IsAlive)
        {
            isAttacking = false;
            nextAttackTime = Time.time + attackDelay;
            yield break;
        }

        laserLine.enabled = true;
        Vector3 laserEnd = target.transform.position;

        elapsed = 0f;
        while (elapsed < laserDuration)
        {
            elapsed += Time.deltaTime;

            laserLine.SetPosition(0, MuzzlePoint.position);
            laserLine.SetPosition(1, laserEnd);

            yield return null;
        }

        // 데미지 적용
        target = GetTarget();
        if (target != null && target.IsAlive)
            target.TakeDamage(attackDamage);

        laserLine.enabled = false;

        // ── 종료 ──
        isAttacking = false;
        nextAttackTime = Time.time + attackDelay;
    }

    private void DrawCircle(LineRenderer lr, Vector3 center, float radius)
    {
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            // Z축 고정 (2D 평면에서 표시)
            lr.SetPosition(i, new Vector3(center.x + x, center.y + y, center.z));
        }
    }

    public override void Move() { }
    public override void Jump() { }

    public override void Die()
    {
        warningCircle.enabled = false;
        laserLine.enabled = false;
        StopAllCoroutines();
        base.Die();
    }
}
