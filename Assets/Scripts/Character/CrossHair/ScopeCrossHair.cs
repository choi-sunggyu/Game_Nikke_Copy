using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScopeCrossHair : CrossHairBase
{
    [Header("Scope 전용")]
    public GameObject scopeOverlay;
    public GameObject crossHairImage;  // 미터치 시 표시할 CrossHair
    private CanvasGroup canvasGroup;
    private RectTransform scopeRectTransform;
    public Image donutImage;
    [SerializeField] private TextMeshProUGUI bulletText;
    private bool isReloading = false;
    [Header("BulletCount 위치")]
    [SerializeField] private Vector2 bulletCountIdlePos;  // 미클릭 시 위치
    [SerializeField] private Vector2 bulletCountAimPos;   // 클릭(조준) 시 위치
    private RectTransform bulletCountRect;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = scopeOverlay.GetComponent<CanvasGroup>();
        if(canvasGroup == null)
            canvasGroup = scopeOverlay.AddComponent<CanvasGroup>();

        scopeRectTransform = scopeOverlay.GetComponent<RectTransform>();
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);

        Texture2D donut = CreateDonutTexture(3072, 150f);
        donutImage.sprite = Sprite.Create(
            donut, new Rect(0, 0, 3072, 3072), new Vector2(0.5f, 0.5f)
        );
    }

    protected override void Start()
    {
        base.Start();
        isActive = false;
        crossHairImage.SetActive(false);
        bulletCountText = bulletText;
        bulletCountRect = bulletCountText.GetComponent<RectTransform>();
        if(bulletCountText != null)
            bulletCountText.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CharacterBase.OnForcedReloadStart += HandleReloadStart;
        CharacterBase.OnForcedReloadEnd   += HandleReloadEnd;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CharacterBase.OnForcedReloadStart -= HandleReloadStart;
        CharacterBase.OnForcedReloadEnd   -= HandleReloadEnd;
    }

    protected override void OnSwitchCharacter(int index)
    {
        isActive = (CM.CurrentCharacter == owner);
        crossHairImage.SetActive(isActive);
        if(bulletCountText != null)
        {
            bulletCountText.gameObject.SetActive(isActive);
            if(isActive)
                bulletCountRect.anchoredPosition = bulletCountIdlePos;
        }
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

    private void RefreshBulletCount()
    {
        if(CM != null && CM.CurrentCharacter != null && bulletCountText != null)
            bulletCountText.text = CM.CurrentCharacter.CurrentBulletCount.ToString();
    }

    protected override void OnFirePress()
    {
        if(!isActive || isReloading) return;

        isDragging = true;
        currentPosition = Input.mousePosition;

        // 스코프를 현재 크로스헤어 위치에 표시
        scopeRectTransform.position = rectTransform.position;

        crossHairImage.SetActive(false);
        scopeOverlay.SetActive(true);
        canvasGroup.alpha = 1f;

        // 조준 시 위치로 이동
        bulletCountRect.anchoredPosition = bulletCountAimPos;
    }

    protected override void OnFireRelease()
    {
        isDragging = false;
        HideScope();
        if(isActive)
        {
            crossHairImage.SetActive(true);  // CrossHair 다시 표시
            // 미클릭 위치로 복귀
            bulletCountRect.anchoredPosition = bulletCountIdlePos;
        }
    }

    void HandleReloadStart(CharacterBase sender)
    {
        if(!isActive) return;
        if (CM == null || sender != CM.CurrentCharacter) return;
        isReloading = true;
        isDragging = false;
        HideScope();
    }

    void HandleReloadEnd(CharacterBase sender) 
    { 
        // 조준 상태에서 강제 리로드 → 조준 유지한 채로 리로드 완료 → 다시 스코프 표시
        if (CM == null || sender != CM.CurrentCharacter) return;
        isReloading = false;
        if (Input.GetMouseButton(0))
            OnFirePress();
    }

    void HideScope()
    {
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    protected override void DrawCrossHair() { }

    protected override void Update()
    {
        // AutoScopeMode 전용 스코프 표시 처리
        if (CharacterAI.IsAutoScopeMode && isActive && !isReloading)
        {
            // 스코프가 꺼져 있으면 켜기
            if (!scopeOverlay.activeSelf)
            {
                crossHairImage.SetActive(false);
                scopeOverlay.SetActive(true);
                canvasGroup.alpha = 1f;
                bulletCountRect.anchoredPosition = bulletCountAimPos;
            }
            // 스코프가 crosshair 위치를 따라가도록
            scopeRectTransform.position = rectTransform.position;
        }
        else if (!CharacterAI.IsAutoScopeMode && isActive && !isDragging)
        {
            // AutoScopeMode 해제 시 스코프 닫기
            if (scopeOverlay.activeSelf && canvasGroup.alpha > 0f)
            {
                HideScope();
                crossHairImage.SetActive(true);
                bulletCountRect.anchoredPosition = bulletCountIdlePos;
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