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
            if(bulletCountText != null)
                bulletCountText.gameObject.SetActive(isActive);
            crossHairObject.SetActive(true);
        }
        if(bulletCountText != null)
            bulletCountText.gameObject.SetActive(isActive);
        
        // 활성화/비활성화 처리
        DrawCrossHair();
    }

    protected override void Start()
    {
        base.Start();
        bulletCountText = bulletText;
        if(bulletCountText != null)
            bulletCountText.gameObject.SetActive(false);
    }

    protected override void DrawCrossHair()
    {
        crossHairObject.GetComponent<Image>().sprite = crossHairSprite;
    }
}
