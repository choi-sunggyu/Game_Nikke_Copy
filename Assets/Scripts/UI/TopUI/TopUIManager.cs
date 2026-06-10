using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopUIManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [Header("── UI 오브젝트 ───────────────────")]
    [SerializeField] private GameObject      waveProgressBarUI;
    [SerializeField] private GameObject      eliteWarningUI;
    [SerializeField] private TMP_Text        eliteWarningText;
    [SerializeField] private CanvasGroup     eliteWarningGroup;

    [Header("── 보스 HP바 ─────────────────────")]
    [SerializeField] private BossHPBar bossHPBar;
    [SerializeField] private GameObject bossHPBarUI;

    [Header("── 카메라 줌 설정 ──────────────")]
    [SerializeField] private Camera          targetCamera;
    [SerializeField] private float           eliteZoomFOV      = 40f;  // 줌인 시 FOV
    [SerializeField] private float           normalFOV         = 60f;  // 기본 FOV
    [SerializeField] private float           eliteZoomDuration = 1.0f; // 줌인 지속 시간
    [SerializeField] private float           zoomReturnDuration = 0.5f; // 원상복귀 시간

    [Header("── 경고 텍스트 설정 ─────────────")]
    [SerializeField] private float           warningBlinkSpeed  = 4f;
    [SerializeField] private float           warningDuration    = 2f;  // 경고 UI 표시 시간

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private float _originalFOV;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _originalFOV = targetCamera.fieldOfView;

        eliteWarningUI.SetActive(false);
    }

    void OnEnable()
    {
        WaveManager.OnElitePhaseStart += HandleElitePhaseStart;
        WaveManager.OnEliteDefeated   += HandleEliteDefeated;
        WaveManager.OnBossPhaseStart += HandleBossPhaseStart;
    }

    void OnDisable()
    {
        WaveManager.OnElitePhaseStart -= HandleElitePhaseStart;
        WaveManager.OnEliteDefeated   -= HandleEliteDefeated;
        WaveManager.OnBossPhaseStart -= HandleBossPhaseStart;
    }

    // ═══════════════════════════════════════════════════════
    //  엘리트 등장 처리
    // ═══════════════════════════════════════════════════════
    private void HandleElitePhaseStart()
    {
        StartCoroutine(ElitePhaseSequence());
    }

    // 보스 등장 처리
    private void HandleBossPhaseStart(EnemyBase boss)
    {
        waveProgressBarUI.SetActive(false);
        eliteWarningUI.SetActive(false);
        bossHPBar.Show(boss);
    }

    private IEnumerator ElitePhaseSequence()
    {
        // ① WaveProgressBar → EliteWarningUI 전환
        waveProgressBarUI.SetActive(false);
        eliteWarningUI.SetActive(true);

        // ② 경고 텍스트 페이드인 + 반짝임
        yield return StartCoroutine(ShowEliteWarning());

        // ③ 경고 UI 페이드아웃
        yield return StartCoroutine(FadeCanvasGroup(eliteWarningGroup, 1f, 0f, 0.3f));
        eliteWarningUI.SetActive(false);

        // ④ 카메라 줌인 (1초)
        yield return StartCoroutine(ZoomCamera(normalFOV, eliteZoomFOV, eliteZoomDuration));

        // ⑤ 원상복귀
        yield return StartCoroutine(ZoomCamera(eliteZoomFOV, normalFOV, zoomReturnDuration));
    }

    private IEnumerator ShowEliteWarning()
    {
        eliteWarningGroup.alpha = 0f;
        float elapsed = 0f;

        // 페이드인
        yield return StartCoroutine(FadeCanvasGroup(eliteWarningGroup, 0f, 1f, 0.3f));

        // 반짝임 유지 (warningDuration 동안)
        elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(
                0.4f,
                1.0f,
                (Mathf.Sin(Time.time * warningBlinkSpeed) + 1f) * 0.5f
            );

            eliteWarningGroup.alpha = alpha;
            yield return null;
        }

        eliteWarningGroup.alpha = 1f;
    }

    // ═══════════════════════════════════════════════════════
    //  엘리트 처치 처리
    // ═══════════════════════════════════════════════════════
    private void HandleEliteDefeated()
    {
        StartCoroutine(EliteDefeatedSequence());
    }

    private IEnumerator EliteDefeatedSequence()
    {
        // 빠르게 줌인 후 MissionClearUI로 전환
        yield return StartCoroutine(ZoomCamera(normalFOV, eliteZoomFOV, 0.3f));

        // MissionClearUI 활성화는 MissionClearUI 자체에서 이벤트 수신
        // TopUIManager는 줌 상태 유지만 담당
    }

    // ═══════════════════════════════════════════════════════
    //  카메라 줌 코루틴
    // ═══════════════════════════════════════════════════════
    private IEnumerator ZoomCamera(float fromFOV, float toFOV, float duration)
    {
        CameraController.FovLocked = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // EaseInOutCubic — 자연스러운 줌
            t = t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

            targetCamera.fieldOfView = Mathf.Lerp(fromFOV, toFOV, t);
            yield return null;
        }

        targetCamera.fieldOfView = toFOV;
        CameraController.FovLocked = false;
    }

    // ═══════════════════════════════════════════════════════
    //  페이드 헬퍼
    // ════════════════════════════════════════════════════
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }
}