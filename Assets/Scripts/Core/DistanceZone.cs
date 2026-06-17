/// <summary>
/// 적의 거리 구역 분류. WaveManager 의 minZ/maxZ 범위를 3등분해서 결정됨.
/// EnemyBase.GetDistanceZone() 에서 자동 계산.
/// 무기의 적정 사거리(WeaponSpecs)와 비교해 보너스 피해를 결정.
/// </summary>
public enum DistanceZone
{
    Close = 0, // 근거리 (스폰 z 범위의 하위 1/3)
    Mid   = 1, // 중거리 (중간 1/3)
    Far   = 2  // 장거리 (상위 1/3)
}
