using UnityEngine;
using UnityEngine.UI;

public class AutoScopeButtonUI : MonoBehaviour
{
    [SerializeField] private Image autoScopeIcon;

    private static readonly Color ColorOn  = new Color(1.0f, 0.4980392f, 0f);
    private static readonly Color ColorOff = new Color(0.7830189f, 0.7830189f, 0.7830189f);
    private bool isScopeAutoMode = false;

    void OnEnable()
    {
        CharacterAI.OnAutoScopeModeChanged += AutoScopeModeChanged;
    }

    void OnDisable()
    {
        CharacterAI.OnAutoScopeModeChanged -= AutoScopeModeChanged;
    }

    private void AutoScopeModeChanged(bool isScopeAuto)
    {
        if(isScopeAuto)
        {
            autoScopeIcon.color = ColorOn;
        }
        else
        {
            autoScopeIcon.color = ColorOff;
        }
        isScopeAutoMode = isScopeAuto;
    }

    public void OnButtonClicked() // Button 컴포넌트 OnClick에 연결
    {
        CharacterAI.ToggleAutoScopeMode();
    }
}