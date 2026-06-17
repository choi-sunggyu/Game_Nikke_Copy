using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    //이벤트 구독, 현재 활성 캐릭터에게 전달
    private CharacterBase currentCharacter;
    [SerializeField] private List<CharacterBase> characters; // - 게임 내 모든 캐릭터를 관리하는 리스트
    private bool changing; // - 전환 중인지 아닌지
    private float delayTime; // - 전환 딜레이 시간
    private bool isCovering = false; // 엄폐 상태
    public bool IsCovering => isCovering;
    public List<CharacterBase> Characters => characters;
    public CharacterBase CurrentCharacter => currentCharacter;
    public static event Action OnGameOver;
    public static event Action<int> OnCharacterSwitchConfirmed;
    public void ToggleCover()
    {
        HandleCoverToggle();
    }

    void Awake()
    {
        int startIndex = characters.Count / 2;
        currentCharacter = characters[startIndex];  // Awake로 이동
        if(currentCharacter == null)
        {
            Debug.LogError("캐릭터가 연결되지 않았습니다");
        }
    }

    IEnumerator Start()
    {
        yield return null; // Start에서 초기화 순서 보장 위해 한 프레임 대기
        int startIndex = characters.Count / 2;
        currentCharacter = characters[startIndex];

        foreach (var c in characters)
        {
            c.IsActiveCharacter = false;
        }
        currentCharacter.IsActiveCharacter = true;

        InputManager.InvokeSwitchCharacter(startIndex);
        CharacterBase.InvokeBulletCountChanged(currentCharacter, currentCharacter.CurrentBulletCount);
    }

    void OnEnable()
    {
        //이벤트 구독
        InputManager.OnFire += HandleFire;
        InputManager.OnIdle += HandleIdle;
        InputManager.OnSwitchCharacter += SwitchCharacter;
        CharacterBase.OnCharacterDied += HandleCharacterDied;
        InputManager.OnCoverToggle += HandleCoverToggle;
    }

    void HandleFire()
    {
        // RL(런처) 캐릭터는 OnFire 매 프레임 발화로 TryFire 가 호출되면 fireRate 우회 때문에
        // 매 프레임 발사 → 탄창 즉시 소진. LauncherCrossHair 의 차지 시스템이 발사 시점을 단독 제어해야 함.
        // 따라서 RL 의 경우 HandleFire 는 무시하고, LauncherCrossHair.Update 가 ratio>=1 시점에만 owner.TryFire() 호출.
        if (currentCharacter != null && currentCharacter.WeaponType == WeaponType.RL) return;

        currentCharacter.TryFire();
    }

    void HandleIdle()
    {
        // RL(런처) 캐릭터는 마우스 떼는 순간마다 강제 reload 가 발생하면 탄창이 남아도 발사 불가가 됨.
        // RL 의 reload 시점은 Ghost.TryFire 내부의 "bulletCount==0 시 TryReload" 가 단독 결정.
        // LauncherCrossHair 는 reload 중에는 차지 시작을 막아 자연스럽게 사이클이 끊김.
        if (currentCharacter != null && currentCharacter.WeaponType == WeaponType.RL) return;

        currentCharacter.TryReload();
    }

    private void HandleCoverToggle()
    {
        isCovering = !isCovering;

        if (isCovering)
        {
            // 엄폐 진입: AI 캐릭터 사격 중단 → 리로드 or Idle
            foreach (var c in characters)
            {
                if (!c.IsAlive) continue; // 사망 시 무시
                if (c == currentCharacter) continue; // 조작 캐릭터는 아래에서 별도 처리
                c.TryReload(); // 탄창 가득 차있으면 내부 guard에서 Idle 처리
            }

            // 조작 중인 캐릭터: 클릭하여 사격 중이라도 강제 리로딩(일시적 강제 엄폐).
            // 리로드가 끝나면 잠금이 풀려, 마우스를 계속 누르고 있었다면 사격이 자동 재개된다.
            currentCharacter.ForceCoverReload();
        }
        else
        {
            // 엄폐 해제: 별도 처리 없음 (다음 입력 때 자연스럽게 재개)
        }
    }

    public void SwitchCharacter(int index)
    {
        // 안전망: 인스펙터의 characters 리스트가 호출 인덱스보다 작으면 무시
        if (characters == null || index < 0 || index >= characters.Count)
        {
            Debug.LogWarning($"[CharacterManager] SwitchCharacter({index}) — characters 리스트 크기 부족 ({characters?.Count ?? 0}). 인스펙터 할당 확인 필요.");
            return;
        }
        // 조건 체크 3가지
        // 1. 전환 중이면 무시
        // 2. 요청한 캐릭터가 사망했으면 무시
        // 3. 요청한 캐릭터가 현재 캐릭터면 무시
        if (!BattleIntroManager.IsComplete) return;
        if(changing) return;
        if(!characters[index].IsAlive) return;
        if(characters[index] == currentCharacter) return;
        StartCoroutine(SwitchDelay(index));
    }

    private IEnumerator SwitchDelay(int index)
    {
        changing = true;

        currentCharacter.IsActiveCharacter = false;
        currentCharacter.StopAllSounds(); // 전환 시 현재 캐릭터의 모든 사운드 즉시 정지

        if (isCovering)
            currentCharacter.TryReload(); // 엄폐 중 → 이전 캐릭터 Reload
        else
            currentCharacter.TryReload();  // 비엄폐 중 → 클릭 중 전환 시 이전 캐릭터도 Reload

        // currentCharacter 변경
        currentCharacter = characters[index];
        currentCharacter.ChangeState(CharacterState.Idle); // 전환 시 현재 캐릭터 상태 Idle로 강제 변경
        currentCharacter.IsActiveCharacter = true;

        // 전환 시 새 캐릭터 BulletCount 이벤트 발생
        CharacterBase.InvokeBulletCountChanged(currentCharacter, currentCharacter.CurrentBulletCount);
        InputManager.InvokeSwitchCharacter(index);
        
        yield return new WaitForSeconds(delayTime);
        changing = false;
    }

    private void HandleSwitchCharacter(int index)
    {
        // 사망 체크 등 검증...
        if (!characters[index].IsAlive)
        {
            Debug.Log("요청한 캐릭터 사망");
            return;
        }

        // 검증 통과 후 CurrentCharacter 업데이트
        currentCharacter = characters[index];

        // ← 검증 완료 후에만 발화
        OnCharacterSwitchConfirmed?.Invoke(index);
    }

    private void HandleCharacterDied(CharacterBase dead)
    {
        // 사망한 캐릭터가 현재 캐릭터일 때만 자동 전환
        if (dead != currentCharacter) return;

        // 살아있는 캐릭터 중 HP 최저 캐릭터 선택
        CharacterBase nextCharacter = null;
        float lowestHp = float.MaxValue;

        foreach (var c in characters)
        {
            if (!c.IsAlive || c == dead) continue;
            if (c.HpRatio < lowestHp)
            {
                lowestHp = c.HpRatio;
                nextCharacter = c;
            }
        }

        if (nextCharacter == null)
        {
            OnGameOver?.Invoke();
            return;
        }

        int nextIndex = characters.IndexOf(nextCharacter);
        StartCoroutine(SwitchDelay(nextIndex));
    }
    
    void OnDisable() {
        InputManager.OnFire -= HandleFire;
        InputManager.OnIdle -= HandleIdle;
        InputManager.OnSwitchCharacter -= SwitchCharacter;
        CharacterBase.OnCharacterDied -= HandleCharacterDied;
        InputManager.OnCoverToggle -= HandleCoverToggle;
    }
}
