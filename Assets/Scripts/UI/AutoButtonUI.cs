using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoButtonUI : MonoBehaviour
{
    [SerializeField] private Image buttonBackground;
    [SerializeField] private TextMeshProUGUI label;

    private static readonly Color ColorOn  = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color ColorOff = new Color(0.4f, 0.4f, 0.4f);

    void OnEnable()
    {
        BurstGaugeManager.OnAutoModeChanged += HandleAutoModeChanged;
    }

    void OnDisable()
    {
        BurstGaugeManager.OnAutoModeChanged -= HandleAutoModeChanged;
    }

    private void HandleAutoModeChanged(bool isAuto)
    {
        buttonBackground.color = isAuto ? ColorOn : ColorOff;
        label.text = isAuto ? "AUTO ON" : "AUTO OFF";
    }

    public void OnButtonClicked() // Button 컴포넌트 OnClick에 연결
    {
        BurstGaugeManager.Instance?.ToggleAutoMode();
    }
}