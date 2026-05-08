using System;
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
    [SerializeField] protected Transform muzzlePoint;  // 총구 위치
    [SerializeField] protected ObjectPool bulletPool;   // 총알 풀
    
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
    [SerializeField] protected CrossHairBase crossHair;
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

    public static event Action<int> OnBulletCountChanged;

    private Coroutine reloadCoroutine;

    public abstract void Initialize();
    public abstract void UseSkill();
    public abstract void UseBurst();
    public static event Action OnForcedReloadStart;
    public static event Action OnForcedReloadEnd;

    public static void InvokeBulletCountChanged(int count)
    {
        OnBulletCountChanged?.Invoke(count);
    }

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

                OnBulletCountChanged?.Invoke(bulletCount);
                FireBullet();

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

    protected virtual void FireBullet()
    {
        Debug.Log($"[FireBullet] 호출 / damage: {attackDamage}");
        if(bulletPool == null || muzzlePoint == null) return;

        // CrossHair 위치에서 Ray 생성
        Ray ray = Camera.main.ScreenPointToRay(crossHair.CrossHairPosition);

        // 적이 있는 Z평면 (Z=5, Z=10, Z=20 중 현재 타겟 레이어)
        float targetZ = 10f;  // 기본값, 나중에 적 레이어에 따라 변경
        
        // Ray와 Z평면 교차점 계산
        float t = (targetZ - ray.origin.z) / ray.direction.z;
        Vector3 targetPoint = ray.origin + ray.direction * t;

        // 총알 방향 계산
        Vector3 direction3D = (targetPoint - muzzlePoint.position).normalized;

        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        BulletBase bulletBase = bullet.GetComponent<BulletBase>();
        bulletBase.Init(attackDamage, 10f, direction3D);
    }

    protected void StopReload()
    {
        if(reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
    }
    public virtual void TryReload()
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

        bool isForced = (bulletCount == 0);
        reloadCoroutine = StartCoroutine(ReloadDelay(isForced));
        // 이후 리로딩 애니메이션 재생 추가할 예정
    }

    private IEnumerator ReloadDelay(bool isForced = false)
    {
        currentState = CharacterState.Reload;
        spriteRenderer.sprite = reloadSprite;

        if(isForced) OnForcedReloadStart?.Invoke();
        
        // 리로딩 시간 대기
        yield return new WaitForSeconds(reloadTime);
        
        bulletCount = maxBulletCount;
        currentState = CharacterState.Idle;
        spriteRenderer.sprite = idleSprite;

        OnBulletCountChanged?.Invoke(bulletCount);

        if(isForced) OnForcedReloadEnd?.Invoke();
        
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

    protected virtual void OnEnable() {}

    protected virtual void OnDisable() {}

    // Update is called once per frame
    void Update()
    {
        
    }
}
