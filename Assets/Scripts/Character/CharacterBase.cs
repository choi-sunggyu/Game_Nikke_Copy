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
    [SerializeField] private bool crunch;
    [SerializeField] protected float skillCoolTime;
    [SerializeField] protected float burstCoolTime;
    [SerializeField] protected float reloadTime;
    [SerializeField] private bool survive;
    [SerializeField] protected float chargingBurstGauge;
    [SerializeField] protected float attackDamage;

    public bool IsAlive => survive;
    public float HpRatio => hp / maxHp;
    public float MaxBulletCount => maxBulletCount;
    public int CurrentBulletCount => bulletCount;
    public float ShieldRatio => shield / maxShield;

    public bool isForceReloading { get; private set; }

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
            //디퍼브 고려
            if (debuff)
            {
                damage *= 1.25f;
            }

            if (crunch) //엄폐 중이면 쉴드를 우선 깎음
            {
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
            }
            else
            {
                hp -= damage;
            }
            
            //사망 여부   
            if(hp <= 0)
            {
                survive = false;
            }
        }
    }    

    public void TryFire()
    {
        // 사격 조건 체크
        if (survive)
        {
            if (!isForceReloading && bulletCount > 0) //강제 리로딩 중이 아니고 탄창이 남아 있는 경우에만 사격
            {
                bulletCount--;
                // 사격 로직 (예: 총알 발사, 애니메이션 재생 등)
                Debug.Log("사격");
                if(bulletCount == 0) //탄창이 다 떨어졌으면 강제 리로딩 상태로 전환
                {
                    isForceReloading = true;
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

    public void TryReload()
    {
        // 호출 전 survive 체크가 보장되므로 중복 체크 생략
        // 리로딩 조건 체크
        if(bulletCount == maxBulletCount) return;
        StartCoroutine(ReloadDelay());
        // 이후 리로딩 애니메이션 재생 추가할 예정
    }

    private IEnumerator ReloadDelay()
    {
        // 리로딩 시간 대기
        yield return new WaitForSeconds(reloadTime);
        bulletCount = maxBulletCount;
        isForceReloading = false;
        Debug.Log("리로딩 완료. 사격 가능");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
