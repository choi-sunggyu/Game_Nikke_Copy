using UnityEngine;
using UnityEngine.UI;

public class ScopeCrossHair : CrossHairBase
{
    [Header("Scope 전용")]
    public GameObject scopeOverlay;
    private CanvasGroup canvasGroup;
    private RectTransform scopeRectTransform;
    public Image donutImage;
    private bool isReloading = false;

    protected override void Awake()
    {
        base.Awake();  // CrossHairBase.Awake() 호출

        canvasGroup = scopeOverlay.GetComponent<CanvasGroup>();
        if(canvasGroup == null)
            canvasGroup = scopeOverlay.AddComponent<CanvasGroup>();

        scopeRectTransform = scopeOverlay.GetComponent<RectTransform>();
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);

        Texture2D donut = CreateDonutTexture(3072, 200f);
        donutImage.sprite = Sprite.Create(
            donut,
            new Rect(0, 0, 3072, 3072),
            new Vector2(0.5f, 0.5f)
        );
    }

    protected override void OnEnable()
    {
        base.OnEnable();  // CrossHairBase 이벤트 구독
        CharacterBase.OnForcedReloadStart += HandleReloadStart;
        CharacterBase.OnForcedReloadEnd   += HandleReloadEnd;
    }

    protected override void OnDisable()
    {
        base.OnDisable();  // CrossHairBase 이벤트 해제
        CharacterBase.OnForcedReloadStart -= HandleReloadStart;
        CharacterBase.OnForcedReloadEnd   -= HandleReloadEnd;
    }

    protected override void OnSwitchCharacter(int index)
    {
        isActive = (index == 2);  // Viper
        if(!isActive)
        {
            isDragging = false;
            HideScope();
        }
    }

    protected override void OnFirePress()
    {
        if(!isActive || isReloading) return;
        isDragging = true;
        scopeOverlay.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    protected override void OnFireRelease()
    {
        isDragging = false;
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
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

    protected override void DrawCrossHair() { }  // Scope는 donut으로 대체

    protected override void Update()
    {
        if(isDragging && isActive)
            scopeRectTransform.position = Input.mousePosition;

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