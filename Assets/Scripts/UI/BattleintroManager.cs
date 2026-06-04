using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 시작 인트로 — UI 연출 전담 (책임: 다이아몬드 / 크로스헤어 / 텍스트 / 페이드)
/// 카메라 위치 결정 권한은 CameraController 에 위임 (Single Source of Truth).
/// 전체 시퀀스는 2초 이내에 종료되도록 조율됨.
/// </summary>
public class BattleIntroManager : MonoBehaviour
{
    public static event System.Action OnBattleIntroComplete;

    [Header("── Camera ──────────────────────")]
    [Tooltip("비워두면 Camera.main 자동 사용")]
    [SerializeField] private Camera           targetCamera;
    //[SerializeField] private CameraController cameraController;     // 인트로 동안 enable=false 잠금용. 카메라 위치 계산에는 사용 안 함.
    [Tooltip("인트로 시작 카메라 위치 (월드 좌표)")]
    [SerializeField] private Vector3          cameraIntroStartPos = new Vector3(0f, 0f, 15f);
    [Tooltip("인트로 종료 카메라 위치 — CameraController 가 Start 에서 도달하는 위치와 동일해야 점프 없음")]
    [SerializeField] private Vector3          cameraIntroEndPos   = new Vector3(0f, -1.5f, -7f);
    [SerializeField] private float            cameraDuration      = 2.0f; // 카메라가 종료점까지 도달하는 시간

    [Header("── Diamonds ─────────────────────")]
    [SerializeField] private RectTransform outerDiamond;
    [SerializeField] private RectTransform innerDiamond;
    [SerializeField] private CanvasGroup   diamondGroup;
    [SerializeField] private float         diamondStartScale = 1.1f;
    [SerializeField] private float         outerFinalScale   = 0.9f;
    [SerializeField] private float         innerFinalScale   = 1.0f;
    [SerializeField] private float         outerShrinkDur    = 0.5f;
    [SerializeField] private float         innerShrinkDur    = 0.8f;
    [SerializeField] private float         overlapThreshold  = 0.12f;

    [Header("── Crosshair ────────────────────")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private float         crosshairRotSpeed = 40f;
    [SerializeField] private CanvasGroup   crosshairGroup;

    [Header("── Text ─────────────────────────")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("── Canvas Group ─────────────────")]
    [SerializeField] private CanvasGroup introGroup;

    [Header("── Timing ───────────────────────")]
    [SerializeField] private float blinkInterval    = 0.05f;
    [SerializeField] private float textHoldTime     = 0.15f;
    [SerializeField] private float diceRollDuration = 0.10f;
    [SerializeField] private float diceSlideDistance = 50f;
    [SerializeField] private float fadeOutDuration  = 0.30f;

    public static bool IsComplete { get; private set; } = false;
    private bool      _rotateCrosshair = false;
    private Transform _camTransform;
    private Vector3   _camGameplayPos;     // 인트로 종료 목표 = CameraController 의 게임플레이 위치

    void Awake()
    {
        InitState();

        // if (cameraController != null)
        //     cameraController.enabled = false;
    }

    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (!_rotateCrosshair) return;
        crosshair.Rotate(0f, 0f, crosshairRotSpeed * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════════
    //  메인 시퀀스 (목표: 총 길이 ≤ 2.0s)
    //  타임라인:
    //   [0.00 ~ 1.00] PullCamera + ShrinkDiamonds 병렬 (+ 크로스헤어 회전)
    //   [1.00 ~ 1.20] DiceRollText "APPROVED"
    //   [1.20 ~ 1.35] textHoldTime
    //   [1.35 ~ 1.45] BlinkText(1)
    //   [1.45 ~ 1.60] textHoldTime + "OPERATION START"
    //   [1.60 ~ 1.90] FadeOut (intro + crosshair 병렬)
    // ═══════════════════════════════════════════════════════
    IEnumerator PlayIntro()
    {
        // 인트로 UI 즉시 표시 (페이드인 없이 곧장 켜짐)
        introGroup.alpha     = 1f;
        crosshairGroup.alpha = 0.4f;
        _rotateCrosshair     = true;

        // ── Phase 1: 카메라 풀과 다이아 수축을 동시에 시작 ──
        Coroutine camRoutine     = StartCoroutine(PullCamera());
        Coroutine diamondRoutine = StartCoroutine(ShrinkDiamonds());
        yield return camRoutine;
        yield return diamondRoutine;

        // ── Phase 2: 텍스트 전환 SYSTEM ACCESS → APPROVED ──
        yield return StartCoroutine(DiceRollText("APPROVED"));
        yield return new WaitForSeconds(textHoldTime);

        // ── Phase 3: 텍스트 점멸 후 OPERATION START 표시 ──
        yield return StartCoroutine(BlinkText(1));
        statusText.text = "";
        titleText.text  = "OPERATION START";
        yield return new WaitForSeconds(textHoldTime);

        // ── Phase 4: 페이드아웃 (두 그룹 병렬) ──
        _rotateCrosshair = false;
        Coroutine c1 = StartCoroutine(FadeGroup(introGroup,     1f,   0f, fadeOutDuration));
        Coroutine c2 = StartCoroutine(FadeGroup(crosshairGroup, 0.4f, 0f, fadeOutDuration));
        yield return c1;
        yield return c2;

        OnIntroFinished();
    }

    void InitState()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        _camTransform = targetCamera.transform;

        // 인트로 종료점 = 인스펙터 입력값 (CameraController.Start 의 도달 위치와 정확히 일치해야 점프 안 보임)
        _camGameplayPos = cameraIntroEndPos;

        // 인트로 시작점 = 인스펙터 입력값 — 카메라를 즉시 이 위치로 이동
        _camTransform.position = cameraIntroStartPos;

        // 다이아 초기 스케일
        Vector3 s = Vector3.one * diamondStartScale;
        outerDiamond.localScale = s;
        innerDiamond.localScale = s;

        // 텍스트 초기 상태
        titleText.text   = "BATTLE COMMAND";
        statusText.text  = "...SYSTEM ACCESS...";
        titleText.alpha  = 1f;
        statusText.alpha = 1f;

        // 시작 시점에는 안 보이게 (PlayIntro 시작과 동시에 1로 켜짐)
        introGroup.alpha     = 0f;
        crosshairGroup.alpha = 0f;
    }

    IEnumerator PullCamera()
    {
        float   elapsed  = 0f;
        Vector3 startPos = _camTransform.position;
        Vector3 endPos   = _camGameplayPos;

        while (elapsed < cameraDuration)
        {
            elapsed += Time.deltaTime;
            _camTransform.position = Vector3.Lerp(startPos, endPos,
                EaseOutCubic(Mathf.Clamp01(elapsed / cameraDuration)));
            yield return null;
        }
        _camTransform.position = endPos;
    }

    IEnumerator ShrinkDiamonds()
    {
        float   elapsed    = 0f;
        float   maxDur     = Mathf.Max(outerShrinkDur, innerShrinkDur);
        Vector3 startScale = Vector3.one * diamondStartScale;
        Vector3 outerEnd   = Vector3.one * outerFinalScale;
        Vector3 innerEnd   = Vector3.one * innerFinalScale;

        bool blinkFired = false;

        while (elapsed < maxDur)
        {
            elapsed += Time.deltaTime;

            float tO = EaseOutCubic(Mathf.Clamp01(elapsed / outerShrinkDur));
            float tI = EaseOutCubic(Mathf.Clamp01(elapsed / innerShrinkDur));

            outerDiamond.localScale = Vector3.Lerp(startScale, outerEnd, tO);
            innerDiamond.localScale = Vector3.Lerp(startScale, innerEnd, tI);

            float diff         = Mathf.Abs(outerDiamond.localScale.x - innerDiamond.localScale.x);
            bool  outerNearEnd = outerDiamond.localScale.x <= outerFinalScale + 0.3f;

            // 메인 루프 멈춤 방지 위해 yield 없이 fire-and-forget
            if (!blinkFired && diff < overlapThreshold && outerNearEnd)
            {
                blinkFired = true;
                StartCoroutine(BlinkDiamonds(2));
            }

            yield return null;
        }

        outerDiamond.localScale = outerEnd;
        innerDiamond.localScale = innerEnd;
    }

    IEnumerator BlinkDiamonds(int count)
    {
        for (int i = 0; i < count; i++)
        {
            diamondGroup.alpha = 0f;
            yield return new WaitForSeconds(blinkInterval);
            diamondGroup.alpha = 1f;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    IEnumerator DiceRollText(string nextStatus)
    {
        RectTransform rt      = statusText.rectTransform;
        Vector2       origPos = rt.anchoredPosition;
        float         elapsed = 0f;

        while (elapsed < diceRollDuration)
        {
            elapsed            += Time.deltaTime;
            float t             = EaseInCubic(Mathf.Clamp01(elapsed / diceRollDuration));
            rt.anchoredPosition = origPos + Vector2.down * (diceSlideDistance * t);
            statusText.alpha    = 1f - t;
            yield return null;
        }

        statusText.text     = nextStatus;
        statusText.alpha    = 0f;
        rt.anchoredPosition = origPos + Vector2.up * diceSlideDistance;
        elapsed             = 0f;

        while (elapsed < diceRollDuration)
        {
            elapsed            += Time.deltaTime;
            float t             = EaseOutCubic(Mathf.Clamp01(elapsed / diceRollDuration));
            rt.anchoredPosition = Vector2.Lerp(origPos + Vector2.up * diceSlideDistance, origPos, t);
            statusText.alpha    = t;
            yield return null;
        }

        rt.anchoredPosition = origPos;
        statusText.alpha    = 1f;
    }

    IEnumerator BlinkText(int count)
    {
        for (int i = 0; i < count; i++)
        {
            titleText.alpha  = 0f;
            statusText.alpha = 0f;
            yield return new WaitForSeconds(blinkInterval);
            titleText.alpha  = 1f;
            statusText.alpha = 1f;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float dur)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseInCubic(float t)  => t * t * t;

    void OnIntroFinished()
    {
        IsComplete = true;
        // CameraController 에게 통제권 반환 (이벤트 + 직접 enable 양쪽 안전망)
        // if (cameraController != null) cameraController.enabled = true;
        OnBattleIntroComplete?.Invoke();
        gameObject.SetActive(false);
    }
}
