using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private CharacterBase currentCharacter; // - 현재 조작 중인 캐릭터를 추적하는 변수
    [SerializeField] private List<CharacterBase> characters; // - 게임 내 모든 캐릭터를 관리하는 리스트
    private bool changing; // - 전환 중인지 아닌지
    private float delayTime; // - 전환 딜레이 시간

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentCharacter = characters[0];
        if(currentCharacter == null)
        {
            Debug.LogError("캐릭터가 연결되지 않았습니다");
        }
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
        // currentCharacter 변경
        currentCharacter = characters[index];
        yield return new WaitForSeconds(delayTime);
        changing = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
