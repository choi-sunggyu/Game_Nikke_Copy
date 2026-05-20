using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{//이벤트 구독, 현재 활성 캐릭터에게 전달
    private CharacterBase currentCharacter;
    [SerializeField] private List<CharacterBase> characters; // - 게임 내 모든 캐릭터를 관리하는 리스트
    private bool changing; // - 전환 중인지 아닌지
    private float delayTime; // - 전환 딜레이 시간
    public List<CharacterBase> Characters => characters;
    public CharacterBase CurrentCharacter => currentCharacter;

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
    }

    void HandleFire()
    {
        Debug.Log("HandleFire 호출됨");
        currentCharacter.TryFire();
    }

    void HandleIdle()
    {
        currentCharacter.TryReload();
    }

    public void SwitchCharacter(int index)
    {
        // 조건 체크 3가지 먼저
        // 1. 전환 중이면 무시
        // 2. 요청한 캐릭터가 사망했으면 무시
        // 3. 요청한 캐릭터가 현재 캐릭터면 무시
        if(changing)
        {
            Debug.Log("캐릭터 전환 중입니다.");
            return;
        }
        if(!characters[index].IsAlive)
        {
            Debug.Log("요청한 캐릭터는 사망했습니다.");
            return;
        }
        if(characters[index] == currentCharacter)
        {
            Debug.Log("요청한 캐릭터는 현재 캐릭터와 같습니다.");
            return;
        }
        // 통과하면 StartCoroutine 호출
        StartCoroutine(SwitchDelay(index));
    }

    private IEnumerator SwitchDelay(int index)
    {
        changing = true;

        currentCharacter.IsActiveCharacter = false;
        currentCharacter.StopAllSounds(); // 전환 시 현재 캐릭터의 모든 사운드 즉시 정지

        // currentCharacter 변경
        currentCharacter = characters[index];
        currentCharacter.ChangeState(CharacterState.Idle); // 전환 시 현재 캐릭터 상태 Idle로 강제 변경
        currentCharacter.IsActiveCharacter = true;

        // 전환 시 새 캐릭터 BulletCount 이벤트 발생
        CharacterBase.InvokeBulletCountChanged(currentCharacter, currentCharacter.CurrentBulletCount);

        yield return new WaitForSeconds(delayTime);
        changing = false;
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

        if (nextCharacter == null) return; // 전원 사망

        int nextIndex = characters.IndexOf(nextCharacter);
        StartCoroutine(SwitchDelay(nextIndex));
    }
    
    void OnDisable() {
        InputManager.OnFire -= HandleFire;
        InputManager.OnIdle -= HandleIdle;
        InputManager.OnSwitchCharacter -= SwitchCharacter;
        CharacterBase.OnCharacterDied -= HandleCharacterDied;
    }
}
