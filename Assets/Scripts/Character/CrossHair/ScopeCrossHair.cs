using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScopeCrossHair : CrossHairBase
{
    [Header("── Scope 차징 UI ─────────────")]
    [SerializeField] private Image     chargeProgressBar;
    [SerializeField] private TMP_Text  chargePercentText;
    [SerializeField] private Image     innerGlow; // 차징 완료 빛

    [Header("── Scope 탄약 UI ─────────────")]
    [SerializeField] private Image     scopeAmmoBar;
    [SerializeField] private TMP_Text  scopeAmmoText;

    [Header("── Idle 탄약 UI ──────────────")]
    [SerializeField] private GameObject idleAmmoUI;
    [SerializeField] private Image      idleAmmoBar; // 가로 바
    [SerializeField] private TMP_Text   idleAmmoText;

    private static readonly Color normalColor = Color.white;
    private static readonly Color warningColor = new Color(1f, 0.25f, 0.25f); // 빨간

    [Header("Scope 전용")]
    public GameObject scopeOverlay; // 스코프 UI 전체 오브젝트 (도넛 + 차징 + 탄약 등)
    public GameObject crossHairImage;  // 미터치 시 표시할 CrossHair
    private CanvasGroup canvasGroup; // 스코프 페이드 효과용 CanvasGroup
    private RectTransform scopeRectTransform;
    public Image donutImage;
    private Coroutine _glowCoroutine;
    private bool isReloading = false;
    [Header("BulletCount 위치")]
    [SerializeField] private Vector2 bulletCountIdlePos;  // 미클릭 시 위치
    [SerializeField] private Vector2 bulletCountAimPos;   // 클릭(조준) 시 위치
    private RectTransform bulletCountRect;
    private bool _glowActive = false;

    private bool IsOwnerReloading => owner != null && 
                                  owner.CurrentState == CharacterState.Reload;

    protected override void Awake()
    {
        base.Awake();

        canvasGroup = scopeOverlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = scopeOverlay.AddComponent<CanvasGroup>();

        scopeRectTransform = scopeOverlay.GetComponent<RectTransform>();

        float diagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);

        // ScopeOverlay는 화면 전체를 덮어야 하므로 diagonal * 2f 유지
        scopeRectTransform.sizeDelta = new Vector2(diagonal * 2f, diagonal * 2f);

        // ▼ 구멍 크기 조절 → holeRadius 값을 올리면 구멍이 커짐
        float texSize    = 3072f;
        float holeRadius = 80f; // ← 이 값을 올릴수록 도넛 구멍이 커짐 (기본 66f → 100f)

        Texture2D donut = CreateDonutTexture((int)texSize, holeRadius);
        donutImage.sprite = Sprite.Create(
            donut, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f)
        );

        // 구멍 실제 픽셀 크기를 화면 크기 기준으로 환산
        // ▼ holeRadius 바꿨으면 여기도 자동으로 맞춰짐 (건드릴 필요 없음)
        float scopeSize = diagonal * 2f * (holeRadius / texSize) * 2f;

        // InnerGlow 크기를 구멍에 맞춤 (원형 텍스처 사용)
        if (innerGlow != null)
        {
            RectTransform glowRect = innerGlow.GetComponent<RectTransform>();
            float glowScale = 2.0f;
            glowRect.sizeDelta = new Vector2(scopeSize * glowScale, scopeSize * glowScale);

            // 원형 방사형 텍스처 생성 (원 밖은 완전 투명)
            Texture2D glowTex = CreateRadialGlowTexture(512);
            innerGlow.sprite = Sprite.Create(
                glowTex, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f)
            );

            // 시작 시 alpha 0, 비활성
            Color c = innerGlow.color;
            c.a = 0f;
            innerGlow.color = c;
            innerGlow.enabled = false;
        }

        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        isActive = false;
        crossHairImage.SetActive(false);

        if (idleAmmoUI != null)
            idleAmmoUI.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CharacterBase.OnForcedReloadStart += HandleReloadStart;
        CharacterBase.OnForcedReloadEnd   += HandleReloadEnd;
        CharacterBase.OnBulletCountChanged += HandleBulletCountChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CharacterBase.OnForcedReloadStart -= HandleReloadStart;
        CharacterBase.OnForcedReloadEnd   -= HandleReloadEnd;
        CharacterBase.OnBulletCountChanged -= HandleBulletCountChanged;
    }

    // [캐릭터 전환]

    protected override void OnSwitchCharacter(int index)
    {
        isActive = (CM.CurrentCharacter == owner);
        crossHairImage.SetActive(isActive);

        if (idleAmmoUI != null)
            idleAmmoUI.SetActive(isActive);

        if(!isActive)
        {
            isDragging = false;
            HideScope();
        }
        else
        {
            RefreshBulletCount();
        }
    }

    // [탄약 변경]
    private void RefreshBulletCount()
    {
        if(CM != null && CM.CurrentCharacter != null && bulletCountText != null)
            bulletCountText.text = CM.CurrentCharacter.CurrentBulletCount.ToString();
    }

    void HandleBulletCountChanged(CharacterBase sender, int count)
    {
        if (owner == null || sender != owner) return;
        UpdateAmmoUI(count, owner.MaxBulletCount);
    }

    // [조준/미조준] - 클릭 시 스코프 표시, 기존 크로스헤어 숨김
    protected override void OnFirePress()
    {
        if(!isActive || isReloading) return;

        isDragging = true;
        currentPosition = Input.mousePosition;

        // 스코프를 현재 크로스헤어 위치에 표시
        scopeRectTransform.position = rectTransform.position;

        // 기존 크로스헤어 숨기고 스코프 표시
        crossHairImage.SetActive(false);
        idleAmmoUI.SetActive(false);
        scopeOverlay.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    // [조준/미조준] - 클릭 해제 시 스코프 숨김, 기존 크로스헤어 표시
    protected override void OnFireRelease()
    {
        isDragging = false;
        HideScope();
        if(isActive)
        {
            crossHairImage.SetActive(true);  // CrossHair 다시 표시
            idleAmmoUI.SetActive(true);
        }
    }

    // [리로드] - 강제 리로드 시작 → 조준 해제 + 스코프 숨김
    void HandleReloadStart(CharacterBase sender)
    {
        if(!isActive) return;
        if (CM == null || sender != CM.CurrentCharacter) return;
        isReloading = true;
        isDragging = false;
        HideScope();
    }

    // [리로드] - 강제 리로드 완료 → 조준 유지한 채로 리로드 완료 → 다시 스코프 표시
    void HandleReloadEnd(CharacterBase sender) 
    { 
        // 조준 상태에서 강제 리로드 → 조준 유지한 채로 리로드 완료 → 다시 스코프 표시
        if (CM == null || sender != CM.CurrentCharacter) return;
        isReloading = false;
        if (Input.GetMouseButton(0))
            OnFirePress();
    }

    // [스코프UI 숨김]
    void HideScope()
    {
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    // [드로잉] - Scope는 별도의 UI로 처리하므로 기본 드로잉 로직은 비활성화
    protected override void DrawCrossHair() { }

    protected override void Update()
    {
        if (CharacterAI.IsAutoScopeMode && isActive && !IsOwnerReloading)
        {
            if (!scopeOverlay.activeSelf)
            {
                crossHairImage.SetActive(false);
                idleAmmoUI?.SetActive(false);
                scopeOverlay.SetActive(true);
                canvasGroup.alpha = 1f;
            }
            scopeRectTransform.position = rectTransform.position;
        }
        else if (CharacterAI.IsAutoScopeMode && isActive && IsOwnerReloading)
        {
            // 자동사격 모드 + 리로딩 중 → 스코프 닫고 일반 크로스헤어
            if (scopeOverlay.activeSelf)
            {
                HideScope();
                crossHairImage.SetActive(true);
                idleAmmoUI?.SetActive(true);
            }
        }
        else if (!CharacterAI.IsAutoScopeMode && isActive && !isDragging)
        {
            if (scopeOverlay.activeSelf && canvasGroup.alpha > 0f)
            {
                HideScope();
                crossHairImage.SetActive(true);
                idleAmmoUI?.SetActive(true);
            }
        }

        if(isPCMode && isActive)
        {
            // PC: 클릭 여부 상관없이 항상 마우스를 따라다님
            // AutoScopeMode 중에는 CharacterAI가 위치를 제어하므로 마우스 추적 비활성
            if (!CharacterAI.IsAutoScopeMode)
            {
                Vector3 newPos = Input.mousePosition;
                newPos.x = Mathf.Clamp(newPos.x, 0f, Screen.width);
                newPos.y = Mathf.Clamp(newPos.y, 0f, Screen.height);
                rectTransform.position = newPos;
            }

            // 클릭 중이면 스코프도 따라감
            if(isDragging)
                scopeRectTransform.position = rectTransform.position;
        }
        else if(!isPCMode && isActive)
        {
            // 모바일: 미터치 or 강제 리로딩 중 → CrossHair 드래그
            if(!isDragging)
            {
                if(Input.GetMouseButton(0) && isReloading)
                {
                    Vector2 delta = (Vector2)Input.mousePosition - currentPosition;
                    Vector3 newPos = rectTransform.position + (Vector3)delta;
                    newPos.x = Mathf.Clamp(newPos.x, 0f, Screen.width);
                    newPos.y = Mathf.Clamp(newPos.y, 0f, Screen.height);
                    rectTransform.position = newPos;
                }
                currentPosition = Input.mousePosition;
            }

            // 모바일: 조준 중 → 조준경 + CrossHair 같이 이동
            if(isDragging)
            {
                Vector2 touchDelta = (Vector2)Input.mousePosition - currentPosition;
                Vector3 newPos = rectTransform.position + (Vector3)touchDelta;
                newPos.x = Mathf.Clamp(newPos.x, 0f, Screen.width);
                newPos.y = Mathf.Clamp(newPos.y, 0f, Screen.height);
                rectTransform.position = newPos;

                scopeRectTransform.position = rectTransform.position;
                currentPosition = Input.mousePosition;
            }
        }

        if(!isDragging && canvasGroup.alpha <= 0f)
            scopeOverlay.SetActive(false);
    }

    // [차징 UI 업데이트]
    public void UpdateChargeUI(float chargeRatio)
    {
        if (chargeProgressBar == null) return;

        chargeProgressBar.fillAmount = chargeRatio;
        chargePercentText.text = Mathf.RoundToInt(chargeRatio * 100f).ToString();

        if (chargeRatio >= 1f && innerGlow != null && !_glowActive)
        {
            // 완료 시 단 한 번만 실행
            _glowActive = true;
            if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
            _glowCoroutine = StartCoroutine(FadeInGlow());
        }
        else if (chargeRatio < 1f && innerGlow != null && _glowActive)
        {
            // 차징 해제 시 즉시 초기화
            _glowActive = false;
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
            innerGlow.enabled = false;

            Color c = innerGlow.color;
            c.a = 0f;
            innerGlow.color = c;
        }
    }

    IEnumerator FadeInGlow()
    {
        innerGlow.enabled = true;
        float elapsed  = 0f;
        float duration = 0.4f; // ← 페이드인 시간 조절
        Color c = innerGlow.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            innerGlow.color = c;
            yield return null;
        }

        c.a = 1f;
        innerGlow.color = c;
        _glowCoroutine = null;
    }

    // [조준/미조준] - 조준 해제 시 스코프 숨김, 기존 크로스헤어 표시
    Texture2D CreateRadialGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                // 원 밖은 완전 투명 → 사각형 안 보임
                if (dist > maxDist)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // 중앙 = alpha 0, 외곽 = alpha 1 (주황빛)
                float ratio = Mathf.Clamp01(dist / maxDist);
                tex.SetPixel(x, y, new Color(1f, 0.6f, 0.1f, ratio));
            }
        }
        tex.Apply();
        return tex;
    }

    // [탄약 UI 업데이트]
    public void UpdateAmmoUI(int current, int max)
    {
        float ratio = (float)current / max;
        bool isLow  = ratio <= 0.5f;

        // Scope 탄약 바
        if (scopeAmmoBar != null)
        {
            scopeAmmoBar.fillAmount = ratio;
            scopeAmmoBar.color      = isLow ? warningColor : normalColor;
        }
        if (scopeAmmoText != null)
        {
            scopeAmmoText.text  = current.ToString("D3");
            scopeAmmoText.color = isLow ? warningColor : normalColor;
        }

        // Idle 탄약 바
        if (idleAmmoBar != null)
        {
            idleAmmoBar.fillAmount = ratio;
            idleAmmoBar.color      = isLow ? warningColor : normalColor;
        }
        if (idleAmmoText != null)
        {
            idleAmmoText.text  = current.ToString("D3"); // 3자리로 표시
            idleAmmoText.color = isLow ? warningColor : normalColor;
        }
    }

    // [도넛 텍스처 생성] - 스코프 배경용 도넛 모양 텍스처 생성
    Texture2D CreateDonutTexture(int texSize, float holeRadius)
    {
        Texture2D tex = new Texture2D(texSize, texSize);
        Vector2 center = new Vector2(texSize / 2f, texSize / 2f);
        for(int x = 0; x < texSize; x++)
            for(int y = 0; y < texSize; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist < holeRadius ? Color.clear : new Color(0,0,0,0.9f));
            }
        tex.Apply();
        return tex;
    }
}