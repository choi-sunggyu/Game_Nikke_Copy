using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEngine;

public class SniperScopeUI : MonoBehaviour
{
    [Header("조준경 UI 요소")]
    public GameObject scopeOverlay; // 조준경 전체 패널
    private CanvasGroup canvasGroup; //뭔데 이거
    private RectTransform scopeRectTransform;
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
    }

    void OnEnable()
    {
        InputManager.OnFirePress   += ShowScope;
        InputManager.OnFireRelease += HideScope;
        InputManager.OnSwitchCharacter += OnCharacterSwitch;
        CharacterBase.OnReloadStart   += HandleReloadStart;
        CharacterBase.OnReloadEnd     += HandleReloadEnd;
    }

    void OnDisable()
    {
        InputManager.OnFirePress   -= ShowScope;
        InputManager.OnFireRelease -= HideScope;
        InputManager.OnSwitchCharacter -= OnCharacterSwitch;
        CharacterBase.OnReloadStart -= HandleReloadStart;
        CharacterBase.OnReloadEnd -= HandleReloadEnd;
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
