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
        isActive = (CM.CurrentCharacter == owner);
        if(!isActive)
        {
            isDragging = false;
            crossHairObject.SetActive(false);
        }
        else
        {
            crossHairObject.SetActive(true);
            RefreshBulletCount();
        }

        if(bulletCountText != null)
            bulletCountText.gameObject.SetActive(isActive);

        DrawCrossHair();
    }

    private void RefreshBulletCount()
    {
        if(CM != null && CM.CurrentCharacter != null && bulletCountText != null)
            bulletCountText.text = CM.CurrentCharacter.CurrentBulletCount.ToString();
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
