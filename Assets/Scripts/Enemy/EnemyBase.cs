using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] protected ObjectPool bulletPool;
    [SerializeField] private Transform muzzlePoint;
    private EnemyState currentState { get; set; }
    private SpriteRenderer spriteRenderer;
    protected ITargetStrategy targetStrategy;
    protected List<CharacterBase> characters;
    private Coroutine hitFlashCoroutine;
    private bool isStunned = false;
    private Coroutine stunCoroutine;
    public static bool BattleStarted = false;

    // 출현 연출 관련
    protected Vector3 targetPosition;
    protected bool isSpawning = true; // 출현 연출 중 여부

    //프로퍼티
    public float Hp => hp;
    public bool IsAlive => survive;
    public EnemyState CurrentState => currentState;
    public Vector3 TargetPosition => targetPosition;
    public bool IsSpawning => isSpawning;
    public bool IsStunned => isStunned;
    public Transform MuzzlePoint => muzzlePoint;
    public event Action OnDied;

    // abstract 메서드
    public abstract void Initialize();
    public abstract void Attack();
    public abstract void Move();
    public abstract void Jump();

    // 공통 메서드
    public virtual void TakeDamage(float damage)
    {
        if (!survive) return;
        hp -= damage;
        
        // 피격 모션 (빨갛게)
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlash());

        // 데미지 팝업
        DamagePopupManager.Instance?.ShowDamage(damage, transform.position);

        if (hp <= 0) Die();
    }

    public void ApplyStun(float duration)
    {
        if (!survive) return;
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        OnStunned();
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    protected virtual void OnStunned() { }

    private IEnumerator HitFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    public virtual void Die()
    {
        OnDied?.Invoke();
        survive = false;
        currentState = EnemyState.Dead;
        // 사망 처리: 콜라이더 비활성화 후 오브젝트 제거
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;
        Collider col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        Destroy(gameObject, 0.3f); // 약간의 딜레이 후 삭제 (사망 이펙트 여유)
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
        // 나중에 애니메이션 연동 추가
    }
    
    protected CharacterBase GetTarget()
    {
        if (targetStrategy == null) return null;
        return targetStrategy.GetTarget();
    }

    void Start()
    {
        InitBase(); 
        Initialize();
    }

    void Update()
    {
        if (!BattleStarted) return;
        if (!survive) return;
        if (isStunned) return;
        OnUpdate();
    }

    protected virtual void OnUpdate() { }

    public void TryAttack()
    {
        if (!survive) return;
        if (isStunned) return;
        Attack();
    }

    public void SetBulletPool(ObjectPool pool)
    {
        bulletPool = pool;
    }

    public void SetTargetPosition(Vector3 pos)
    {
        targetPosition = pos;
    }

    private void InitBase()
    {
        survive = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // characters 연결 추가
        CharacterManager characterManager = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
        if(characterManager != null)
        {
            characters = characterManager.Characters;
            targetStrategy = new RandomTargetStrategy(characters);
            Debug.Log("[EnemyBase] CharacterManager 연결 성공");
        }
        else
        {
            Debug.LogError("[EnemyBase] CharacterManager를 찾을 수 없습니다.");
        }
    }
}

public interface ITargetStrategy
{
    CharacterBase GetTarget();
}