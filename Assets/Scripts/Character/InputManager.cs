using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // 이벤트 선언 (C# event 사용)
    public static event Action OnFire;
    public static event Action OnIdle;
    public static event Action OnReload;

    // 필요한 변수
    // 강제 리로딩 중인지
    private bool isReloading = false;
    // 버스트 단계 (0~3)
    private int burstStage = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputManager.OnFire += HandleFire;
        InputManager.OnIdle += HandleIdle;
    }

    void HandleFire()
    {
        // 현재 캐릭터 사격
        Debug.Log("사격");
    }

    void HandleIdle()
    {
        // 탄창 체크 후 리로딩 여부 결정

    }

    // Update is called once per frame
    void Update()
    {
        //    클릭 중 → OnFire 이벤트 발생
        if(Input.GetMouseButton(0) && !isReloading)
        {
            OnFire?.Invoke();
        }
        //    미클릭  → OnIdle 이벤트 발생
        else
        {
            OnIdle?.Invoke();
        }
    }
}
