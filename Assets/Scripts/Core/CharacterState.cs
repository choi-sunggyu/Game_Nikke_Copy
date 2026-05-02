using UnityEngine;

// 캐릭터의 상태를 나타내는 열거형
// 캐릭터 상태를 새 파일에 넣는 이유는 어디서든 참조할 수 있게 하기 위해서
// 적 AI에서 캐릭터가 사격상태일 때 공격하는 것이 shield가 아닌 hp를 깎을 수 있어서 효과적
public enum CharacterState
{
    Idle,
    Fire,
    Reload,
    Dead
}
