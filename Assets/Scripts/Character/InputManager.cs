using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private bool _inputLocked = true;
    private List<CharacterBase> characters; // 게임 내 모든 캐릭터를 관리하는 리스트

    void UnlockInput() => _inputLocked = false;


    void Start()
    {
        characters = new List<CharacterBase>();
    }

    void OnEnable()
    {
        BattleIntroManager.OnBattleIntroComplete += UnlockInput;
        // 게임 오버 및 클리어 이벤트를 구독하여 스스로를 끄게 만듭니다.
        CharacterManager.OnGameOver += DisableInput;
        WaveManager.OnStageClear += DisableInput;
    }

    void OnDisable()
    {
        BattleIntroManager.OnBattleIntroComplete -= UnlockInput;
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
        
    }

    public static void SetInputLocked(bool locked)
    {
        var instance = FindAnyObjectByType<InputManager>();
        Debug.Log($"[DIAG-5] InputManager.SetInputLocked({locked}) — instance={(instance == null ? "NULL" : "OK")}, 호출 스택 다음 줄 참고");
        if (instance != null)
        {
            instance._inputLocked = locked;
        }
    }
    
    public static void InvokeSwitchCharacter(int index)
    {
        OnSwitchCharacter?.Invoke(index);
    }

    void Update()
    {
        if (_inputLocked) return;

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

        // 5명 캐릭터 전환 키 — CharacterManager.Characters 의 인덱스 순서(= Burst 순서)
        // Z X C V B 는 키보드 왼손 아래쪽 한 줄 → 한 손으로 빠른 전환 가능
        //   Z(0): Ghost  (1버스트)
        //   X(1): Trend  (2버스트)
        //   C(2): Titan  (2버스트)
        //   V(3): Viper  (3버스트)
        //   B(4): Astro  (3버스트)
        if(Input.GetKeyDown(KeyCode.Z))
            OnSwitchCharacter?.Invoke(0);
        if(Input.GetKeyDown(KeyCode.X))
            OnSwitchCharacter?.Invoke(1);
        if(Input.GetKeyDown(KeyCode.C))
            OnSwitchCharacter?.Invoke(2);
        if(Input.GetKeyDown(KeyCode.V))
            OnSwitchCharacter?.Invoke(3);
        if(Input.GetKeyDown(KeyCode.B))
            OnSwitchCharacter?.Invoke(4);
    }
}