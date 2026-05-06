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

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = scopeOverlay.GetComponent<CanvasGroup>();
        if(canvasGroup == null)
            canvasGroup = scopeOverlay.AddComponent<CanvasGroup>();

        scopeRectTransform = scopeOverlay.GetComponent<RectTransform>();
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);

        Texture2D donut = CreateDonutTexture(3072, 200f);
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
        isActive = (index == 2);
        crossHairImage.SetActive(isActive);  // CrossHair는 항상 표시
        if(bulletCountText != null)
            bulletCountText.gameObject.SetActive(isActive);
        if(!isActive)
        {
            isDragging = false;
            HideScope();
        }
    }

    protected override void OnFirePress()
    {
        if(!isActive || isReloading) return;
        if(isReloading) return;

        isDragging = true;
        currentPosition = Input.mousePosition;
        crossHairImage.SetActive(false);
        scopeOverlay.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    protected override void OnFireRelease()
    {
        isDragging = false;
        HideScope();
        if(isActive)
            crossHairImage.SetActive(true);  // CrossHair 다시 표시
    }

    void HandleReloadStart()
    {
        if(!isActive) return;
        isReloading = true;
        isDragging = false;
        HideScope();
    }

    void HandleReloadEnd() { isReloading = false; }

    void HideScope()
    {
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    protected override void DrawCrossHair() { }

    protected override void Update()
    {
        // 미터치 or 강제 리로딩 중 → CrossHair 드래그
        if(!isDragging && isActive)
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

        // 조준 중 → 조준경 + CrossHair 같이 이동
        if(isDragging && isActive)
        {
            Vector2 touchDelta = (Vector2)Input.mousePosition - currentPosition;

            Vector3 crossHairPos = rectTransform.position + (Vector3)touchDelta;
            crossHairPos.x = Mathf.Clamp(crossHairPos.x, 0f, Screen.width);
            crossHairPos.y = Mathf.Clamp(crossHairPos.y, 0f, Screen.height);
            rectTransform.position = crossHairPos;

            Vector3 scopePos = scopeRectTransform.position + (Vector3)touchDelta;
            scopePos.x = Mathf.Clamp(scopePos.x, 0f, Screen.width);
            scopePos.y = Mathf.Clamp(scopePos.y, 0f, Screen.height);
            scopeRectTransform.position = scopePos;

            currentPosition = Input.mousePosition;
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