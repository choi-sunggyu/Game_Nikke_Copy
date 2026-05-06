using System.Collections.Generic;
using UnityEngine;

public class RandomTargetStrategy : ITargetStrategy
{
    private List<CharacterBase> characters;

    public RandomTargetStrategy(List<CharacterBase> characters)
    {
        this.characters = characters;
    }

    public CharacterBase GetTarget()
    {
        // 살아있는 캐릭터 중 랜덤 선택
        List<CharacterBase> aliveCharacters = characters.FindAll(c => c.IsAlive);
        return aliveCharacters[Random.Range(0, aliveCharacters.Count)];
    }
}
