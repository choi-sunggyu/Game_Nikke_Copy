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
    public float ShieldRatio => shield / maxShield;


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
