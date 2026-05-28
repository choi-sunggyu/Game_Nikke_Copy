using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleIntroManager : MonoBehaviour
{
    public static event System.Action OnBattleIntroComplete;

    [Header("── Camera ──────────────────────")]
    [Tooltip("비워두면 Camera.main 자동 사용")]
    [SerializeField] private Camera         targetCamera;
    [SerializeField] private float         cameraIntroZOffset = 8f;
    [SerializeField] private float         cameraDuration     = 0.4f; // 0.5s -> 0.4s 단축
    [SerializeField] private MonoBehaviour cameraController;

    [Header("── Diamonds ─────────────────────")]
    [SerializeField] private RectTransform outerDiamond;
    [SerializeField] private RectTransform innerDiamond;
    [SerializeField] private CanvasGroup   diamondGroup;
    [SerializeField] private float         diamondStartScale = 1.2f;
    [SerializeField] private float         outerFinalScale   = 0.9f;
    [SerializeField] private float         innerFinalScale   = 1.0f;
    [SerializeField] private float         outerShrinkDur    = 0.5f; // 0.6s -> 0.5s 단축
    [SerializeField] private float         innerShrinkDur    = 0.8f; // 1.0s -> 0.8s 단축
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
    [SerializeField] private float blinkInterval     = 0.05f; // 0.06s -> 0.05s 단축
    [SerializeField] private float textHoldTime      = 0.2f;  // 0.25s -> 0.2s 단축
    [SerializeField] private float diceRollDuration = 0.12f; // 0.15s -> 0.12s 단축
    [SerializeField] private float diceSlideDistance= 50f;

    private bool      _rotateCrosshair = false;
    private Transform _camTransform;
    private Vector3   _camOriginalPos;

    void Awake()
    {
        InitState();
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
    //  메인 시퀀스 수정
    // ═══════════════════════════════════════════════════════
    IEnumerator PlayIntro()
    {
        // [해결 3] 게임 시작 즉시 UI를 보여주어 '유저 체감 지연 대기시간'을 제거
        introGroup.alpha     = 1f;
        crosshairGroup.alpha = 0.4f;
        _rotateCrosshair     = true;

        // ① 카메라 후퇴 (UI 연출과 동시에 진행하거나 빠르게 처리)
        yield return StartCoroutine(PullCamera());

        // ② 마름모 수축 및 점멸 (내부 정지 버그 수정됨)
        yield return StartCoroutine(ShrinkDiamonds());

        // ③ 주사위 전환: SYSTEM ACCESS → APPROVED
        yield return StartCoroutine(DiceRollText("APPROVED"));
        yield return new WaitForSeconds(textHoldTime);

        // ④ OPERATION START: 텍스트만 점멸
        yield return StartCoroutine(BlinkText(1));
        statusText.text = "";
        titleText.text  = "OPERATION START";
        yield return new WaitForSeconds(textHoldTime);

        // ⑤ 종료 페이드아웃 (두 그룹을 동시에 페이드하기 위해 yield를 하나로 병합)
        _rotateCrosshair = false;
        Coroutine c1 = StartCoroutine(FadeGroup(introGroup,     1f,   0f, 0.15f));
        Coroutine c2 = StartCoroutine(FadeGroup(crosshairGroup, 0.4f, 0f, 0.15f));
        yield return c1;
        yield return c2;

        OnIntroFinished();
    }

    void InitState()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        _camTransform   = targetCamera.transform;
        _camOriginalPos = _camTransform.position;

        if (cameraController != null) cameraController.enabled = false;

        Vector3 introPos = _camOriginalPos;
        introPos.z      += cameraIntroZOffset;
        _camTransform.position = introPos;

        Vector3 s = Vector3.one * diamondStartScale;
        outerDiamond.localScale = s;
        innerDiamond.localScale = s;

        titleText.text   = "BATTLE COMMAND";
        statusText.text  = "...SYSTEM ACCESS...";
        titleText.alpha  = 1f;
        statusText.alpha = 1f;

        // 시작 시점에 깜빡이지 않도록 투명도 초기화
        introGroup.alpha     = 0f;
        crosshairGroup.alpha = 0f;
    }

    IEnumerator PullCamera()
    {
        float   elapsed  = 0f;
        Vector3 startPos = _camTransform.position;
        Vector3 endPos   = _camOriginalPos;

        while (elapsed < cameraDuration)
        {
            elapsed += Time.deltaTime;
            _camTransform.position = Vector3.Lerp(startPos, endPos,
                EaseOutCubic(Mathf.Clamp01(elapsed / cameraDuration)));
            yield return null;
        }
        _camTransform.position = endPos;
    }

    // ═══════════════════════════════════════════════════════
    //  마름모 수축 로직 수정 (핵심 수정)
    // ═══════════════════════════════════════════════════════
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

            // [해결 1] yield return 없이 StartCoroutine만 실행하여 메인 루프 멈춤을 방지!
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
        RectTransform rt       = statusText.rectTransform;
        Vector2       origPos = rt.anchoredPosition;
        float         elapsed = 0f;

        while (elapsed < diceRollDuration)
        {
            elapsed          += Time.deltaTime;
            float t           = EaseInCubic(Mathf.Clamp01(elapsed / diceRollDuration));
            rt.anchoredPosition = origPos + Vector2.down * (diceSlideDistance * t);
            statusText.alpha  = 1f - t;
            yield return null;
        }

        statusText.text     = nextStatus;
        statusText.alpha    = 0f;
        rt.anchoredPosition = origPos + Vector2.up * diceSlideDistance;
        elapsed             = 0f;

        while (elapsed < diceRollDuration)
        {
            elapsed          += Time.deltaTime;
            float t           = EaseOutCubic(Mathf.Clamp01(elapsed / diceRollDuration));
            rt.anchoredPosition = Vector2.Lerp(origPos + Vector2.up * diceSlideDistance, origPos, t);
            statusText.alpha  = t;
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
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseInCubic(float t)  => t * t * t;

    void OnIntroFinished()
    {
        if (cameraController != null) cameraController.enabled = true;
        OnBattleIntroComplete?.Invoke();
        gameObject.SetActive(false);
    }
}