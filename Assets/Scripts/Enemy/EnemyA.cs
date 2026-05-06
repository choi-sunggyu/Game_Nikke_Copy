using UnityEngine;

public class EnemyA : EnemyBase // 원거리 적
{
    private float nextEnemyAFireTime = 7f; // EnemyA 전용 변수
    public override void Attack()
    {
        if (Time.time < nextEnemyAFireTime) return;
        ChangeState(EnemyState.Attack);
        nextEnemyAFireTime = Time.time + attackDelay;
        // 공격 로직 (예: 플레이어에게 데미지 주기)
        // 가장 가까운 캐릭터 찾기 or 랜덤으로 캐릭터 선택하기 or 엄폐물에서 나온 캐릭터 공격하기
        
    }

    public override void Initialize()
    {
        hp = 100f;
        maxHp = 100f;
        attackDamage = 10f;
        speed = 0f; // 원거리 적이므로 이동하지 않음
        survive = true;
        attackDelay = 5f;
        currentLayer = 20;
    }

    public override void Jump()
    {}

    public override void Move()
    {}

    void Update()
    {
        
    }
}
