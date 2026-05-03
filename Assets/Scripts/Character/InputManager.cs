using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // 이벤트 선언 (C# event 사용)
    public static event Action OnFire;
    public static event Action OnIdle;
    public static event Action OnFirePress; // Viper 조준 시작 이벤트 전용
    public static event Action OnFireRelease; // Viper 조준 해제 이벤트 전용
    public static event Action<int> OnSwitchCharacter;  // int: 캐릭터 인덱스

    void Start()
    {
        
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0)) // 클릭할 때
        {
            OnFirePress?.Invoke(); // Viper 조준 시작 이벤트 전용
        }

        if(Input.GetMouseButtonUp(0)) // 클릭에서 땔 때
        {
            OnFireRelease?.Invoke(); // Viper 조준 해제 이벤트 전용
        }

        if(Input.GetMouseButton(0)) // 클릭 중일 때
        {
            OnFire?.Invoke(); // 모든 캐릭터 공통 공격 이벤트
        }
        else
        {
            OnIdle?.Invoke(); // 클릭 안 하고 있을 때 (Idle 상태) 이벤트
        }

        if(Input.GetKeyDown(KeyCode.Alpha1))
            OnSwitchCharacter?.Invoke(0);  // Ghost
        if(Input.GetKeyDown(KeyCode.Alpha2))
            OnSwitchCharacter?.Invoke(1);  // Titan
        if(Input.GetKeyDown(KeyCode.Alpha3))
            OnSwitchCharacter?.Invoke(2);  // Viper
    }
}
