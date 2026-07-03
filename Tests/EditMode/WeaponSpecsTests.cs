using NUnit.Framework;

/// <summary>
/// WeaponSpecs 회귀 방지 — 사거리 보너스 / 적정 구역 매핑 / 거리 무관 무기 처리.
///
/// 비즈니스 규칙 (밸런스의 핵심):
///   • RL 은 거리와 무관, 항상 1배
///   • 무기와 적의 구역이 일치하면 OPTIMAL_RANGE_BONUS(1.5) 배 피해
///   • 그 외엔 1배
///
/// 누가 WeaponSpecs 의 매핑이나 상수를 잘못 바꿔도 이 테스트가 즉시 잡아낸다.
/// EditMode 테스트라 Play 안 누르고 0.1초 안에 실행됨.
/// </summary>
public class WeaponSpecsTests
{
    // ═══════════════════════════════════════════════════════
    //  IsRangeIndependent — RL 만 true
    // ═══════════════════════════════════════════════════════
    [Test]
    public void IsRangeIndependent_RL_ReturnsTrue()
    {
        Assert.IsTrue(WeaponSpecs.IsRangeIndependent(WeaponType.RL));
    }

    [TestCase(WeaponType.SG)]
    [TestCase(WeaponType.SMG)]
    [TestCase(WeaponType.AR)]
    [TestCase(WeaponType.MG)]
    [TestCase(WeaponType.SR)]
    public void IsRangeIndependent_NonRL_ReturnsFalse(WeaponType weapon)
    {
        Assert.IsFalse(WeaponSpecs.IsRangeIndependent(weapon));
    }

    // ═══════════════════════════════════════════════════════
    //  GetOptimalZone — 무기별 적정 구역 매핑
    //   매개변수화 테스트 — 한 메서드에서 모든 무기 검증.
    // ═══════════════════════════════════════════════════════
    [TestCase(WeaponType.SG,  DistanceZone.Close)]
    [TestCase(WeaponType.SMG, DistanceZone.Close)]
    [TestCase(WeaponType.AR,  DistanceZone.Mid)]
    [TestCase(WeaponType.MG,  DistanceZone.Mid)]
    [TestCase(WeaponType.SR,  DistanceZone.Far)]
    public void GetOptimalZone_각_무기_매핑이_정확(WeaponType weapon, DistanceZone expected)
    {
        Assert.AreEqual(expected, WeaponSpecs.GetOptimalZone(weapon));
    }

    // ═══════════════════════════════════════════════════════
    //  GetDamageMultiplier — 적정 사거리 일치 → 보너스
    // ═══════════════════════════════════════════════════════
    [TestCase(WeaponType.SG,  DistanceZone.Close)]
    [TestCase(WeaponType.SMG, DistanceZone.Close)]
    [TestCase(WeaponType.AR,  DistanceZone.Mid)]
    [TestCase(WeaponType.MG,  DistanceZone.Mid)]
    [TestCase(WeaponType.SR,  DistanceZone.Far)]
    public void GetDamageMultiplier_적정사거리_일치시_보너스배율(WeaponType weapon, DistanceZone zone)
    {
        float multiplier = WeaponSpecs.GetDamageMultiplier(weapon, zone);
        Assert.AreEqual(WeaponSpecs.OPTIMAL_RANGE_BONUS, multiplier, 0.0001f);
    }

    // ═══════════════════════════════════════════════════════
    //  GetDamageMultiplier — 사거리 불일치 → 1배
    //   SG 는 Close 가 적정이라 Mid / Far 는 1배여야 함.
    // ═══════════════════════════════════════════════════════
    [TestCase(WeaponType.SG,  DistanceZone.Mid)]
    [TestCase(WeaponType.SG,  DistanceZone.Far)]
    [TestCase(WeaponType.SMG, DistanceZone.Mid)]
    [TestCase(WeaponType.SMG, DistanceZone.Far)]
    [TestCase(WeaponType.AR,  DistanceZone.Close)]
    [TestCase(WeaponType.AR,  DistanceZone.Far)]
    [TestCase(WeaponType.MG,  DistanceZone.Close)]
    [TestCase(WeaponType.MG,  DistanceZone.Far)]
    [TestCase(WeaponType.SR,  DistanceZone.Close)]
    [TestCase(WeaponType.SR,  DistanceZone.Mid)]
    public void GetDamageMultiplier_사거리_불일치시_1배(WeaponType weapon, DistanceZone zone)
    {
        float multiplier = WeaponSpecs.GetDamageMultiplier(weapon, zone);
        Assert.AreEqual(1f, multiplier, 0.0001f);
    }

    // ═══════════════════════════════════════════════════════
    //  GetDamageMultiplier — RL 은 어느 구역이든 항상 1배
    //   런처의 거리 무관 정책 — 매개변수화로 3구역 한 번에.
    // ═══════════════════════════════════════════════════════
    [TestCase(DistanceZone.Close)]
    [TestCase(DistanceZone.Mid)]
    [TestCase(DistanceZone.Far)]
    public void GetDamageMultiplier_RL은_모든_구역에서_1배(DistanceZone zone)
    {
        float multiplier = WeaponSpecs.GetDamageMultiplier(WeaponType.RL, zone);
        Assert.AreEqual(1f, multiplier, 0.0001f);
    }

    // ═══════════════════════════════════════════════════════
    //  상수 회귀 방지 — 밸런스 핵심 값들
    //   누가 무심코 상수를 바꿔도 테스트가 잡아낸다.
    //   값이 의도적으로 바뀌면 이 테스트도 같이 갱신할 것.
    // ═══════════════════════════════════════════════════════
    [Test]
    public void Constants_OPTIMAL_RANGE_BONUS_값_고정()
    {
        Assert.AreEqual(1.5f, WeaponSpecs.OPTIMAL_RANGE_BONUS, 0.0001f);
    }

    [Test]
    public void Constants_SG_PELLET_COUNT_값_고정()
    {
        Assert.AreEqual(5, WeaponSpecs.SG_PELLET_COUNT);
    }

    [Test]
    public void Constants_SG_SPREAD_ANGLE_값_고정()
    {
        // 기존 10도에서 5도로 줄였음 — 다시 늘어나면 의도 변경. 그 변경을 가시화.
        Assert.AreEqual(5f, WeaponSpecs.SG_SPREAD_ANGLE, 0.0001f);
    }

    [Test]
    public void Constants_RL_SPLASH_RADIUS_값_고정()
    {
        Assert.AreEqual(3.0f, WeaponSpecs.RL_SPLASH_RADIUS, 0.0001f);
    }

    [Test]
    public void Constants_RL_SPLASH_DAMAGE_RATIO_값_고정()
    {
        Assert.AreEqual(0.7f, WeaponSpecs.RL_SPLASH_DAMAGE_RATIO, 0.0001f);
    }
}
