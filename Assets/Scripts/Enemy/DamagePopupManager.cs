using UnityEngine;
using TMPro;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private Canvas targetCanvas;
    private RectTransform canvasRect;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // FindAnyObjectByType 제거 → 인스펙터 연결로 대체
        if (targetCanvas == null)
        {
            Debug.LogError("[DamagePopupManager] targetCanvas가 연결되지 않았습니다.");
            return;
        }

        canvasRect = targetCanvas.GetComponent<RectTransform>();
    }

    public void ShowDamage(float damage, Vector3 worldPos)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out Vector2 canvasPos
        );

        GameObject popup = Instantiate(popupPrefab, canvasRect);
        popup.SetActive(true);

        // ★ 진단용 로그
        Debug.Log($"activeSelf: {popup.activeSelf}");
        Debug.Log($"activeInHierarchy: {popup.activeInHierarchy}");
        Debug.Log($"Canvas activeInHierarchy: {canvasRect.gameObject.activeInHierarchy}");

        popup.GetComponent<RectTransform>().localPosition = canvasPos;
        popup.GetComponent<DamagePopup>().Init(damage);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}