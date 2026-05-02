using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    // 변수
    [SerializeField] protected float hp;
    [SerializeField] protected float maxHp;
    [SerializeField] protected float attackDamage;
    [SerializeField] protected float speed;
    [SerializeField] protected bool survive;
    [SerializeField] protected float attackDelay;
    [SerializeField] protected int currentLayer;
    private EnemyState currentState { get; set; }
    private SpriteRenderer spriteRenderer;

    //프로퍼티
    public bool IsAlive => survive;
    public EnemyState CurrentState => currentState;

    // abstract 메서드
    public abstract void Initialize();
    public abstract void Attack();
    public abstract void Move();
    public abstract void Jump();

    // 공통 메서드
    public void TakeDamage(float damage)
    {
        if(survive){ //살아 있는 상태인지 확인 (데미지를 주기 전에 파악할건지는 미정)
            hp -= damage;
            if (hp <= 0)
                Die();
        }
    }

    public void Die()
    {
        survive = false;
        currentState = EnemyState.Dead;
        // 사망 처리 (예: 애니메이션 재생, 콜라이더 비활성화 등)
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
        // 나중에 애니메이션 연동 추가
    }
    

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        
    }
}
