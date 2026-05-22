using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HPShieldBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image shieldBar;
    [SerializeField] private CharacterBase owner;

    [Header("Colors")]
    [SerializeField] private Color hpColor = Color.white;
    [SerializeField] private Color shieldColor = new Color(0.4f, 0.7f, 1f, 1f);

    [Header("Fill Animation")]
    [SerializeField] private float fillLerpSpeed = 5f;

    [Header("Morph - Front Bar (앞에 오는 바)")]
    [SerializeField] private Vector2 frontPosition = Vector2.zero;       // anchoredPosition
    [SerializeField] private Vector3 frontScale = Vector3.one;           // localScale (1,1,1)

    [Header("Morph - Back Bar (뒤에 가는 바)")]
    [SerializeField] private Vector2 backPosition = new Vector2(0f, 10f); // 살짝 위로
    [SerializeField] private Vector3 backScale = new Vector3(0.85f, 0.7f, 1f); // 가로/세로 축소

    [Header("Morph Animation")]
    [SerializeField] private float morphDuration = 0.25f;

    // 내부 상태
    private float targetHP;
    private float targetShield;
    private bool isInCover = true;
    private Coroutine morphCoroutine;

    void OnEnable()
    {
        CharacterBase.OnStatChanged += HandleStatChanged;
    }

    void OnDisable()
    {
        CharacterBase.OnStatChanged -= HandleStatChanged;
    }

    private void HandleStatChanged(CharacterBase sender)
    {
        if (sender != owner) return;
        targetHP = owner.HpRatio;
        targetShield = owner.ShieldRatio;
    }

    void Start()
    {
        // 색상 초기화
        hpBar.color = hpColor;
        shieldBar.color = shieldColor;

        // fill 초기화 (NaN 방지 — maxHp가 0이면 1로 표시)
        targetHP = owner.HpRatio;
        targetShield = owner.ShieldRatio;
        if (float.IsNaN(targetHP) || targetHP < 0f) targetHP = 1f;
        if (float.IsNaN(targetShield) || targetShield < 0f) targetShield = 1f;

        hpBar.fillAmount = targetHP;
        shieldBar.fillAmount = targetShield;

        // 레이아웃 초기화 (엄폐 상태: Shield 앞(크게), HP 뒤(작게 위에))
        ApplyState(shieldBar, frontPosition, frontScale);
        ApplyState(hpBar, backPosition, backScale);
        // 작은 바(HP)를 나중에 그려서 큰 바(Shield) 위에 보이게
        hpBar.transform.SetAsLastSibling();
    }

    void Update()
    {
        // fillAmount 부드럽게 보간
        hpBar.fillAmount = Mathf.Lerp(hpBar.fillAmount, targetHP, Time.deltaTime * fillLerpSpeed);
        shieldBar.fillAmount = Mathf.Lerp(shieldBar.fillAmount, targetShield, Time.deltaTime * fillLerpSpeed);

        // 상태 전환 감지
        bool shouldBeCover = owner.CurrentState == CharacterState.Idle
                          || owner.CurrentState == CharacterState.Reload;

        if (shouldBeCover != isInCover)
        {
            isInCover = shouldBeCover;
            StartMorph();
        }
    }

    private void StartMorph()
    {
        if (morphCoroutine != null)
            StopCoroutine(morphCoroutine);
        morphCoroutine = StartCoroutine(MorphRoutine());
    }

    IEnumerator MorphRoutine()
    {
        // 앞으로 올 바 / 뒤로 갈 바 결정
        Image frontBar = isInCover ? shieldBar : hpBar;
        Image backBar  = isInCover ? hpBar : shieldBar;

        RectTransform frontRT = frontBar.rectTransform;
        RectTransform backRT  = backBar.rectTransform;

        // 현재 상태 저장
        Vector2 frontStartPos   = frontRT.anchoredPosition;
        Vector3 frontStartScale = frontRT.localScale;
        Vector2 backStartPos    = backRT.anchoredPosition;
        Vector3 backStartScale  = backRT.localScale;

        bool swapped = false;
        float elapsed = 0f;

        while (elapsed < morphDuration)
        {
            float t = elapsed / morphDuration;
            float smooth = t * t * (3f - 2f * t); // SmoothStep

            // Front 바: 정위치 + 풀 스케일로
            frontRT.anchoredPosition = Vector2.Lerp(frontStartPos, frontPosition, smooth);
            frontRT.localScale = Vector3.Lerp(frontStartScale, frontScale, smooth);

            // Back 바: 위로 + 축소
            backRT.anchoredPosition = Vector2.Lerp(backStartPos, backPosition, smooth);
            backRT.localScale = Vector3.Lerp(backStartScale, backScale, smooth);

            // 50% 시점에 정렬 순서 교체 (작은 바가 위에 보이도록)
            if (t >= 0.5f && !swapped)
            {
                backBar.transform.SetAsLastSibling();
                swapped = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 값 확정
        frontRT.anchoredPosition = frontPosition;
        frontRT.localScale = frontScale;
        backRT.anchoredPosition = backPosition;
        backRT.localScale = backScale;

        if (!swapped)
            backBar.transform.SetAsLastSibling();

        morphCoroutine = null;
    }

    private void ApplyState(Image bar, Vector2 position, Vector3 scale)
    {
        bar.rectTransform.anchoredPosition = position;
        bar.rectTransform.localScale = scale;
    }
}