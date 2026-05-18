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
        canvasRect = FindAnyObjectByType<Canvas>().GetComponent<RectTransform>();
    }

    public void ShowDamage(float damage, Vector3 worldPos)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null, // Overlay Canvas는 null
            out Vector2 canvasPos
        );

        GameObject popup = Instantiate(popupPrefab, canvasRect);
        popup.GetComponent<RectTransform>().localPosition = canvasPos;
        popup.GetComponent<DamagePopup>().Init(damage);
    }
}