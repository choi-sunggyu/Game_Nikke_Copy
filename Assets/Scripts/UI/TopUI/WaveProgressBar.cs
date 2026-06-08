using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveProgressBar : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [Header("── 바 오브젝트 ───────────────────")]
    [SerializeField] private Image     fillBar;
    [SerializeField] private Image     bgBar;
    [SerializeField] private Image     outline;
    [SerializeField] private Image     diamondHead;
    [SerializeField] private Image     eliteMark;

    [Header("── 색상 설정 ───────────────────")]
    [SerializeField] private Color normalDiamondColor  = new Color(0.2f, 0.6f, 1.0f, 1f); // 파란색
    [SerializeField] private Color blockedDiamondColor = new Color(1.0f, 0.2f, 0.2f, 1f); // 빨간색
    [SerializeField] private Color fillBarColor        = new Color(0.2f, 0.6f, 1.0f, 1f);

    [Header("── 애니메이션 설정 ─────────────")]
    [SerializeField] private float blinkSpeed         = 3f;   // 반짝임 속도
    [SerializeField] private float blinkMinAlpha      = 0.4f; // 반짝임 최소 알파
    [SerializeField] private float progressSmoothSpeed = 0.3f;  // 프로그레스 바 부드러운 이동 속도

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private WaveManager _waveManager;
    private RectTransform _fillBarRect;
    private RectTransform _diamondHeadRect;
    private float _barWidth;
    private float _currentFill;       // 현재 표시 중인 fillAmount (스무딩용)
    private bool  _isBlocked;
    private bool  _isElitePhase;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    void Awake()
    {
        _fillBarRect     = fillBar.GetComponent<RectTransform>();
        _diamondHeadRect = diamondHead.GetComponent<RectTransform>();
        _barWidth        = bgBar.GetComponent<RectTransform>().rect.width;

        // 초기 상태
        fillBar.fillAmount   = 0f;
        fillBar.color        = fillBarColor;
        outline.gameObject.SetActive(false);
        eliteMark.gameObject.SetActive(true);

        SetDiamondColor(normalDiamondColor);
    }

    void OnEnable()
    {
        WaveManager.OnElitePhaseStart += HandleElitePhaseStart;
        WaveManager.OnEliteDefeated   += HandleEliteDefeated;
    }

    void OnDisable()
    {
        WaveManager.OnElitePhaseStart -= HandleElitePhaseStart;
        WaveManager.OnEliteDefeated   -= HandleEliteDefeated;
    }

    void Start()
    {
        _waveManager = FindAnyObjectByType<WaveManager>();
    }

    // ═══════════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════════
    void Update()
    {
        if (_waveManager == null || _isElitePhase) return;

        UpdateProgress();
        UpdateBlockState();
        UpdateDiamondBlink();
        UpdateDiamondPosition();
    }

    // ═══════════════════════════════════════════════════════
    //  프로그레스 갱신
    // ═══════════════════════════════════════════════════════
    private void UpdateProgress()
    {
        float targetFill = _waveManager.WaveProgress;

        if (_waveManager.IsWaveBlocked)
        {
            // 블록 시 현재 위치 유지
            return;
        }

        // 목표값을 향해 천천히 이동
        _currentFill = Mathf.MoveTowards(
            _currentFill,
            targetFill,
            progressSmoothSpeed * Time.deltaTime
        );

        fillBar.fillAmount = _currentFill;
    }

    // ═══════════════════════════════════════════════════════
    //  블록 상태 처리
    // ═══════════════════════════════════════════════════════
    private void UpdateBlockState()
    {
        bool nowBlocked = _waveManager.IsWaveBlocked;

        if (nowBlocked == _isBlocked) return; // 상태 변화 없으면 스킵

        _isBlocked = nowBlocked;

        outline.gameObject.SetActive(_isBlocked);
        SetDiamondColor(_isBlocked ? blockedDiamondColor : normalDiamondColor);
    }

    // ═══════════════════════════════════════════════════════
    //  다이아몬드 반짝임
    // ═══════════════════════════════════════════════════════
    private void UpdateDiamondBlink()
    {
        // sin 파형으로 알파 진동 (blinkMinAlpha ~ 1.0)
        float alpha = Mathf.Lerp(
            blinkMinAlpha,
            1.0f,
            (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f
        );

        Color c = diamondHead.color;
        c.a = alpha;
        diamondHead.color = c;
    }

    // ═══════════════════════════════════════════════════════
    //  다이아몬드 위치 — Fill_Bar 끝에 맞추기
    // ═══════════════════════════════════════════════════════
    private void UpdateDiamondPosition()
    {
        // BG_Bar 왼쪽 끝 기준으로 fillAmount 비율만큼 X 이동
        float xPos = (_currentFill * _barWidth) - (_barWidth * 0.5f);
        _diamondHeadRect.anchoredPosition = new Vector2(xPos, 0f);
    }

    // ═══════════════════════════════════════════════════════
    //  색상 헬퍼
    // ═══════════════════════════════════════════════════════
    private void SetDiamondColor(Color color)
    {
        // 알파는 반짝임으로 제어하므로 RGB만 변경
        color.a           = diamondHead.color.a;
        diamondHead.color = color;
    }

    // ═══════════════════════════════════════════════════════
    //  엘리트 페이즈 이벤트
    // ═══════════════════════════════════════════════════════
    private void HandleElitePhaseStart()
    {
        _isElitePhase = true;

        // 바 100% 고정
        fillBar.fillAmount = 1f;
        _currentFill       = 1f;

        outline.gameObject.SetActive(false);

        // 다이아몬드 헤드 숨김 (엘리트 마크로 대체)
        diamondHead.gameObject.SetActive(false);

        // TopUI 자체를 EliteWarningUI로 전환은 TopUIManager에서 처리
    }

    private void HandleEliteDefeated()
    {
        // MissionClearUI 전환은 TopUIManager에서 처리
        gameObject.SetActive(false);
    }
}