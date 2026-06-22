using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RandomTargetStrategyTests
{
    public class FakeTarget : ITargetable
    {
        public bool IsAlive { get; set; }
        public Transform transform => null;  // ← 테스트에선 안 씀
    }

    [Test]
    public void 모두_살아있을때_null_아님() 
    {
        // Arrange — 살아있는 FakeTarget 3개 리스트 만들기
        var targets = new List<ITargetable>
        {
            new FakeTarget { IsAlive = true },
            new FakeTarget { IsAlive = true },
            new FakeTarget { IsAlive = true },
        };
        var strategy = new RandomTargetStrategy(targets);

        // Act
        var result = strategy.GetTarget();

        // Assert
        Assert.IsNotNull(result); //어떤 경우에도 null 값이 나오지 않음
        Assert.IsTrue(result.IsAlive); //잡은 unit의 생사여부는 항상 true
    }

    [Test]
    public void 모두_죽었을때_null() { 
        // Arrange — 죽어있는 FakeTarget 3개 리스트 만들기
        var targets = new List<ITargetable>
        {
            new FakeTarget { IsAlive = false },
            new FakeTarget { IsAlive = false },
            new FakeTarget { IsAlive = false },
        };
        var strategy = new RandomTargetStrategy(targets);

        // Act
        var result = strategy.GetTarget();

        // Assert
        Assert.IsNull(result); //모든 타겟이 죽어 null만을 반환한다
    }

    [Test]
    public void 빈_리스트면_null() { 
        // Arrange — 비었는 targets 전달
        var targets = new List<ITargetable>
        {
        };
        var strategy = new RandomTargetStrategy(targets);

        // Act
        var result = strategy.GetTarget();

        // Assert
        Assert.IsNull(result); //타겟이 없어서 null만을 반환한다
    }

    [Test]
    public void 한명만_살아있으면_그_한명만_반환() { 
        // Arrange — 3명 중 1명 생존
        var aliveOne = new FakeTarget { IsAlive = true };

        var targets = new List<ITargetable>
        {
            aliveOne,
            new FakeTarget { IsAlive = false },
            new FakeTarget { IsAlive = false },
        };
        var strategy = new RandomTargetStrategy(targets);

        // Act
        var result = strategy.GetTarget();

        // Assert
        Assert.AreSame(aliveOne, result);
    }

    [Test]
    public void 일부만_살아있을때_죽은_캐릭터는_절대_안나옴_100회() { 
        // Arrange — 5명 중 2명 생존
        var targets = new List<ITargetable>
        {
            new FakeTarget { IsAlive = true },
            new FakeTarget { IsAlive = false },
            new FakeTarget { IsAlive = true },
            new FakeTarget { IsAlive = false },
            new FakeTarget { IsAlive = true },
        };
        var strategy = new RandomTargetStrategy(targets);

        // Act
        // Assert
        for (int i = 0; i < 100; i++)
        {
            var result = strategy.GetTarget();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAlive, $"{i}번째 반복에서 죽은 타겟이 반환됨");
        }
    }
}

