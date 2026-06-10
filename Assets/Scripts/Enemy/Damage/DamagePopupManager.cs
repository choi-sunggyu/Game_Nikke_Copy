using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private ObjectPool popupPool;
    [SerializeField] private Canvas     targetCanvas;
    private RectTransform _canvasRect;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (targetCanvas == null)
        {
            Debug.LogError("[DamagePopupManager] targetCanvas가 연결되지 않았습니다.");
            return;
        }

        _canvasRect = targetCanvas.GetComponent<RectTransform>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 기존 호출부 호환용 (크리티컬 없음)
    public void ShowDamage(float damage, Vector3 worldPos)
    {
        Show(worldPos, damage, false);
    }

    // 크리티컬 포함 신규 호출
    public void Show(Vector3 worldPos, float damage, bool isCritical)
    {
        Debug.Log($"popupPool = {popupPool}");
        
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPos,
            null,
            out Vector2 canvasPos
        );

        GameObject obj = popupPool.Get(Vector3.zero, Quaternion.identity);
        if (obj == null) return;

        // GetComponent 캐싱 — 반복 호출 제거
        RectTransform rt    = obj.GetComponent<RectTransform>();
        DamagePopup   popup = obj.GetComponent<DamagePopup>();

        if (popup == null)
        {
            popupPool.Return(obj);
            return;
        }

        obj.transform.SetParent(_canvasRect, false);
        rt.localPosition = canvasPos;
        popup.Init(damage, isCritical);
    }
}