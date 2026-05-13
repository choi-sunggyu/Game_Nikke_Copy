using System.Collections;
using UnityEngine;

public class EnemyA : EnemyBase
{
    [SerializeField] private Transform muzzlePoint;

    [Header("Spawn Animation")]
    [SerializeField] private float fallDuration = 0.8f; // 낙하 소요 시간
    [SerializeField] private float landBounceHeight = 0.5f; // 착지 바운스 높이
    [SerializeField] private float bounceDuration = 0.2f; // 바운스 시간

    private float nextAttackTime;

    public override void Initialize()
    {
        hp = 100f;
        maxHp = 100f;
        attackDamage = 10f;
        attackDelay = 2f;
        survive = true;
        nextAttackTime = 0f;

        // 출현 연출 시작
        isSpawning = true;
        StartCoroutine(SpawnFallRoutine());
    }

    IEnumerator SpawnFallRoutine()
    {
        Vector3 startPos = transform.position; // 화면 위 (WaveManager가 설정)
        Vector3 endPos = targetPosition;        // 목표 착지 위치

        // 1단계: 가속 낙하 (EaseIn 커브)
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            float t = elapsed / fallDuration;
            float easeT = t * t; // 가속 커브 (중력 느낌)
            transform.position = Vector3.Lerp(startPos, endPos, easeT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        // 2단계: 착지 바운스 (살짝 올라갔다 내려옴)
        Vector3 bounceUp = endPos + Vector3.up * landBounceHeight;
        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            float t = elapsed / bounceDuration;
            // 위로 갔다 다시 내려오는 사인 커브
            float bounceT = Mathf.Sin(t * Mathf.PI);
            transform.position = Vector3.Lerp(endPos, bounceUp, bounceT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        // 출현 완료
        isSpawning = false;
        nextAttackTime = Time.time + attackDelay;
        Debug.Log($"[EnemyA] 착지 완료: {endPos}");
    }

    void Update()
    {
        if (!IsAlive || isSpawning) return;

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
        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        EnemyBulletBase bulletBase = bullet.GetComponent<EnemyBulletBase>();
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