using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniGunCrossHair : CrossHairBase
{
    [SerializeField] private GameObject crossHairObject;
    [SerializeField] private Sprite crossHairSprite;
    [SerializeField] private TextMeshProUGUI bulletText;
    protected override void OnSwitchCharacter(int index)
    {
        isActive = (index == 1);
        if(!isActive)
        {
            isDragging = false;
            crossHairObject.SetActive(false);
        }
        else
        {
            crossHairObject.SetActive(true);
        }
        // 활성화/비활성화 처리
        DrawCrossHair();
    }

    protected override void DrawCrossHair()
    {
        crossHairObject.GetComponent<Image>().sprite = crossHairSprite;
    }
}
