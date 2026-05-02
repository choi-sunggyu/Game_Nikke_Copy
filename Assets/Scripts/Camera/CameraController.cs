using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // InputManager에서 구독한 이벤트를 통해 캐릭터 전환 시 카메라 위치 업데이트
    // 번호키에 따라 캐릭터 전환이 일어나면 해당 캐릭터 위치로 카메라가 부드럽게 이동하도록 구현
    private Vector3 targetPosition; // 카메라가 따라갈 목표 위치
    [SerializeField] private float moveSpeed; // 카메라 이동 속도
    [SerializeField] private List<CharacterBase> characters; // 게임 내 모든 캐릭터를 관리하는 리스트

    [SerializeField] private Camera cam;
    [SerializeField] private float targetZoom = 5f;
    [SerializeField] private float zoomSpeed = 5f;
    
    void Start()
    {
        // 초기 카메라 위치는 가운데 있는 캐릭터로 설정
        if(characters.Count > 1)
        {
            targetPosition = characters[characters.Count / 2].transform.position;
            targetPosition.z = transform.position.z; // Z축은 고정
            transform.position = targetPosition;
        }
        else
        {
            Debug.LogError("캐릭터가 충분히 연결되지 않았습니다.");
        }
    }

    void OnEnable()
    {
        InputManager.OnSwitchCharacter += MoveToCharacter;
    }

    void OnDisable()
    {
        InputManager.OnSwitchCharacter -= MoveToCharacter;
    }

    void MoveToCharacter(int index)
    {
        // targetPosition 설정
        targetPosition = characters[index].transform.position;
        targetPosition.z = transform.position.z; // Z축은 고정
    }

    void Update()
    {
        // 목표 위치로 부드럽게 이동
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            moveSpeed * Time.deltaTime
        );

        // 확대/축소
        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            zoomSpeed * Time.deltaTime
        );
    }
}
