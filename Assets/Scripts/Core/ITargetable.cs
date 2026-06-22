using UnityEngine;

/// <summary>
/// RandomTargetStaregy 테스트 코드 작성을 위한 인터페이스
/// List<CharacterBase> 의존을 List<ITargetable> 로 일반화. 알고리즘 변경 0%, 타입만 추상화. -> 이해 못함
/// </summary>
/// 
public interface ITargetable
{
    public bool IsAlive { get; }
    Transform transform { get; } // MonoBehaviour 파생체는 자동 구현
}