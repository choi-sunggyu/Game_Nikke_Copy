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
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] protected float bulletSpeed;
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
    public CrossHairBase CrossHair => crossHair;
    protected void SetNextFireTime(float time) => nextFireTime = time;
    public bool IsActiveCharacter { get; set; }


    // sender: 이벤트를 발생시킨 캐릭터, count: 탄 수
    public static event Action<CharacterBase, int> OnBulletCountChanged;
    public static event Action<CharacterBase> OnStatChanged;
    public static event Action<CharacterBase, float> OnReloadProgress; // UI용 이벤트 (0~1)
    public static event Action<CharacterBase> OnCharacterDied;

    private Coroutine reloadCoroutine;

    public abstract void Initialize();
    public abstract void UseSkill();
    public abstract void UseBurst();
    public static event Action OnForcedReloadStart;
    public static event Action OnForcedReloadEnd;

    public static void InvokeBulletCountChanged(CharacterBase sender, int count)
    {
        OnBulletCountChanged?.Invoke(sender, count);
    }

    public void TakeDamage(float damage)
    {
        if (!survive) return;

        if (buff) damage *= 0.75f;
        if (debuff) damage *= 1.25f;

        // 1. idle일 때는 쉴드가 먼저 깎이고, 쉴드가 깨지면 남은 데미지가 hp에 적용
        // 2. fire일 때는 바로 hp에 데미지 적용
        // 3. 리로딩 중일 때는 fire와 동일하게 shield에 데미지 적용 (리로딩이 엄폐 상태라고 가정)
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
                shield -= damage;
                if (shield < 0)
                {
                    hp += shield;
                    shield = 0;
                }
                break;
        }
        
        //사망 여부   
        if(hp <= 0)
        {
            survive = false;
            OnCharacterDied?.Invoke(this);
        }
        OnStatChanged?.Invoke(this);
    }    

    public virtual void TryFire()
    {
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

                OnBulletCountChanged?.Invoke(this, bulletCount);
                FireBullet();

                // 사격 로직 (예: 총알 발사, 애니메이션 재생 등)
                //Debug.Log("사격");
                if(bulletCount == 0) //탄창이 다 떨어졌으면 강제 리로딩 상태로 전환
                {
                    //Debug.Log("탄창이 다 떨어졌습니다. 강제 리로딩 상태로 전환합니다.");
                    TryReload();
                }
            }
            else // 강제 리로딩 중이거나 탄창이 없는 경우 사격 불가
            {
                //Debug.Log("사격 불가");
                // bulletCount가 0인 경우는 여기서 TryReload를 하지 않고 다른 곳에서 처리 중일 것임
            }
        }
    }

    protected virtual void FireBullet()
    {
        if (bulletPool == null || muzzlePoint == null) return;

        // Step 1: 카메라 → 크로스헤어 방향으로 worldTarget 결정
        Ray camRay = Camera.main.ScreenPointToRay(crossHair.CrossHairPosition);
        Vector3 worldTarget;

        if (Physics.Raycast(camRay, out RaycastHit camHit, 1000f, enemyLayer))
        {
            worldTarget = camHit.point; // 적에 맞으면 그 지점
        }
        else
        {
            worldTarget = camRay.GetPoint(1000f); // 허공이면 최대 사거리 지점
        }

        // Step 2: muzzlePoint → worldTarget 방향으로 총알 발사
        Vector3 fireDir = (worldTarget - muzzlePoint.position).normalized;

        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        BulletBase bulletBase = bullet.GetComponent<BulletBase>();
        bulletBase.Init(attackDamage, bulletSpeed, fireDir);
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

        // 탄창 절반 이하면 리로드 타임 +1초
        float actualReloadTime = ((float)bulletCount / maxBulletCount) <= 0.5f
            ? reloadTime + 1f
            : reloadTime;

        reloadCoroutine = StartCoroutine(ReloadDelay(isForced, actualReloadTime));
        // 이후 리로딩 애니메이션 재생 추가할 예정
    }

    private IEnumerator ReloadDelay(bool isForced = false, float duration = 0f)
    {
        currentState = CharacterState.Reload;
        spriteRenderer.sprite = reloadSprite;

        if(isForced) OnForcedReloadStart?.Invoke();
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            OnReloadProgress?.Invoke(this, elapsed / duration); // ← UI용 이벤트
            yield return null;
        }
        
        bulletCount = maxBulletCount;
        currentState = CharacterState.Idle;
        spriteRenderer.sprite = idleSprite;

        OnBulletCountChanged?.Invoke(this, bulletCount);

        if(isForced) OnForcedReloadEnd?.Invoke();
        
    }

    public void ChangeState(CharacterState newState)
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

    // 캐릭터 AI용 사격 (CrossHair 우회)
    public virtual void TryFireAtTarget(Vector3 worldTarget)
    {
        if (!survive) return;

        // 쿨타임 중 → Idle
        if (Time.time < nextFireTime)
            return;

        // 탄창 없음 → 리로드
        if (bulletCount <= 0)
        {
            TryReload();
            return;
        }

        // 사격
        ChangeState(CharacterState.Fire);
        bulletCount--;
        nextFireTime = Time.time + fireRate;
        OnBulletCountChanged?.Invoke(this, bulletCount);
        FireBulletAtTarget(worldTarget);

        if (bulletCount == 0) TryReload();
    }

    public virtual void OnStopFiring() { }

    protected virtual void FireBulletAtTarget(Vector3 worldTarget)
    {
        if (bulletPool == null || muzzlePoint == null) return;
        
        Vector3 fireDir = (worldTarget - muzzlePoint.position).normalized;
        GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);
        if (bullet == null) return;

        bullet.GetComponent<BulletBase>().Init(attackDamage, bulletSpeed, fireDir);
    }

    public virtual void StopAllSounds() { }
}
