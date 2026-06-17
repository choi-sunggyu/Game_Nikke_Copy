using System;
using UnityEngine;

public class BulletBase : MonoBehaviour, IPoolable
{
    private ObjectPool ownerPool;
    private CharacterBase owner;
    private float damage;
    private float speed;
    private Vector3 direction;
    private float lifetime = 3f;
    private float spawnTime;
    private float burstChargeAmount;
    private bool isInitialized = false;
    private CharacterManager characterManager;

    //프로퍼티 추가
    public CharacterBase Owner => owner;

    // 인스펙터나 코드로 적/아군/배경 레이어를 구분할 수 있도록 설정
    [SerializeField] private LayerMask collisionMask;

    public static event Action<CharacterBase> OnLastBulletHit;

    void OnEnable()
    {
        spawnTime = Time.time;
        ownerPool = GetComponent<PoolObject>().OwnerPool;
    }

    void Start()
    {
        characterManager = FindFirstObjectByType<CharacterManager>();
    }

    public void OnGet() { isInitialized = false; }
    public void OnReturn() { isInitialized = false; }

    public void Init(CharacterBase owner, float damage, float speed, Vector3 direction, float burstCharge)
    {
        isInitialized = true;
        this.owner = owner;
        this.damage = damage;
        this.speed = speed;
        this.direction = direction.normalized; 
        burstChargeAmount = burstCharge;

        // 예시: 자동으로 Enemy, Player, Background 레이어를 체크하도록 설정 (프로젝트 레이어 이름에 맞게 수정)
        collisionMask = LayerMask.GetMask("Enemy", "Player", "Object");
    }

    void Update()
    {
        if (!isInitialized) return;

        float moveDistance = speed * Time.deltaTime;

        // 이번 프레임에 이동할 거리만큼 미리 3D 레이를 쏘아 충돌을 예측합니다. (속도 800f 대응)
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, moveDistance, collisionMask))
        {
            // 충돌한 지점으로 총알을 일단 이동시키고 처리
            transform.position = hit.point;
            HandleCollision(hit.collider);
            return;
        }

        // 충돌이 없으면 정상 이동
        transform.Translate(direction * moveDistance, Space.World);
    
        // 수명 초과 시 반환
        if(Time.time - spawnTime > lifetime)
        {
            ownerPool.Return(gameObject);
        }
    }

    // 기존 OnTriggerEnter2D 로직을 대체하는 충돌 처리 함수
    private void HandleCollision(Collider other)
    {
        if (other.TryGetComponent<EnemyBase>(out EnemyBase enemy))
        {
            var (finalDamage, isCritical) = owner.CalculateDamage(damage);

            owner?.AddDamageRecord(finalDamage);

            // 직격 데미지 — 적정 사거리 보너스 적용 (owner 가 있으면 무기 타입 전달)
            if (owner != null)
                enemy.TakeDamage(finalDamage, owner.WeaponType);
            else
                enemy.TakeDamage(finalDamage);

            // ── RL(런처): 스플래시 데미지 — 직격 지점 주변 적에게 추가 피해 ──
            if (owner != null && owner.WeaponType == WeaponType.RL)
            {
                ApplyRocketSplash(enemy, finalDamage);
            }

            if (owner == characterManager.CurrentCharacter)
            {
                DamagePopupManager.Instance?.Show(
                    enemy.transform.position,
                    finalDamage,
                    isCritical
                );
            }

            BurstGaugeManager.Instance?.AddGauge(burstChargeAmount);
            CheckViperLastBulletHit();
            ownerPool.Return(gameObject);
            return;
        }

        if (other.TryGetComponent<CharacterBase>(out CharacterBase character))
        {
            character.TakeDamage(damage);
            ownerPool.Return(gameObject);
            return;
        }

        if (other.CompareTag("Background"))
        {
            ownerPool.Return(gameObject);
        }
    }

    private void CheckViperLastBulletHit()
    {
        if(owner == null)
            return;

        if(owner is not Viper)
            return;

        if(owner.CurrentBulletCount != 0)
            return;

        OnLastBulletHit?.Invoke(owner);
    }

    // ═══════════════════════════════════════════════════════
    //  RL 스플래시 데미지
    //   직격 적의 위치를 중심으로 SPLASH_RADIUS 내 적 탐색.
    //   직격 적은 이미 별도 처리되었으므로 제외.
    //   주변 적은 directDamage × SPLASH_DAMAGE_RATIO 받음 (사거리 보너스 없이 단순 피해 — RL 거리 무관 컨셉).
    // ═══════════════════════════════════════════════════════
    private void ApplyRocketSplash(EnemyBase directHit, float directDamage)
    {
        float splashDamage = directDamage * WeaponSpecs.RL_SPLASH_DAMAGE_RATIO;

        // OverlapSphere — 폭발 반경 내 모든 콜라이더 탐색
        Collider[] hits = Physics.OverlapSphere(
            directHit.transform.position,
            WeaponSpecs.RL_SPLASH_RADIUS,
            collisionMask);

        foreach (var col in hits)
        {
            // 직격 적은 제외 (이미 풀데미지 적용됨)
            if (col == null) continue;
            if (!col.TryGetComponent<EnemyBase>(out EnemyBase splashEnemy)) continue;
            if (splashEnemy == directHit) continue;
            if (!splashEnemy.IsAlive) continue;

            splashEnemy.TakeDamage(splashDamage); // 사거리 보너스 무관 단순 피해

            // 스플래시 피해 팝업 (작게 표시)
            if (owner == characterManager.CurrentCharacter)
            {
                DamagePopupManager.Instance?.Show(
                    splashEnemy.transform.position,
                    splashDamage,
                    isCritical: false);
            }
        }
    }
}