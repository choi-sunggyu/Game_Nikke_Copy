using UnityEngine;

/// <summary>
/// 무기 종류별 정적 명세 — 적정 사거리, 보너스 배율 매핑.
///
/// 디자인 결정에 가까운 상수 (밸런서가 매일 바꾸지 않음) 라서 코드에 둠.
/// 만약 무기별 보너스를 실시간 조정해야 할 정도면 WeaponDatabase SO 로 이전 가능.
///
/// RL (런처) 는 거리 무관 — 항상 동일 피해. IsRangeIndependent() 로 분기.
/// </summary>
public static class WeaponSpecs
{
    /// <summary>적정 사거리에서 적을 맞췄을 때의 피해 배율.</summary>
    public const float OPTIMAL_RANGE_BONUS = 1.5f;

    // ═══════════════════════════════════════════════════════
    //  SG (샷건) — 산탄 발사
    //   1발 트리거 = PELLET_COUNT 개 탄환 발사, 탄당 damage = attackDamage / PELLET_COUNT
    //   가까운 적이 겹쳐있으면 한 트리거로 여러 적 동시 타격 가능
    // ═══════════════════════════════════════════════════════
    public const int   SG_PELLET_COUNT  = 5;     // 산탄 수
    public const float SG_SPREAD_ANGLE  = 5f;    // 중심 방향 기준 ±도 (cone) — 기존 10 → 5 로 축소

    // ═══════════════════════════════════════════════════════
    //  RL (런처) — 스플래시 데미지
    //   직격 적 주변 SPLASH_RADIUS 내 추가 적에게 SPLASH_DAMAGE_RATIO × 직격 데미지
    //   직격 적은 100% 데미지, 주변 적은 비율 데미지
    // ═══════════════════════════════════════════════════════
    public const float RL_SPLASH_RADIUS       = 3.0f;  // 폭발 반경 (월드 유닛)
    public const float RL_SPLASH_DAMAGE_RATIO = 0.7f;  // 주변 적 데미지 비율 (70%)

    /// <summary>이 무기가 거리 무관(런처 등)인지 여부.</summary>
    public static bool IsRangeIndependent(WeaponType type)
    {
        return type == WeaponType.RL;
    }

    /// <summary>
    /// 무기의 적정 사거리 구역.
    /// 거리 무관 무기(RL)에서는 호출 의미 없음 — IsRangeIndependent 먼저 체크.
    /// </summary>
    public static DistanceZone GetOptimalZone(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.SG:
            case WeaponType.SMG:
                return DistanceZone.Close;

            case WeaponType.AR:
            case WeaponType.MG:
                return DistanceZone.Mid;

            case WeaponType.SR:
                return DistanceZone.Far;

            case WeaponType.RL:
            default:
                return DistanceZone.Mid; // 의미 없음 — IsRangeIndependent 에서 걸러짐
        }
    }

    /// <summary>
    /// 무기와 적의 현재 구역을 비교하여 최종 피해 배율 계산.
    ///   - 런처 → 항상 1배
    ///   - 적정 사거리 일치 → OPTIMAL_RANGE_BONUS (기본 1.5)
    ///   - 그 외 → 1배
    /// </summary>
    public static float GetDamageMultiplier(WeaponType weapon, DistanceZone enemyZone)
    {
        if (IsRangeIndependent(weapon)) return 1f;

        return GetOptimalZone(weapon) == enemyZone
            ? OPTIMAL_RANGE_BONUS
            : 1f;
    }
}
