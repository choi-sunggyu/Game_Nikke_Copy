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
    private bool wasFiring = false;

    void Start()
    {
        
    }
    
    public static void InvokeSwitchCharacter(int index)
    {
        OnSwitchCharacter?.Invoke(index);
    }

    void Update()
    {
        bool isFiring = Input.GetMouseButton(0);

        if(Input.GetMouseButtonDown(0))
            OnFirePress?.Invoke();

        if(Input.GetMouseButtonUp(0))
            OnFireRelease?.Invoke();

        if(isFiring)
            OnFire?.Invoke();
        
        if(!isFiring && wasFiring)  // 뗀 순간 1번만
            OnIdle?.Invoke();

        wasFiring = isFiring;

        if(Input.GetKeyDown(KeyCode.Alpha1))
            OnSwitchCharacter?.Invoke(0);
        if(Input.GetKeyDown(KeyCode.Alpha2))
            OnSwitchCharacter?.Invoke(1);
        if(Input.GetKeyDown(KeyCode.Alpha3))
            OnSwitchCharacter?.Invoke(2);
    }
}