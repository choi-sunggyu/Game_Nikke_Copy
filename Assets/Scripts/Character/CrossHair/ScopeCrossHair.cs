using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScopeCrossHair : CrossHairBase
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [Header("── Scope 오브젝트 ───────────────")]
    public  GameObject scopeOverlay;
    public  GameObject crossHairImage;
    public  Image      donutImage;
    [SerializeField] private Image scopeImage;

    [Header("── Scope 크기 (레퍼런스 1080x1920 기준) ──")]
    [SerializeField] private float holeSize       = 300f;
    [SerializeField] private float scopeImageSize = 280f;
    [SerializeField] private float glowMultiplier = 1.0f;
    [SerializeField] private int   donutTexSize   = 512;
    [SerializeField] private Color glowColor      = new Color(1f, 0.6f, 0.1f, 1f);

    [Header("── Scope 차징 UI ─────────────")]
    [SerializeField] private Image    chargeProgressBar;
    [SerializeField] private TMP_Text chargePercentText;
    [SerializeField] private Image    innerGlow;

    [Header("── Scope 탄약 UI ─────────────")]
    [SerializeField] private Image    scopeAmmoBar;
    [SerializeField] private TMP_Text scopeAmmoText;

    [Header("── Idle 탄약 UI ──────────────")]
    [SerializeField] private GameObject idleAmmoUI;
    [SerializeField] private Image      idleAmmoBar;
    [SerializeField] private TMP_Text   idleAmmoText;

    // ═══════════════════════════════════════════════════════
    //  내부 상태 및 캐싱
    // ═══════════════════════════════════════════════════════
    private static readonly Color normalColor  = Color.white;
    private static readonly Color warningColor = new Color(1f, 0.25f, 0.25f);

    private CanvasGroup   canvasGroup;
    private RectTransform scopeRectTransform;
    private RectTransform _donutRect;
    private RectTransform _glowRect;

    private Coroutine _glowCoroutine;
    private bool      _glowActive = false;

    // 매 프레임 new 할당 방지용 캐싱 필드
    private Vector3 _targetPosition;
    private Vector2 _touchDelta;

    // Screen 크기 캐싱 (Start에서 1회 세팅)
    private float _screenW;
    private float _screenH;

    private bool IsOwnerReloading =>
        owner != null && owner.CurrentState == CharacterState.Reload;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    protected override void Awake()
    {
        base.Awake();

        if (!scopeOverlay.TryGetComponent(out canvasGroup))
            canvasGroup = scopeOverlay.AddComponent<CanvasGroup>();

        scopeRectTransform = scopeOverlay.GetComponent<RectTransform>();

        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();

        // 화면 크기 1회 캐싱
        _screenW = Screen.width;
        _screenH = Screen.height;

        BuildScopeUI();

        isActive = false;
        crossHairImage.SetActive(false);
        if (idleAmmoUI != null)
            idleAmmoUI.SetActive(false);
    }

    /// <summary>
    /// 모든 Scope UI 크기를 코드로 통합 관리.
    /// 레퍼런스 해상도(1080x1920) 기준 → Canvas Scaler가 스케일 처리.
    /// holeSize, scopeImageSize, glowMultiplier 세 값만 Inspector에서 조절.
    /// </summary>
    private void BuildScopeUI()
    {
        float refDiagonal = Mathf.Sqrt(1080f * 1080f + 1920f * 1920f);

        // ① ScopeOverlay — 레퍼런스 대각선 크기로 화면 전체 덮기
        scopeRectTransform.sizeDelta        = new Vector2(refDiagonal, refDiagonal);
        scopeRectTransform.anchoredPosition = Vector2.zero;

        // ② DonutImage — ScopeOverlay와 동일 크기, holeSize 기준 구멍 비율 계산
        float holeRadiusPx = donutTexSize * 0.5f * (holeSize * 0.5f / (refDiagonal * 0.5f));
        Texture2D donut = CreateDonutTexture(donutTexSize, holeRadiusPx);
        donutImage.sprite = Sprite.Create(
            donut,
            new Rect(0, 0, donutTexSize, donutTexSize),
            new Vector2(0.5f, 0.5f)
        );

        _donutRect = donutImage.GetComponent<RectTransform>();
        _donutRect.sizeDelta        = new Vector2(refDiagonal, refDiagonal);
        _donutRect.anchoredPosition = Vector2.zero;

        // ③ ScopeImage — scopeImageSize 고정 (holeSize보다 작게 유지)
        if (scopeImage != null)
        {
            RectTransform scopeRect     = scopeImage.GetComponent<RectTransform>();
            scopeRect.sizeDelta         = new Vector2(scopeImageSize, scopeImageSize);
            scopeRect.anchoredPosition  = Vector2.zero;
        }

        // ④ InnerGlow — holeSize * glowMultiplier
        if (innerGlow != null)
        {
            float glowSize = holeSize * glowMultiplier;

            _glowRect = innerGlow.GetComponent<RectTransform>();
            _glowRect.sizeDelta        = new Vector2(glowSize, glowSize);
            _glowRect.anchoredPosition = Vector2.zero;

            Texture2D glowTex = CreateRadialGlowTexture(256, glowColor);
            innerGlow.sprite = Sprite.Create(
                glowTex,
                new Rect(0, 0, 256, 256),
                new Vector2(0.5f, 0.5f)
            );

            Color c = innerGlow.color;
            c.a             = 0f;
            innerGlow.color = c;
            innerGlow.enabled = false;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  이벤트 구독
    // ═══════════════════════════════════════════════════════
    protected override void OnEnable()
    {
        base.OnEnable();
        CharacterBase.OnForcedReloadStart  += HandleReloadStart;
        CharacterBase.OnForcedReloadEnd    += HandleReloadEnd;
        CharacterBase.OnBulletCountChanged += HandleBulletCountChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CharacterBase.OnForcedReloadStart  -= HandleReloadStart;
        CharacterBase.OnForcedReloadEnd    -= HandleReloadEnd;
        CharacterBase.OnBulletCountChanged -= HandleBulletCountChanged;
    }

    // ═══════════════════════════════════════════════════════
    //  캐릭터 전환 및 이벤트 핸들링
    // ═══════════════════════════════════════════════════════
    protected override void OnSwitchCharacter(int index)
    {
        isActive = (CM.CurrentCharacter == owner);
        crossHairImage.SetActive(isActive);
        if (idleAmmoUI != null) idleAmmoUI.SetActive(isActive);

        if (!isActive)
        {
            isDragging = false;
            HideScope();
        }
        else
        {
            if (owner != null)
                UpdateAmmoUI(owner.CurrentBulletCount, owner.MaxBulletCount);
        }
    }

    void HandleBulletCountChanged(CharacterBase sender, int count)
    {
        if (owner == null || sender != owner) return;
        UpdateAmmoUI(count, owner.MaxBulletCount);
    }

    // ═══════════════════════════════════════════════════════
    //  조준 입력
    // ═══════════════════════════════════════════════════════
    protected override void OnFirePress()
    {
        if (!isActive) return;

        isDragging      = true;
        currentPosition = Input.mousePosition;
        scopeRectTransform.position = rectTransform.position;

        crossHairImage.SetActive(false);
        if (idleAmmoUI != null) idleAmmoUI.SetActive(false);
        scopeOverlay.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    protected override void OnFireRelease()
    {
        isDragging = false;
        HideScope();

        if (isActive)
        {
            crossHairImage.SetActive(true);
            if (idleAmmoUI != null) idleAmmoUI.SetActive(true);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  강제 리로드
    // ═══════════════════════════════════════════════════════
    void HandleReloadStart(CharacterBase sender)
    {
        if (!isActive) return;
        if (CM == null || sender != CM.CurrentCharacter) return;
        isDragging = false;
        HideScope();
    }

    void HandleReloadEnd(CharacterBase sender)
    {
        if (CM == null || sender != CM.CurrentCharacter) return;
        if (Input.GetMouseButton(0))
            OnFirePress();
    }

    void HideScope()
    {
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    protected override void DrawCrossHair() { }

    // ═══════════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════════
    protected override void Update()
    {
        if (!isActive) return;

        UpdateScopeVisibilityState();
        UpdateCrosshairPosition();

        if (!isDragging && canvasGroup.alpha <= 0f)
            scopeOverlay.SetActive(false);
    }

    private void UpdateScopeVisibilityState()
    {
        if (CharacterAI.IsAutoScopeMode)
        {
            if (!IsOwnerReloading)
            {
                if (!scopeOverlay.activeSelf)
                {
                    crossHairImage.SetActive(false);
                    if (idleAmmoUI != null) idleAmmoUI.SetActive(false);
                    scopeOverlay.SetActive(true);
                    canvasGroup.alpha = 1f;
                }
                scopeRectTransform.position = rectTransform.position;
            }
            else
            {
                if (scopeOverlay.activeSelf)
                {
                    HideScope();
                    crossHairImage.SetActive(true);
                    if (idleAmmoUI != null) idleAmmoUI.SetActive(true);
                }
            }
        }
        else
        {
            if (!isDragging && scopeOverlay.activeSelf && canvasGroup.alpha > 0f)
            {
                HideScope();
                crossHairImage.SetActive(true);
                if (idleAmmoUI != null) idleAmmoUI.SetActive(true);
            }
        }
    }

    private void UpdateCrosshairPosition()
    {
        if (isPCMode)
        {
            if (!CharacterAI.IsAutoScopeMode)
            {
                _targetPosition   = Input.mousePosition;
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, 0f, _screenW);
                _targetPosition.y = Mathf.Clamp(_targetPosition.y, 0f, _screenH);
                rectTransform.position = _targetPosition;
            }

            if (isDragging)
                scopeRectTransform.position = rectTransform.position;
        }
        else
        {
            if (!isDragging)
            {
                currentPosition = Input.mousePosition;
            }
            else
            {
                _touchDelta       = (Vector2)Input.mousePosition - currentPosition;
                _targetPosition   = rectTransform.position + (Vector3)_touchDelta;
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, 0f, _screenW);
                _targetPosition.y = Mathf.Clamp(_targetPosition.y, 0f, _screenH);

                rectTransform.position      = _targetPosition;
                scopeRectTransform.position = _targetPosition;
                currentPosition             = Input.mousePosition;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  UI 데이터 갱신
    // ═══════════════════════════════════════════════════════
    public void UpdateChargeUI(float chargeRatio)
    {
        if (chargeProgressBar == null) return;

        chargeProgressBar.fillAmount = chargeRatio;
        if (chargePercentText != null)
            chargePercentText.text = Mathf.RoundToInt(chargeRatio * 100f).ToString();

        if (chargeRatio >= 1f && innerGlow != null && !_glowActive)
        {
            _glowActive = true;
            if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
            _glowCoroutine = StartCoroutine(FadeInGlow());
        }
        else if (chargeRatio < 1f && innerGlow != null && _glowActive)
        {
            _glowActive = false;
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
            innerGlow.enabled = false;

            Color c = innerGlow.color;
            c.a             = 0f;
            innerGlow.color = c;
        }
    }

    IEnumerator FadeInGlow()
    {
        innerGlow.enabled = true;
        float elapsed  = 0f;
        float duration = 0.4f; // ← 페이드인 시간 조절
        Color c        = innerGlow.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a      = Mathf.Lerp(0f, 1f, elapsed / duration);
            innerGlow.color = c;
            yield return null;
        }

        c.a             = 1f;
        innerGlow.color = c;
        _glowCoroutine  = null;
    }

    public void UpdateAmmoUI(int current, int max)
    {
        float ratio       = (float)current / max;
        bool  isLow       = ratio <= 0.5f;
        Color targetColor = isLow ? warningColor : normalColor;
        string ammoStr    = current.ToString("D3");

        if (scopeAmmoBar  != null) { scopeAmmoBar.fillAmount  = ratio;   scopeAmmoBar.color  = targetColor; }
        if (scopeAmmoText != null) { scopeAmmoText.text        = ammoStr; scopeAmmoText.color = targetColor; }
        if (idleAmmoBar   != null) { idleAmmoBar.fillAmount   = ratio;   idleAmmoBar.color   = targetColor; }
        if (idleAmmoText  != null) { idleAmmoText.text         = ammoStr; idleAmmoText.color  = targetColor; }
    }

    // ═══════════════════════════════════════════════════════
    //  텍스처 절차적 생성 (GC 최적화 완료)
    // ═══════════════════════════════════════════════════════
    private Texture2D CreateDonutTexture(int size, float holeRadius)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        float   cx          = size * 0.5f;
        float   cy          = size * 0.5f;
        float   radiusSqr   = holeRadius * holeRadius; // sqrt 제거용 제곱값 캐싱
        Color32 clearColor  = new Color32(0, 0, 0, 0);
        Color32 bgColor     = new Color32(0, 0, 0, 230); // alpha 0.9f ≈ 230

        for (int y = 0; y < size; y++)
        {
            float dy        = y - cy;
            float dySqr     = dy * dy;
            int   rowOffset = y * size;

            for (int x = 0; x < size; x++)
            {
                float dx      = x - cx;
                float distSqr = dx * dx + dySqr;
                pixels[rowOffset + x] = distSqr < radiusSqr ? clearColor : bgColor;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private Texture2D CreateRadialGlowTexture(int size, Color baseColor)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        float cx          = size * 0.5f;
        float cy          = size * 0.5f;
        float maxRadius   = size * 0.5f; // 이 크기가 곧 innerGlow의 UI 크기(가장자리 변두리)가 됩니다.
        float maxRadiusSqr = maxRadius * maxRadius;

        // 빛이 안쪽으로 스며들 투명도 두께 비율 (0.1f = 가장자리에서 안쪽으로 10%만큼만 빛남)
        // 이 값을 조절하여 불빛이 들어오는 두께를 제어할 수 있습니다.
        float glowThicknessRatio = 0.15f; 

        byte r = (byte)Mathf.Clamp(baseColor.r * 255f, 0f, 255f);
        byte g = (byte)Mathf.Clamp(baseColor.g * 255f, 0f, 255f);
        byte b = (byte)Mathf.Clamp(baseColor.b * 255f, 0f, 255f);
        Color32 clearColor = new Color32(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            float dy        = y - cy;
            float dySqr     = dy * dy;
            int   rowOffset = y * size;

            for (int x = 0; x < size; x++)
            {
                float dx      = x - cx;
                float distSqr = dx * dx + dySqr;
                int   index   = rowOffset + x;

                // 1. 최외곽 원을 벗어나면 완전히 투명 처리
                if (distSqr > maxRadiusSqr)
                {
                    pixels[index] = clearColor;
                    continue;
                }

                float dist = Mathf.Sqrt(distSqr);
                float normDist = dist / maxRadius; // 0(중심) ~ 1(가장자리)

                // 2. 가장자리 경계선(1.0)에 가까울수록 진하고, 안쪽으로 들어올수록 흐려지게 만듦
                if (normDist >= (1.0f - glowThicknessRatio))
                {
                    // 안쪽 경계선(0.85)에서 외곽(1.0)까지 0 ~ 1로 보간
                    float alphaRatio = (normDist - (1.0f - glowThicknessRatio)) / glowThicknessRatio;
                    
                    // 자연스러운 스며듦을 위해 부드러운 보간(SmoothStep) 적용 가능
                    alphaRatio = Mathf.SmoothStep(0f, 1f, alphaRatio);

                    byte alpha = (byte)Mathf.RoundToInt(alphaRatio * baseColor.a * 255f);
                    pixels[index] = new Color32(r, g, b, alpha);
                }
                else
                {
                    // 지정한 두께보다 안쪽 영역은 불빛이 들어오지 않도록 투명 처리
                    pixels[index] = clearColor;
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}