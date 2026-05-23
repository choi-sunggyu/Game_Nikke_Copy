using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BurstSlotUI : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI keyLabel;       // 왼쪽 중간: A/S/D/F/G
    [SerializeField] private TextMeshProUGUI burstNumLabel;  // 왼쪽 하단: I/II/III
    [SerializeField] private Button slotButton;

    [Header("슬라이딩")]
    [SerializeField] private float slideInDuration  = 0.3f;
    [SerializeField] private float slideOutDuration = 0.2f;
    [SerializeField] private float hiddenOffsetX    = 300f;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private static readonly string[] RomanNumerals = { "", "I", "II", "III" };
    private static readonly string[] KeyLabels = { "A", "S", "D", "F", "G" };

    private RectTransform rectTransform;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;
    private CharacterBase assignedCharacter;
    private Coroutine slideCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        shownPosition = rectTransform.anchoredPosition;
        hiddenPosition = shownPosition + Vector2.right * hiddenOffsetX;
        rectTransform.anchoredPosition = hiddenPosition;
        gameObject.SetActive(false);
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    void OnEnable()
    {
        BurstGaugeManager.OnStepTimeChanged  += HandleTimeChanged;
        BurstGaugeManager.OnBurstConsumed   += HideCooldown;
    }

    void OnDisable()
    {
        BurstGaugeManager.OnStepTimeChanged  -= HandleTimeChanged;
        BurstGaugeManager.OnBurstConsumed   -= HideCooldown;
    }

    private void HandleTimeChanged(float remaining)
    {
        if (cooldownText == null) return;
        cooldownText.text = remaining.ToString("F1");
    }

    private void HideCooldown()
    {
        if (cooldownText == null) return;
        cooldownText.text = "";
    }

    public void Setup(CharacterBase character, int slotIndex)
    {
        assignedCharacter = character;
        keyLabel.text = KeyLabels[slotIndex];
        burstNumLabel.text = RomanNumerals[character.BurstNumber];
        characterImage.sprite = character.CharacterPortrait;
    }

    public void SlideIn()
    {
        gameObject.SetActive(true);
        if (cooldownText != null) cooldownText.text = "";
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideCoroutine(hiddenPosition, shownPosition, slideInDuration));
    }

    public void SlideOut()
    {
        if (!gameObject.activeSelf) return;
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideOutCoroutine());
    }

    private IEnumerator SlideCoroutine(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        rectTransform.anchoredPosition = to;
    }

    private IEnumerator SlideOutCoroutine()
    {
        yield return StartCoroutine(SlideCoroutine(shownPosition, hiddenPosition, slideOutDuration));
        gameObject.SetActive(false);
    }

    private void OnSlotClicked()
    {
        BurstGaugeManager.Instance?.TryUseBurstByCharacter(assignedCharacter);
    }
}