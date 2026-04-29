using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // 이벤트 선언 (C# event 사용)
    public static event Action OnFire;
    public static event Action OnIdle;
    public static event Action OnFireRelease;

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
    }
}
