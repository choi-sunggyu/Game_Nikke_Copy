using UnityEngine;

public class BulletBase : MonoBehaviour, IPoolable
{
    // 변수
    private ObjectPool ownerPool;   // 이 총알을 관리하는 풀
    private float damage;           // 데미지
    private float speed;     // 이동 속도
    private Vector3 direction;      // 이동 방향
    private float lifetime = 3f;    // 총알 수명 (초)
    private float spawnTime;        // 총알이 생성된 시간
    private float burstChargeAmount;
    private bool isInitialized = false;

    void OnEnable()
    {
        spawnTime = Time.time;
        ownerPool = GetComponent<PoolObject>().OwnerPool;
    }

    public void OnGet()
    {
        isInitialized = false;
    }

    public void OnReturn()
    {
        isInitialized = false;
    }

    public void Init(float damage, float speed, Vector3 direction, float burstCharge)
    {
        isInitialized = true;
        this.damage = damage;
        this.speed = speed;
        this.direction = direction.normalized; // 방향 벡터를 정규화하여 이동 속도에 영향을 주지 않도록 함
        burstChargeAmount = burstCharge;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log($"[BulletBase] 충돌 감지: {other.gameObject.name}");
        if (!isInitialized) return;

        //Debug.Log($"[BulletBase] 충돌: {other.gameObject.name} / Layer: {other.gameObject.layer}");

        if (other.TryGetComponent<EnemyBase>(out EnemyBase enemy))
        {
            enemy.TakeDamage(damage);
            BurstGaugeManager.Instance?.AddGauge(burstChargeAmount);
            ownerPool.Return(gameObject);
            return;
        }

        if (other.TryGetComponent<CharacterBase>(out CharacterBase character))
        {
            character.TakeDamage(damage);
            ownerPool.Return(gameObject);
            return;
        }

        if (other.CompareTag("Background"))
        {
            ownerPool.Return(gameObject);
        }
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    
        // 수명 초과 시 반환
        if(Time.time - spawnTime > lifetime)
        {
            ownerPool.Return(gameObject);
        }
    }
}
