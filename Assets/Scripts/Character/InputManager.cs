using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    // 이벤트 선언 (C# event 사용)
    public static event Action OnFire;
    public static event Action OnIdle;
    public static event Action OnFirePress; // Viper 조준 시작 이벤트 전용
    public static event Action OnFireRelease; // Viper 조준 해제 이벤트 전용
    public static event Action<int> OnSwitchCharacter;
    public static event Action OnCoverToggle;
    
    private bool wasFiring = false;
    private List<CharacterBase> characters; // 게임 내 모든 캐릭터를 관리하는 리스트

    void Start()
    {
        characters = new List<CharacterBase>();
    }

    void OnEnable()
    {
        // 게임 오버 및 클리어 이벤트를 구독하여 스스로를 끄게 만듭니다.
        CharacterManager.OnGameOver += DisableInput;
        WaveManager.OnStageClear += DisableInput;
    }

    void OnDisable()
    {
        CharacterManager.OnGameOver -= DisableInput;
        WaveManager.OnStageClear -= DisableInput;
    }

    private void DisableInput()
    {
        // 컴포넌트를 비활성화하여 Update() 실행을 막음
        this.enabled = false; 
        
        // 사격 중이었다면 떼진 것으로 안전하게 처리
        OnIdle?.Invoke();
        OnFireRelease?.Invoke();
        wasFiring = false;
        
        Debug.Log("InputManager disabled due to game end.");
    }
    
    public static void InvokeSwitchCharacter(int index)
    {
        OnSwitchCharacter?.Invoke(index);
    }

    void Update()
    {
        //bool isMouseOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        bool isFiring = Input.GetMouseButton(0);

        // 강제 리로딩 중이라면 클릭 했을 때 OnFire, OnFirePress, OnFireRelease 이벤트를 차단해야 엄폐로 보고 sheild가 깎임
        if(Input.GetMouseButtonDown(0))
            OnFirePress?.Invoke();

        if(Input.GetMouseButtonUp(0))
            OnFireRelease?.Invoke();

        if(isFiring)
            OnFire?.Invoke();
        
        if(!isFiring && wasFiring)  // 뗀 순간 1번만
            OnIdle?.Invoke();

        wasFiring = isFiring;
        if(Input.GetKeyDown(KeyCode.Space))
            OnCoverToggle?.Invoke();
        if(Input.GetKeyDown(KeyCode.Alpha1))
            OnSwitchCharacter?.Invoke(0);
        if(Input.GetKeyDown(KeyCode.Alpha2))
            OnSwitchCharacter?.Invoke(1);
        if(Input.GetKeyDown(KeyCode.Alpha3))
            OnSwitchCharacter?.Invoke(2);
    }
}