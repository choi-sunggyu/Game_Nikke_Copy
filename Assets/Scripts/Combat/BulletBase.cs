using UnityEngine;

public class BulletBase : MonoBehaviour
{
    // 변수
    [SerializeField] private ObjectPool ownerPool;
    private float damage;        // 데미지
    private float speed = 10f;         // 이동 속도
    private Vector2 direction;   // 이동 방향

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void Init(float damage, float speed, Vector2 direction)
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
        //이동 로직
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
