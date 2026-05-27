using System.Collections;
using UnityEngine;

public class EnemyC : EnemyBase
{
    private float attackDelaySelf = 1.5f;
    private float nextAttackTime;
    private float nextJumpTime;
    private bool isJumping = false;

    public override void Initialize()
    {
        hp = 80f;
        maxHp = 80f;
        attackDamage = 12f;
        survive = true;

        ScheduleNextJump();
    }

    protected override void OnUpdate()
    {
        if (!IsAlive) return;
        if (isJumping) return;

        if (Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackDelaySelf;
        }

        if (Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpRoutine());
        }
    }

    protected override void OnStunned()
    {
        StopCoroutine(nameof(JumpRoutine));
        isJumping = false;
    }

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

        // 5. 착지 완료
        isJumping = false;
        nextAttackTime = Time.time + 0.5f;
        ScheduleNextJump();
    }

    float GetOffScreenY(float z)
    {
        // 카메라에서 해당 Z 평면까지의 거리 기준으로 화면 상단 월드 Y 계산
        Camera cam = Camera.main;
        float distanceFromCam = z - cam.transform.position.z;
        
        // 카메라 상단 뷰포트(y=1)를 해당 Z 평면에서의 월드 좌표로 변환
        Vector3 topWorld = cam.ViewportToWorldPoint(
            new Vector3(0.5f, 1f, distanceFromCam)
        );
        
        return topWorld.y + 3f; // 상단 + 여유 3f
    }

    Vector3 GetRandomLandPosition()
    {
        float z = Random.Range(10f, 50f);
        float t = (z - 10f) / (50f - 10f);

        // Z에 따라 X 범위 보간
        float maxX = Mathf.Lerp(13f, 30f, t);
        float x = Random.Range(-maxX, maxX);

        // Z에 따라 Y 범위 보간
        float minY = Mathf.Lerp(-2f, 0f, t);
        float maxY = Mathf.Lerp(-2f, 10f, t);
        float y = Random.Range(minY, maxY);

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
        nextJumpTime = Time.time + Random.Range(3f, 10f);
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