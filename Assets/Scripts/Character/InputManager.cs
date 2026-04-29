using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // 이벤트 선언 (C# event 사용)
    public static event Action OnFire;
    public static event Action OnIdle;
    public static event Action OnFireRelease;
    public static event Action<int> OnSwitchCharacter;  // int: 캐릭터 인덱스

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            OnFireRelease?.Invoke();
        }

        if(Input.GetMouseButton(0))
        {
            OnFire?.Invoke();
        }
        else
        {
            OnIdle?.Invoke();
        }

        if(Input.GetKeyDown(KeyCode.Alpha1))
            OnSwitchCharacter?.Invoke(0);  // Ghost
        if(Input.GetKeyDown(KeyCode.Alpha2))
            OnSwitchCharacter?.Invoke(1);  // Titan
        if(Input.GetKeyDown(KeyCode.Alpha3))
            OnSwitchCharacter?.Invoke(2);  // Viper
    }
}
