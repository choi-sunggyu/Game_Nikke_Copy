using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour
{
    [Header("변경 값")]
    [SerializeField] protected float hp;
    [SerializeField] protected float maxHp;
    [SerializeField] protected int maxBulletCount;
    [SerializeField] protected int bulletCount;
    [SerializeField] protected float shield;
    [SerializeField] protected float maxShield;
    [SerializeField] protected bool buff;
    [SerializeField] protected bool debuff;
    
    [SerializeField] protected float skillCoolTime;
    [SerializeField] protected float burstCoolTime;
    [SerializeField] protected float reloadTime;
    [SerializeField] protected bool survive;
    [SerializeField] protected float chargingBurstGauge;
    [SerializeField] protected float attackDamage;
    [SerializeField] protected Sprite idleSprite;   // 대기 이미지
    [SerializeField] protected Sprite shootSprite;  // 사격 이미지
    [SerializeField] protected Sprite reloadSprite; // 리로딩 이미지
    [SerializeField] protected float fireRate; // 발사 딜레이
    private float nextFireTime;                 // 다음 발사 가능 시간
    private CharacterState currentState { get; set; }
    private SpriteRenderer spriteRenderer;

    public bool IsAlive => survive;
    public float HpRatio => hp / maxHp;
    public float MaxBulletCount => maxBulletCount;
    public int CurrentBulletCount => bulletCount;
    public float ShieldRatio => shield / maxShield;
    public float NextFireTime => nextFireTime;
    public CharacterState CurrentState => currentState;
    private Coroutine reloadCoroutine;

    public abstract void Initialize();
    public abstract void UseSkill();
    public abstract void UseBurst();

    public void TakeDamage(float damage)
    {
        if(survive){ //살아 있는 상태인지 확인 (데미지를 주기 전에 파악할건지는 미정)
            //버프 고려
            if (buff)
            {
                damage *= 0.75f;
            }
            //디버프 고려
            if (debuff)
            {
                damage *= 1.25f;
            }

            // 1. idle일 때는 쉴드가 먼저 깎이고, 쉴드가 깨지면 남은 데미지가 hp에 적용
            // 2. fire일 때는 바로 hp에 데미지 적용
            // 3. 리로딩 중일 때는 fire와 동일하게 hp에 데미지 적용 (리로딩이 엄폐 상태라고 가정)
            switch(currentState)
            {
                case CharacterState.Idle:
                    if(shield > 0) //쉴드가 남아 있음
                    {
                        shield -= damage;
                        //체력 감소
                        if(shield < 0) //쉴드 깨짐 남은 데미지 받음
                        {
                            hp += shield;
                            shield = 0;
                        }
                    }
                    else //쉴드 깨짐
                    {
                        hp -= damage;
                    }
                    break;
                case CharacterState.Fire:
                    hp -= damage;
                    break;
                case CharacterState.Reload:
                    hp -= damage;
                    break;
            }
            
            //사망 여부   
            if(hp <= 0)
            {
                survive = false;
            }
        }
    }    

    public virtual void TryFire()
    {
        Debug.Log($"TryFire 호출 / survive: {survive} / state: {currentState} / bullet: {bulletCount}");
        // 사격 조건 체크
        if (survive)
        {
            if (bulletCount > 0 && Time.time >= nextFireTime) //강제 리로딩 중이 아니고 탄창이 남아 있는 경우에만 사격
            {
                StopReload();
                spriteRenderer.sprite = shootSprite;
                currentState = CharacterState.Fire;

                bulletCount--;
                nextFireTime = Time.time + fireRate;

                // 사격 로직 (예: 총알 발사, 애니메이션 재생 등)
                Debug.Log("사격");
                if(bulletCount == 0) //탄창이 다 떨어졌으면 강제 리로딩 상태로 전환
                {
                    Debug.Log("탄창이 다 떨어졌습니다. 강제 리로딩 상태로 전환합니다.");
                    TryReload();
                }
            }
            else // 강제 리로딩 중이거나 탄창이 없는 경우 사격 불가
            {
                Debug.Log("사격 불가");
                // bulletCount가 0인 경우는 여기서 TryReload를 하지 않고 다른 곳에서 처리 중일 것임
            }
        }
    }

    protected void StopReload()
    {
        if(reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
    }
    public void TryReload()
    {
        // 호출 전 survive 체크가 보장되므로 중복 체크 생략
        // 리로딩 조건 체크
        if(bulletCount == maxBulletCount)
        {
            currentState = CharacterState.Idle;
            spriteRenderer.sprite = idleSprite;
            return;
        }
        if(currentState == CharacterState.Reload) return;
        //StartCoroutine(ReloadDelay());
        reloadCoroutine = StartCoroutine(ReloadDelay());
        // 이후 리로딩 애니메이션 재생 추가할 예정
    }

    private IEnumerator ReloadDelay()
    {
        currentState = CharacterState.Reload;
        spriteRenderer.sprite = reloadSprite;
        // 리로딩 시간 대기
        yield return new WaitForSeconds(reloadTime);
        
        bulletCount = maxBulletCount;
        currentState = CharacterState.Idle;
        spriteRenderer.sprite = idleSprite;
        Debug.Log($"리로딩 완료/ survive: {survive} / state: {currentState} / bullet: {bulletCount}");
    }

    protected void ChangeState(CharacterState newState)
    {
        currentState = newState;
        switch(newState)
        {
            case CharacterState.Idle:
                spriteRenderer.sprite = idleSprite;
                break;
            case CharacterState.Fire:
                spriteRenderer.sprite = shootSprite;
                break;
            case CharacterState.Reload:
                spriteRenderer.sprite = reloadSprite;
                break;
        }
    }

    void Awake()
    {
        //SpriteRenderer 같은 컴포넌트 참조는 Awake에서 처리
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        Initialize();
    }

    void OnEnable()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
