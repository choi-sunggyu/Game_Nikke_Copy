using UnityEngine;
using TMPro;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private GameObject popupPrefab;
    private RectTransform canvasRect;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DamagePopupManager] Canvas를 찾을 수 없습니다.");
            return;
        }

        canvasRect = FindAnyObjectByType<Canvas>().GetComponent<RectTransform>();
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
        popup.SetActive(true);  // ★ Init 전에 활성화
        popup.GetComponent<RectTransform>().localPosition = canvasPos;
        popup.GetComponent<DamagePopup>().Init(damage);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}