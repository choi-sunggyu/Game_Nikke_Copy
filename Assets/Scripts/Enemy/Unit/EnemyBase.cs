using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    // 변수
    [SerializeField] protected EnemyType enemyType = EnemyType.Normal;
    [SerializeField] protected float hp;
    [SerializeField] protected float maxHp;
    [SerializeField] protected float attackDamage;
    [SerializeField] protected float speed;
    [SerializeField] protected bool survive;
    [SerializeField] protected float attackDelay;
    [SerializeField] protected int currentLayer;
    protected ObjectPool bulletPool;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject deathEffectPrefab;
    private EnemyState currentState { get; set; }
    private SpriteRenderer spriteRenderer;
    protected ITargetStrategy targetStrategy;
    protected List<CharacterBase> characters;
    private Coroutine hitFlashCoroutine;
    private bool isStunned = false;
    private Coroutine stunCoroutine;
    public static bool BattleStarted = false;
    private EnemyHPBar _hpBar;

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
    public EnemyType EnemyType => enemyType;
    public float MaxHp => maxHp;

    // 이벤트
    public event Action OnDied;
    public static event Action<EnemyBase> OnBossDefeated; // 보스 사망 이벤트
    public event Action<float, float> OnHpChanged;        // (currentHp, maxHp)

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
        hp  = Mathf.Max(hp, 0f);

        OnHpChanged?.Invoke(hp, maxHp);

        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlash());

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
        survive      = false;
        currentState = EnemyState.Dead;

        // 보스 사망 시 별도 이벤트
        if (enemyType == EnemyType.Boss)
            OnBossDefeated?.Invoke(this);

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;
        Collider col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                deathEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(effect, 2f);
        }

        Destroy(gameObject, 0.3f);
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
        survive       = true;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // HP바 초기화
        _hpBar = GetComponentInChildren<EnemyHPBar>(true);
        if (_hpBar != null)
            _hpBar.Init(this);

        CharacterManager characterManager = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
        if (characterManager != null)
        {
            characters     = characterManager.Characters;
            targetStrategy = new RandomTargetStrategy(characters);
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