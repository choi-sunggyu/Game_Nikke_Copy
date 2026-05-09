using UnityEngine;

public class EnemyBulletBase : MonoBehaviour
{
    // 변수
    private ObjectPool ownerPool;   // 이 총알을 관리하는 풀
    private float damage;           // 데미지
    private float speed = 100f;     // 이동 속도
    private Vector3 direction;      // 이동 방향
    private float spawnTime;        // 총알이 생성된 시간
    private bool isInitialized = false;

    void OnEnable()
    {
        isInitialized = false;
        ownerPool = GetComponent<PoolObject>().OwnerPool;
    }

    public void Init(float damage, float speed, Vector3 direction)
    {
        isInitialized = true;
        this.damage = damage;
        this.speed = speed;
        this.direction = direction.normalized; // 방향 벡터를 정규화하여 이동 속도에 영향을 주지 않도록 함
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[EnemyBullet] 충돌 감지: {other.gameObject.name}");
        if (!isInitialized) return;

        if(other.TryGetComponent<CharacterBase>(out CharacterBase character))
        {
            character.TakeDamage(damage);
            ownerPool.Return(gameObject);
        }
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
}
