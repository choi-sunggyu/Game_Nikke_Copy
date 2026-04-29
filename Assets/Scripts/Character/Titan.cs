using UnityEngine;

public class Titan : CharacterBase
{
    private float currentFireRate;   // 현재 발사 딜레이
    private float minFireRate;       // 최소 딜레이 (최고 속도 : 1f/50f)
    private float maxFireRate;       // 최대 딜레이 (시작 속도 : 1f/10f)
    private int shotsFired;          // 발사한 총알 수 (가속 계산용 30발부터 max 속도)
    private float nextTitanFireTime; // Titan 전용 변수
    public override void Initialize()
    {
        // GDD 기준:
        // - 미니건 : 탄창 400발
        // - 강제 리로드 시간: 5.0초
        // - 버스트 차징량: 중간
        // - HP: 200
        // - attackDamage : 10
        maxHp = 200;
        hp = maxHp;
        maxBulletCount = 400;
        bulletCount = maxBulletCount;
        maxShield = 50;
        shield = maxShield;
        reloadTime = 5.0f;
        chargingBurstGauge = 10;
        burstCoolTime = 20.0f;
        skillCoolTime = 10.0f;
        attackDamage = 10;
        survive = true;
        minFireRate = 1f / 60f;  // 최고 속도: 60발/초
        maxFireRate = 1f / 3f;   // 시작 속도: 3발/초
        currentFireRate = maxFireRate;
        shotsFired = 0;
        nextTitanFireTime = 0;
    }

    public void OnEnable()
    {
        InputManager.OnFireRelease += ResetFireRate;
    }



    public override void TryFire()
    {
        if (!survive) return;
        if (bulletCount == 0)
        {
            Debug.Log("탄창이 없습니다. 리로딩 중입니다.");
            return;
        } 

        // 미니건 가속 딜레이 게이트: 아직 발사 시간이 안 됐으면 무시
        if (Time.time < nextTitanFireTime) return;

        StopReload();
        ChangeState(CharacterState.Fire);
        bulletCount--;
        shotsFired++;

        // 0~20발: maxFireRate → minFireRate 로 서서히 빨라짐 / 20발 이후: minFireRate 고정
        currentFireRate = Mathf.Lerp(maxFireRate, minFireRate,
                        Mathf.Clamp01(shotsFired / 20f));

        nextTitanFireTime = Time.time + currentFireRate;
        Debug.Log($"TryFire / bullet: {bulletCount} / shots: {shotsFired} / rate: {1f / currentFireRate:F1}발/초");

        if (bulletCount == 0)
        {
            TryReload();
        }
    }

    public override void UseSkill()
    {
        // Viper의 스킬 사용 로직
    }

    public override void UseBurst()
    {
        // Viper의 버스트 사용 로직
    }

    void ResetFireRate()
    {
        currentFireRate = maxFireRate;
        shotsFired = 0;
        nextTitanFireTime = 0f;
    }

    void OnDisable()
    {
        InputManager.OnFireRelease -= ResetFireRate;
    }
}
