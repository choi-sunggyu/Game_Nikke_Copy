using UnityEngine;

public class BulletBase : MonoBehaviour
{
    // 변수
    private ObjectPool ownerPool;  // 이 총알을 관리하는 풀
    private float damage;        // 데미지
    private float speed = 100f;         // 이동 속도
    private Vector3 direction;   // 이동 방향
    private float lifetime = 5f;
    private float spawnTime;

    void OnEnable()
    {
        spawnTime = Time.time;
        ownerPool = GetComponent<PoolObject>().OwnerPool; // 풀 오브젝트에서 풀 참조 가져오기
    }

    public void Init(float damage, float speed, Vector3 direction)
    {
        this.damage = damage;
        this.speed = speed;
        this.direction = direction.normalized; // 방향 벡터를 정규화하여 이동 속도에 영향을 주지 않도록 함
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.TryGetComponent<EnemyBase>(out EnemyBase enemy))
        {
            enemy.TakeDamage(damage);
            ownerPool.Return(gameObject);
            return;
        }

        if(other.TryGetComponent<CharacterBase>(out CharacterBase character))
        {
            character.TakeDamage(damage);
            ownerPool.Return(gameObject);
            return;
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
