using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BurstGaugeUI : MonoBehaviour
{
    [Header("게이지 바")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hexagonLabel; // 육각형 안 로마자
    [SerializeField] private GameObject hexagonObject;     // 육각형 오브젝트

    [Header("타이머")]
    [SerializeField] private TextMeshProUGUI timerText;    // 남은 시간 텍스트
    [SerializeField] private RectTransform timerRect;      // 타이머 RectTransform
    [SerializeField] private float timerDropDistance = 60f; // 위에서 내려오는 거리
    [SerializeField] private float timerDropDuration = 0.4f;

    private static readonly Color ColorCharging  = new Color(0.8f, 0.5f, 0.2f); // 주황
    private static readonly Color ColorStep1     = Color.green;
    private static readonly Color ColorStep2     = Color.yellow;
    private static readonly Color ColorStep3     = Color.red;
    private static readonly Color ColorFocusFire = new Color(1f, 0f, 0.7f);     // 핑크/마젠타

    private static readonly string[] RomanNumerals = { "", "I", "II", "III" };

    private Coroutine timerCoroutine;
    private Coroutine dropCoroutine;
    private Vector2 timerShownPos;

    void Awake()
    {
        timerShownPos = timerRect.anchoredPosition;
        timerText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        BurstGaugeManager.OnGaugeChanged += HandleGaugeChanged;
        BurstGaugeManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        BurstGaugeManager.OnGaugeChanged -= HandleGaugeChanged;
        BurstGaugeManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandleGaugeChanged(float ratio)
    {
        fillImage.fillAmount = ratio;
    }

    private void HandlePhaseChanged(BurstPhase phase)
    {
        // 색상 전환
        fillImage.color = phase switch
        {
            BurstPhase.Charging   => ColorCharging,
            BurstPhase.Step1Ready => ColorStep1,
            BurstPhase.Step2Ready => ColorStep2,
            BurstPhase.Step3Ready => ColorStep3,
            BurstPhase.FocusFire  => ColorFocusFire,
            _                     => ColorCharging
        };

        // 육각형 숫자 전환
        switch (phase)
        {
            case BurstPhase.Step1Ready:
                hexagonLabel.text = RomanNumerals[1];
                hexagonObject.SetActive(true);
                StartTimerDrop(20f);
                break;
            case BurstPhase.Step2Ready:
                hexagonLabel.text = RomanNumerals[2];
                hexagonObject.SetActive(true);
                StartTimerDrop(20f);
                break;
            case BurstPhase.Step3Ready:
                hexagonLabel.text = RomanNumerals[3];
                hexagonObject.SetActive(true);
                StartTimerDrop(20f);
                break;
            case BurstPhase.FocusFire:
                hexagonObject.SetActive(false);
                StopTimer();
                break;
            case BurstPhase.Charging:
                hexagonObject.SetActive(false);
                StopTimer();
                break;
        }
    }

    private void StartTimerDrop(float duration)
    {
        StopTimer();
        timerText.gameObject.SetActive(true);
        if (dropCoroutine != null) StopCoroutine(dropCoroutine);
        dropCoroutine = StartCoroutine(DropAnimation());
        timerCoroutine = StartCoroutine(TimerCountdown(duration));
    }

    private IEnumerator DropAnimation()
    {
        Vector2 startPos = timerShownPos + Vector2.up * timerDropDistance;
        float elapsed = 0f;
        while (elapsed < timerDropDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            timerRect.anchoredPosition = Vector2.Lerp(startPos, timerShownPos, elapsed / timerDropDuration);
            yield return null;
        }
        timerRect.anchoredPosition = timerShownPos;
    }

    private IEnumerator TimerCountdown(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            timerText.text = remaining.ToString("F2");
            yield return null;
        }
        timerText.text = "0.00";
    }

    private void StopTimer()
    {
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        timerText.gameObject.SetActive(false);
        timerRect.anchoredPosition = timerShownPos;
    }
}