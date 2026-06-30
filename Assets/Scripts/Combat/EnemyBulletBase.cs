using UnityEngine;

public class EnemyBulletBase : MonoBehaviour, IPoolable
{
    private ObjectPool ownerPool;
    private float damage;
    private float speed = 100000f;
    private Vector3 direction;
    private bool isInitialized = false;

    void OnEnable()
    {
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

    public void Init(float damage, float speed, Vector3 direction)
    {
        isInitialized = true;
        this.damage = damage;
        this.speed = speed;
        this.direction = direction.normalized;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;
        //Debug.Log("Bullet hit: " + other.name);
        CharacterBase character = other.GetComponentInParent<CharacterBase>();
        if (character != null)
        {
            //Debug.Log("Bullet hit character: " + character.name);
            character.TakeDamage(damage);
            Return();
        }
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void Return()
    {
        if (ownerPool != null) ownerPool.Return(gameObject);
        else                   gameObject.SetActive(false);
    }
}
