using NUnit.Framework;

public class SpawnQueueGeneratorTests
{
    // ═══════════════════════════════════════════════════════
    //  GetPatternCount — 패턴별 적 수 검증
    //  Single/Trio 는 결정적 (1, 3)
    //  Lateral/DualSide/Top 은 범위 검증 (Random 의존)
    // ═══════════════════════════════════════════════════════

    [Test]
    public void GetPatternCount_Single은_1()
    {
        Assert.AreEqual(1, SpawnQueueGenerator.GetPatternCount(SpawnPattern.Single));
    }

    [Test]
    public void GetPatternCount_Trio는_3()
    {
        Assert.AreEqual(3, SpawnQueueGenerator.GetPatternCount(SpawnPattern.Trio));
    }

    [Test]
    public void GetPatternCount_LateralLeft는_2_또는_3_사이_100회()
    {
        for(int i = 0; i < 100; i++)
        {
            int c = SpawnQueueGenerator.GetPatternCount(SpawnPattern.LateralLeft);
            Assert.IsTrue(c >= 2 && c <= 3);
        }
    }

    [Test]
    public void GetPatternCount_LateralRight도_2_또는_3_사이_100회()
    {
        for(int i = 0; i < 100; i++)
        {
            int c = SpawnQueueGenerator.GetPatternCount(SpawnPattern.LateralRight);
            Assert.IsTrue(c >= 2 && c <= 3);
        }
    }

    [Test]
    public void GetPatternCount_DualSide는_4_부터_6_사이_100회()
    {
        for(int i = 0; i < 100; i++)
        {
            int c = SpawnQueueGenerator.GetPatternCount(SpawnPattern.DualSide);
            Assert.IsTrue(c >= 4 && c <= 6);
        }
    }

    [Test]
    public void GetPatternCount_TopRandom은_2_부터_4_사이_100회()
    {
        for(int i = 0; i < 100; i++)
        {
            int c = SpawnQueueGenerator.GetPatternCount(SpawnPattern.TopRandom);
            Assert.IsTrue(c >= 2 && c <= 4);
        }
    }

    //////////////////////////

    [Test]
    public void BuildPatternPool_poolSize_20_각_패턴_카운트_정확()
    {
        var pool = SpawnQueueGenerator.BuildPatternPool(20);

        int singleCount  = pool.FindAll(p => p == SpawnPattern.Single).Count;
        int trioCount    = pool.FindAll(p => p == SpawnPattern.Trio).Count;
        int LateralLeft  = pool.FindAll(p => p == SpawnPattern.LateralLeft).Count;
        int LateralRight = pool.FindAll(p => p == SpawnPattern.LateralRight).Count;
        int DualSide     = pool.FindAll(p => p == SpawnPattern.DualSide).Count;
        int TopRandom    = pool.FindAll(p => p == SpawnPattern.TopRandom).Count;    

        Assert.AreEqual(6, singleCount);
        Assert.AreEqual(4, trioCount);
        Assert.AreEqual(3, LateralLeft);
        Assert.AreEqual(3, LateralRight);
        Assert.AreEqual(2, DualSide);
        Assert.AreEqual(2, TopRandom);
    }

    [Test]
    public void BuildPatternPool_합계가_poolSize_와_일치()
    {
        var pool = SpawnQueueGenerator.BuildPatternPool(20);
        Assert.AreEqual(20, pool.Count);
    }

    [TestCase(10)]
    [TestCase(13)]    // 반올림 누적 케이스
    [TestCase(50)]
    [TestCase(100)]
    public void BuildPatternPool_다양한_크기_합계_정확(int poolSize)
    {
        var pool = SpawnQueueGenerator.BuildPatternPool(poolSize);
        Assert.AreEqual(poolSize, pool.Count);
    }

    [Test]
    public void BuildPatternPool_셔플_같은_패턴_4연속_안됨()
    {
        var pool = SpawnQueueGenerator.BuildPatternPool(20);
        int run = 1;
        for (int i = 1; i < pool.Count; i++)
        {
            if (pool[i] == pool[i-1]) run++;
            else run = 1;
            Assert.Less(run, 4, $"인덱스 {i} 에서 같은 패턴 4연속");
        }
    }

    [Test]
    public void BuildPatternPool_poolSize_0_빈_리스트()
    {
        var pool = SpawnQueueGenerator.BuildPatternPool(0);
        Assert.AreEqual(0, pool.Count);
    }

    [Test]
    public void BuildPatternPool_poolSize_1_가장_큰_가중치_패턴_1개()
    {
        var pool = SpawnQueueGenerator.BuildPatternPool(1);
        Assert.AreEqual(1, pool.Count);
        Assert.AreEqual(SpawnPattern.Single, pool[0]);  // ← Single 이 가중치 최대(25)
    }

    // ═══════════════════════════════════════════════════════
    //  BuildPrefabKindPool — 일반/엘리트 결정적 분배 검증
    //  eliteRatio 가 round 결과로 정수 분배. 매번 추첨 아님.
    // ═══════════════════════════════════════════════════════

    [Test]
    public void BuildPrefabKindPool_eliteRatio_0_1_60개에_엘리트_6개()
    {
        var pool = SpawnQueueGenerator.BuildPrefabKindPool(60, 0.1f);
        int eliteCount = pool.FindAll(k => k == PrefabKind.Elite).Count;
        Assert.AreEqual(6, eliteCount);     // 60 × 0.1 = 6.0 → 6
    }

    [Test]
    public void BuildPrefabKindPool_eliteRatio_0이면_엘리트_0개()
    {
        var pool = SpawnQueueGenerator.BuildPrefabKindPool(60, 0f);
        int eliteCount = pool.FindAll(k => k == PrefabKind.Elite).Count;
        Assert.AreEqual(0, eliteCount);
    }

    [Test]
    public void BuildPrefabKindPool_eliteRatio_1이면_전부_엘리트()
    {
        var pool = SpawnQueueGenerator.BuildPrefabKindPool(60, 1f);
        int eliteCount = pool.FindAll(k => k == PrefabKind.Elite).Count;
        Assert.AreEqual(60, eliteCount);
    }

    [TestCase(-0.5f)]
    [TestCase(-1f)]
    [TestCase(2f)]
    [TestCase(99f)]
    public void BuildPrefabKindPool_eliteRatio_범위밖_Clamp01_안전망(float ratio)
    {
        var pool = SpawnQueueGenerator.BuildPrefabKindPool(60, ratio);
        int eliteCount = pool.FindAll(k => k == PrefabKind.Elite).Count;

        // 음수 → 0 으로 clamp → 엘리트 0개
        // 1 초과 → 1 로 clamp → 전부 엘리트
        // 어느 쪽이든 합계는 poolSize 유지
        Assert.AreEqual(60, pool.Count);
        Assert.IsTrue(eliteCount == 0 || eliteCount == 60,
            $"Clamp01 후 결과는 0 또는 60 이어야 함. 실제: {eliteCount}");
    }

    [Test]
    public void BuildPrefabKindPool_합계가_poolSize_와_일치()
    {
        var pool = SpawnQueueGenerator.BuildPrefabKindPool(60, 0.1f);
        Assert.AreEqual(60, pool.Count);
    }
}