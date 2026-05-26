using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoBurstButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image rotateIcon;

    private static readonly Color ColorOn  = new Color(1.0f, 0.4980392f, 0f);
    private static readonly Color ColorOff = new Color(0.7830189f, 0.7830189f, 0.7830189f);
    private bool isBurstAutoMode = false;

    void OnEnable()
    {
        BurstGaugeManager.OnAutoModeChanged += HandleAutoModeChanged;
    }

    void OnDisable()
    {
        BurstGaugeManager.OnAutoModeChanged -= HandleAutoModeChanged;
    }

    void Update()
    {
        if(isBurstAutoMode)
        {
            // 반대 방향으로 회전
            rotateIcon.rectTransform.Rotate(0, 0, -100 * Time.deltaTime);
        }
    }

    private void HandleAutoModeChanged(bool isAuto)
    {
        if (isAuto)
        {
            label.color = ColorOn;
            rotateIcon.color = ColorOn;
        }
        else
        {
            label.color = ColorOff;
            rotateIcon.color = ColorOff;
        }
        isBurstAutoMode = isAuto;
    }

    public void OnButtonClicked() // Button 컴포넌트 OnClick에 연결
    {
        BurstGaugeManager.Instance?.ToggleAutoMode();
    }
}