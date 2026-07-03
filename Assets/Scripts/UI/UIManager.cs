using System;
using UnityEngine;

/// <summary>
/// 전투 UI 통합 관리 + 카메라 컷씬 facade.
///
/// 카메라 권한 정책 (월권 차단):
///   - 외부 컴포넌트는 CameraController 를 직접 호출하지 않는다.
///   - 모든 카메라 컷씬 트리거는 UIManager.Instance.Trigger*() 메서드를 통해 호출한다.
///   - UIManager 가 CameraController API 호출의 단일 진입점.
///   - 이유: 카메라 사용 위치를 한 곳에 모아 충돌/잠금 누락을 컴파일 단위로 감지 가능.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("── Battle UI ───────────────")]
    [SerializeField] private CanvasGroup battleUICanvasGroup;

    [Header("── Camera Facade ───────────")]
    [Tooltip("CameraController 참조. 비워두면 Awake 에서 자동 탐색")]
    [SerializeField] private CameraController cameraController;

    [Tooltip("보스 등장 컷씬 — 카메라 줌인+줌아웃 총 시간")]
    [SerializeField] private float bossAppearDuration = 1.0f;
    [Tooltip("보스 등장 컷씬 — 줌인 시 FOV")]
    [SerializeField] private float bossAppearZoomFov = 30f;

    [Tooltip("보스 사망 컷씬 — 줌인 시간")]
    [SerializeField] private float bossDeathDuration = 0.5f;
    [Tooltip("보스 사망 컷씬 — 줌인 시 FOV")]
    [SerializeField] private float bossDeathZoomFov = 25f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();

        HideBattleUI();
    }

    void OnEnable()
    {
        BattleIntroManager.OnBattleIntroComplete += ShowBattleUI;
        CharacterManager.OnGameOver              += HideBattleUI;
        GameTimerManager.OnTimeUp                += HideBattleUI;
        EnemyBase.OnBossDefeated                 += OnBossDefeatedHandler;
    }

    void OnDisable()
    {
        BattleIntroManager.OnBattleIntroComplete -= ShowBattleUI;
        CharacterManager.OnGameOver              -= HideBattleUI;
        GameTimerManager.OnTimeUp                -= HideBattleUI;
        EnemyBase.OnBossDefeated                 -= OnBossDefeatedHandler;
    }

    // OnBossDefeated 시그니처 어댑터
    private void OnBossDefeatedHandler(EnemyBase boss)
    {
        // 보스 사망 시 카메라 컷씬 자동 트리거 (UIManager 가 단일 권한자)
        if (boss != null)
            TriggerBossDeathCinematic(boss.transform.position);

        HideBattleUI();
    }

    // ═══════════════════════════════════════════════════════
    //  Battle UI on/off
    // ═══════════════════════════════════════════════════════
    private void ShowBattleUI()
    {
        if (battleUICanvasGroup == null) return;

        battleUICanvasGroup.alpha          = 1f;
        battleUICanvasGroup.interactable   = true;
        battleUICanvasGroup.blocksRaycasts = true;
    }

    private void HideBattleUI()
    {
        if (battleUICanvasGroup == null) return;

        battleUICanvasGroup.alpha          = 0f;
        battleUICanvasGroup.interactable   = false;
        battleUICanvasGroup.blocksRaycasts = false;
    }

    // ═══════════════════════════════════════════════════════
    //  Camera Facade — 외부 진입점
    //   외부 컴포넌트는 cameraController 를 직접 호출하지 말고
    //   UIManager.Instance.Trigger*() 메서드를 사용한다.
    // ═══════════════════════════════════════════════════════

    /// <summary>보스 등장 컷씬 트리거 — 줌인 → 줌아웃 2단계 자동 처리.</summary>
    public void TriggerBossAppearCinematic(EnemyBase boss, Action onComplete = null)
    {
        if (!ValidateCameraController()) { onComplete?.Invoke(); return; }
        cameraController.StartBossAppearCinematic(boss, bossAppearDuration, bossAppearZoomFov, onComplete);
    }

    /// <summary>보스 사망 컷씬 트리거 — 보스 위치에 줌인 후 동결 (게임 종료 UI 표시 동안 유지).</summary>
    public void TriggerBossDeathCinematic(Vector3 bossPosition, Action onComplete = null)
    {
        if (!ValidateCameraController()) { onComplete?.Invoke(); return; }
        cameraController.StartBossDeathCinematic(bossDeathZoomFov, bossDeathDuration, bossPosition, onComplete);
    }

    /// <summary>엘리트 등장 등 FOV 만 잠깐 조정. duration 후 자동 복귀 안 함 → ResetCameraFov 명시 호출 필요.</summary>
    public void TriggerCameraZoom(float zoomFov)
    {
        if (!ValidateCameraController()) return;
        cameraController.SetTargetFov(zoomFov);
    }

    /// <summary>카메라 FOV 를 기본값으로 복귀.</summary>
    public void ResetCameraFov()
    {
        if (!ValidateCameraController()) return;
        cameraController.ResetFov();
    }

    /// <summary>
    /// 카메라 pitch (rotation.x) offset 지정. 마우스 Y 위치 기반 사격 중 시선 상하.
    /// pitchOffset=+4 → 아래 시선, -4 → 위 시선. 사격 벗어나면 0 호출로 복귀.
    /// </summary>
    public void TriggerCameraTilt(float pitchOffset)
    {
        if (!ValidateCameraController()) return;
        cameraController.SetTargetPitch(pitchOffset);
    }

    /// <summary>임의 Transform 에 포커스 (엘리트 적 따라가기 등). 타겟 null 되면 자동으로 캐릭터 복귀.</summary>
    public void FocusCameraOn(Transform target, float fov)
    {
        if (!ValidateCameraController()) return;
        cameraController.FocusOn(target, fov);
    }

    /// <summary>카메라를 캐릭터 추적 모드로 복귀.</summary>
    public void ReturnCameraToCharacter()
    {
        if (!ValidateCameraController()) return;
        cameraController.FollowCharacter(cameraController.CurrentCharacterIndex);
    }

    /// <summary>게임 종료 시 카메라 동결 (모든 입력 무시).</summary>
    public void FreezeCamera()
    {
        if (!ValidateCameraController()) return;
        cameraController.Freeze();
    }

    private bool ValidateCameraController()
    {
        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();

        if (cameraController == null)
        {
            Debug.LogWarning("[UIManager] CameraController 미할당 — 카메라 컷씬 무시");
            return false;
        }
        return true;
    }
}
