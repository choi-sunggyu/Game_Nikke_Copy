using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // InputManager에서 구독한 이벤트를 통해 캐릭터 전환 시 카메라 위치 업데이트
    // 번호키에 따라 캐릭터 전환이 일어나면 해당 캐릭터 위치로 카메라가 부드럽게 이동하도록 구현
    private Vector3 targetPosition; // 카메라가 따라갈 목표 위치
    [SerializeField] private float moveSpeed; // 카메라 이동 속도
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3.5f, 0f); // 카메라가 캐릭터보다 약간 위에 위치하도록 하는 오프셋
    [SerializeField] private float cameraZOffset = -10f; // 카메라가 캐릭터보다 뒤에 위치하도록 하는 Z축 오프셋
    [SerializeField] private List<CharacterBase> characters; // 게임 내 모든 캐릭터를 관리하는 리스트

    [SerializeField] private Camera cam;
    [SerializeField] private float targetZoom = 40f;
    [SerializeField] private float zoomSpeed = 5f;

    private int _currentCharacterIndex;
    
    public static bool FovLocked { get; set; } = false;
    public static bool PositionLocked { get; set; } = false;

    public void SetTargetZoom(float fov) => targetZoom = fov;

    void Start()
    {
        // 초기 카메라 위치는 가운데 있는 캐릭터로 설정
        if(characters.Count > 1)
        {
            int mid = characters.Count / 2;
            Vector3 charPos = characters[mid].transform.position;
            
            targetPosition = charPos + cameraOffset;
            targetPosition.z = charPos.z + cameraZOffset;
            
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
        if (!characters[index].IsAlive) return;

        _currentCharacterIndex = index;
        Vector3 charPos = characters[index].transform.position;
        targetPosition = charPos + cameraOffset;
        targetPosition.z = charPos.z + cameraZOffset;
    }

    void Update()
    {
        // 목표 위치로 부드럽게 이동
        if (!PositionLocked)
        {
            transform.position = Vector3.Lerp(
                transform.position, 
                targetPosition, 
                moveSpeed * Time.deltaTime
            );
        }

        if (!FovLocked)
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetZoom, zoomSpeed * Time.deltaTime);
    }

    // 보스 출현 연출
    public void StartBossAppearCinematic(EnemyBase boss, float duration, float zoomFov, System.Action onComplete)
    {
        StartCoroutine(BossAppearRoutine(boss, duration, zoomFov, onComplete));
    }

    // 보스 사망 연출
    public void StartBossDeathCinematic(float zoomFov, float zoomDuration, Vector3 bossPosition, System.Action onComplete)
    {
        StartCoroutine(BossDeathRoutine(zoomFov, zoomDuration, bossPosition, onComplete));
    }

    private IEnumerator BossAppearRoutine(EnemyBase boss, float duration, float zoomFov, System.Action onComplete)
    {
        PositionLocked = true;
        FovLocked      = true;

        float elapsed  = 0f;
        float startFov = cam.fieldOfView;

        Vector3 bossTarget = boss.transform.position + cameraOffset;
        bossTarget.z       = boss.transform.position.z + cameraZOffset;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;

            transform.position = Vector3.MoveTowards(
                transform.position,
                bossTarget,
                10f * Time.deltaTime
            );

            cam.fieldOfView = Mathf.Lerp(startFov, zoomFov, t);
            yield return null;
        }

        MoveToCharacter(_currentCharacterIndex);

        // 조작 캐릭터로 복귀 — PositionLocked 해제 후 Update()가 자연스럽게 복귀
        FovLocked      = false;
        PositionLocked = false;
        targetZoom     = 40f; // 기본 FOV 복귀

        onComplete?.Invoke();
    }

    private IEnumerator BossDeathRoutine(float zoomFov, float zoomDuration, Vector3 bossPosition, System.Action onComplete)
    {
        PositionLocked = true;
        FovLocked      = true;

        Vector3 deathTarget = bossPosition + cameraOffset;
        deathTarget.z       = bossPosition.z + cameraZOffset;

        float elapsed  = 0f;
        float startFov = cam.fieldOfView;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / zoomDuration;

            transform.position = Vector3.Lerp(transform.position, deathTarget, 8f * Time.deltaTime);
            cam.fieldOfView    = Mathf.Lerp(startFov, zoomFov, t);
            yield return null;
        }

        onComplete?.Invoke();
        // PositionLocked, FovLocked는 게임 종료 UI 뜰 때까지 유지
    }
}