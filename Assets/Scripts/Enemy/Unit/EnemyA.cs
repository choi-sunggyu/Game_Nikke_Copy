using System.Collections;
using UnityEngine;

public class EnemyA : EnemyBase
{

    [Header("Spawn Animation")]
    [SerializeField] private float fallDuration = 0.8f;
    [SerializeField] private float landBounceHeight = 0.5f;
    [SerializeField] private float bounceDuration = 0.2f;

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
        Vector3 startPos = transform.position;
        Vector3 endPos = targetPosition;

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            float t = elapsed / fallDuration;
            float easeT = t * t;
            transform.position = Vector3.Lerp(startPos, endPos, easeT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        Vector3 bounceUp = endPos + Vector3.up * landBounceHeight;
        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            float t = elapsed / bounceDuration;
            float bounceT = Mathf.Sin(t * Mathf.PI);
            transform.position = Vector3.Lerp(endPos, bounceUp, bounceT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

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