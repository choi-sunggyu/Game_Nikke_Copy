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
        if(bulletCountText != null) // 보험
            bulletCountText.gameObject.SetActive(isActive);
        // 캐릭터 변경 시 현재 캐릭터 총알 수 표시 업데이트
        if(isActive)
            UpdateBulletCount(CharacterBase.CurrentBulletCount);
        
        // 활성화/비활성화 처리
        DrawCrossHair();
    }

    protected override void Start()
    {
        base.Start();
        bulletCountText = bulletText;
        if(bulletCountText != null) // 보험
            bulletCountText.gameObject.SetActive(false);
    }

    protected override void DrawCrossHair()
    {
        crossHairObject.GetComponent<Image>().sprite = crossHairSprite;
    }
}
