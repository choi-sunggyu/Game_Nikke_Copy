using System.Collections.Generic;
using UnityEngine;

public class RandomTargetStrategy : ITargetStrategy
{
    private List<ITargetable> targets;

    public RandomTargetStrategy(List<ITargetable> targets)
    {
        this.targets = targets;
    }

    public ITargetable GetTarget()
    {
        // 살아있는 캐릭터 중 랜덤 선택
        List<ITargetable> aliveCharacters = targets.FindAll(c => c.IsAlive);
        if (aliveCharacters.Count == 0) return null; // 타겟이 없으면 null 반환
        return aliveCharacters[Random.Range(0, aliveCharacters.Count)];
    }
}
