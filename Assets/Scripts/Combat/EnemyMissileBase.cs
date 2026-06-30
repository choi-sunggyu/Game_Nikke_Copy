using UnityEngine;

/// <summary>
/// 적 보스가 발사하는 3D 미사일.
/// EnemyBulletBase 와 별도로 둔 이유:
///   • 2D Trigger 가 아니라 3D Collider 충돌
///   • 호밍(추적) 기능 (homingStrength)
///   • 가속 / 회전 (transform.forward 정렬)
///   • 폭발 이펙트 (옵션)
///
/// 사용 흐름:
///   1. Init(damage, speed, target, homingStrength, lifeTime) 호출
///   2. Update 매 프레임 — 호밍 보정 후 forward 방향으로 전진
///   3. OnTriggerEnter (3D) 로 CharacterBase 명중 시 데미지 + 풀 반환
///   4. lifeTime 초과 시 자동 풀 반환 (벗어난 미사일 누수 방지)
/// </summary>
public class EnemyMissileBase : MonoBehaviour, IPoolable
{
    private ObjectPool ownerPool;

    // ── 런타임 상태 ──
    private float     damage;
    private float     speed;
    private float     homingStrength; // 0=직진, 1=완전 추적
    private Transform target;
    private Vector3   fallbackDirection; // 타겟 사라졌을 때 사용할 마지막 방향
    private float     lifeTime;
    private float     elapsed;
    private bool      isInitialized = false;

    [Header("── 시각 ────────────────────────")]
    [SerializeField] private GameObject explosionEffect; // 명중/만료 시 인스턴스화
    [Tooltip("회전 보간 속도 — 클수록 즉시 타겟 향함")]
    [SerializeField] private float rotateSpeed = 8f;
    [Tooltip("타겟 거리로 계산한 도달 시간보다 길게 보장되는 기본 수명 (초)")]
    [SerializeField] private float defaultLifeTime = 10f;
    [Tooltip("타겟 도달 예상 시간에 더하는 여유 시간 (초)")]
    [SerializeField] private float lifeTimeBuffer = 2f;

    void OnEnable()
    {
        ownerPool = GetComponent<PoolObject>().OwnerPool;
    }

    public void OnGet()
    {
        isInitialized = false;
        elapsed       = 0f;
    }

    public void OnReturn()
    {
        isInitialized = false;
        target        = null;
    }

    /// <summary>
    /// 미사일 발사 초기화.
    /// </summary>
    /// <param name="damage">명중 시 캐릭터에게 줄 피해</param>
    /// <param name="speed">월드 단위/초</param>
    /// <param name="target">호밍 대상 (null 이면 직진)</param>
    /// <param name="homingStrength">0=직진, 1=즉시 정렬</param>
    /// <param name="lifeTime">자동 회수 시간 (초)</param>
    public void Init(float damage, float speed, Transform target, float homingStrength, float lifeTime = -1f)
    {
        isInitialized       = true;
        this.damage         = damage;
        this.speed          = speed;
        this.target         = target;
        this.homingStrength = Mathf.Clamp01(homingStrength);
        this.lifeTime       = lifeTime > 0f ? lifeTime : defaultLifeTime;
        elapsed             = 0f;

        // 초기 방향: 타겟 있으면 그쪽, 없으면 forward 유지
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(dir);
            fallbackDirection  = dir;

            if (speed > 0f)
            {
                float estimatedArrivalTime = Vector3.Distance(transform.position, target.position) / speed;
                this.lifeTime = Mathf.Max(this.lifeTime, estimatedArrivalTime + lifeTimeBuffer);
            }
        }
        else
        {
            fallbackDirection = transform.forward;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 생존 시간 초과 → 회수
        elapsed += Time.deltaTime;
        if (elapsed >= lifeTime)
        {
            Return();
            return;
        }

        // 호밍 보정: 타겟이 살아있으면 그쪽으로 회전
        if (target != null && homingStrength > 0f)
        {
            Vector3    desiredDir = (target.position - transform.position).normalized;
            fallbackDirection     = desiredDir;
            Quaternion desiredRot = Quaternion.LookRotation(desiredDir);
            transform.rotation    = Quaternion.Slerp(
                transform.rotation,
                desiredRot,
                rotateSpeed * homingStrength * Time.deltaTime
            );
        }

        // 전진
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isInitialized) return;
        CharacterBase character = other.GetComponentInParent<CharacterBase>();
        if (character != null)
        {
            character.TakeDamage(damage);
            Explode();
            Return();
        }
    }

    // 2D 콜라이더가 캐릭터에 붙어 있는 경우를 위한 호환 (선택). 캐릭터가 3D 콜라이더만 쓴다면 삭제 가능.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;
        CharacterBase character = other.GetComponentInParent<CharacterBase>();
        if (character != null)
        {
            character.TakeDamage(damage);
            Explode();
            Return();
        }
    }

    private void Explode()
    {
        if (explosionEffect == null) return;
        GameObject fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(fx, 2f);
    }

    private void Return()
    {
        if (ownerPool != null) ownerPool.Return(gameObject);
        else                   gameObject.SetActive(false);
    }
}
