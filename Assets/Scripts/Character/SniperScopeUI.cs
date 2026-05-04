using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SniperScopeUI : MonoBehaviour
{
    [Header("조준경 UI 요소")]
    public GameObject scopeOverlay; // 조준경 전체 패널
    private CanvasGroup canvasGroup; //뭔데 이거
    private RectTransform scopeRectTransform;
    public UnityEngine.UI.Image donutImage;
    private bool isAiming = false;
    private bool isViperActive = false;
    private bool isReloading = false;

    void Awake()
    {
        //전체 패널에 CanvasGroup 컴포넌트가 없으면 추가
        canvasGroup = scopeOverlay.GetComponent<CanvasGroup>(); 
        if (canvasGroup == null)
            canvasGroup = scopeOverlay.AddComponent<CanvasGroup>();

        scopeRectTransform = scopeOverlay.GetComponent<RectTransform>();

        // 시작 시 숨김
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);

        Texture2D donut = CreateDonutTexture(3072, 200f); // 3072x3072 텍스처, 중앙에 반지름 200의 구멍
        Sprite donutSprite = Sprite.Create(
            donut,
            new Rect(0, 0, 3072, 3072),
            new Vector2(0.5f, 0.5f)
        );

        donutImage.sprite = donutSprite;
    }

    void OnEnable()
    {
        InputManager.OnFirePress   += ShowScope;
        InputManager.OnFireRelease += HideScope;
        InputManager.OnSwitchCharacter += OnCharacterSwitch;
        CharacterBase.OnForcedReloadStart    += HandleReloadStart;
        CharacterBase.OnForcedReloadEnd     += HandleReloadEnd;
    }

    void OnDisable()
    {
        InputManager.OnFirePress   -= ShowScope;
        InputManager.OnFireRelease -= HideScope;
        InputManager.OnSwitchCharacter -= OnCharacterSwitch;
        CharacterBase.OnForcedReloadStart -= HandleReloadStart;
        CharacterBase.OnForcedReloadEnd -= HandleReloadEnd;
    }

    void OnCharacterSwitch(int index)
    {
        isViperActive = (index == 2); // Viper는 index 2

        // 바이퍼가 아닌 캐릭터로 전환 시 조준경 즉시 숨김
        if (!isViperActive)
        {
            isAiming = false;
            HideScope();
        }
    }

    // 리로딩 시작 시 조준경 숨김
    void HandleReloadStart()
    { 
        if (!isViperActive) return;
        isReloading = true;
        isAiming = false;
        canvasGroup.alpha = 0f;
        scopeOverlay.SetActive(false);
    }

    void HandleReloadEnd()
    {
        isReloading = false;
    }

    void ShowScope()
    {
        if (!isViperActive) return; // ← Viper 아니면 무시
        if (isReloading) return;
        isAiming = true;
        scopeOverlay.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    void HideScope()
    {
        isAiming = false;
        canvasGroup.alpha = 0f;
    }

    Texture2D CreateDonutTexture(int texSize, float holeRadius)
    {
        Texture2D tex = new Texture2D(texSize, texSize);
        Vector2 center = new Vector2(texSize / 2f, texSize / 2f);

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                // 원 안 = 투명, 원 밖 = 검은색
                if (dist < holeRadius)
                    tex.SetPixel(x, y, Color.clear);
                else
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0.9f));
            }
        }

        tex.Apply();
        return tex;
    }

    void Update()
    {
        // 마우스 추적
        if (isAiming && isViperActive)
        {
            scopeRectTransform.position = Input.mousePosition;
        }

        // 완전히 사라지면 비활성화
        if (!isAiming && canvasGroup.alpha <= 0f)
            scopeOverlay.SetActive(false);
    }

    void Start()
    {
        isViperActive = false;
    }

}
