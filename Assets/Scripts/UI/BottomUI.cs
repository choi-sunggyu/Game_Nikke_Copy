using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomUI : MonoBehaviour
{
    [System.Serializable]
    public class CharacterBox
    {
        public GameObject root;
        public Image background;
        public Image characterIcon;
        public Image hpBar;
        public Image shieldBar;
        public GameObject coverIndicator; // 엄폐 중 표시
        public GameObject disconnectedOverlay; // 사망 표시
    }

    [SerializeField] private List<CharacterBox> characterBoxes;
    [SerializeField] private CharacterManager characterManager;

    // 현재 캐릭터 박스 크기
    [SerializeField] private float boxWidth = 100f;
    [SerializeField] private float activeBoxHeight = 120f;
    [SerializeField] private float inactiveBoxHeight = 100f;

    private List<CharacterBase> characters;

    void Start()
    {
        characters = characterManager.Characters;

        foreach (var box in characterBoxes)
        {
            if (box.background != null)
                box.background.sprite = CreateGradientSprite(100, 120);
        }

        foreach (var box in characterBoxes)
        {
            if (box.disconnectedOverlay != null)
                box.disconnectedOverlay.SetActive(false);
            if (box.coverIndicator != null)
                box.coverIndicator.SetActive(false);
        }

        RefreshAll();
    }

    void OnEnable()
    {
        InputManager.OnSwitchCharacter += HandleSwitchCharacter;
        CharacterBase.OnStatChanged += HandleStatChanged;
        CharacterBase.OnCharacterDied += HandleCharacterDied;
        InputManager.OnCoverToggle += HandleCoverToggle;
    }

    void OnDisable()
    {
        InputManager.OnSwitchCharacter -= HandleSwitchCharacter;
        CharacterBase.OnStatChanged -= HandleStatChanged;
        CharacterBase.OnCharacterDied -= HandleCharacterDied;
        InputManager.OnCoverToggle -= HandleCoverToggle;
    }

    Sprite CreateGradientSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            float alpha = 1f - (float)y / height; // 아래=불투명, 위=투명
            Color color = new Color(0, 0, 0, alpha);
            
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, color);
            }
        }
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
}

    private void RefreshAll()
    {
        for (int i = 0; i < characterBoxes.Count && i < characters.Count; i++)
        {
            bool isActive = characters[i] == characterManager.CurrentCharacter;
            UpdateBox(i, isActive);
        }
        RefreshCoverIndicators();
    }

    private void UpdateBox(int index, bool isActive)
    {
        var box = characterBoxes[index];
        var c = characters[index];

        // 세로만 변경, 가로 고정
        RectTransform rt = box.root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(boxWidth, isActive ? activeBoxHeight : inactiveBoxHeight);

        // HP/Shield 바
        box.hpBar.fillAmount = c.HpRatio;
        box.shieldBar.fillAmount = c.ShieldRatio;
    }

    private void HandleSwitchCharacter(int index)
    {
        RefreshAll();
    }

    private void HandleStatChanged(CharacterBase sender)
    {
        int idx = characters.IndexOf(sender);
        if (idx < 0 || idx >= characterBoxes.Count) return;

        bool isActive = sender == characterManager.CurrentCharacter;
        characterBoxes[idx].hpBar.fillAmount = sender.HpRatio;
        characterBoxes[idx].shieldBar.fillAmount = sender.ShieldRatio;
    }

    private void HandleCharacterDied(CharacterBase dead)
    {
        int idx = characters.IndexOf(dead);
        if (idx < 0 || idx >= characterBoxes.Count) return;

        characterBoxes[idx].hpBar.fillAmount = 0f;
        characterBoxes[idx].shieldBar.fillAmount = 0f;

        // Disconnected 오버레이 표시
        if (characterBoxes[idx].disconnectedOverlay != null)
            characterBoxes[idx].disconnectedOverlay.SetActive(true);

        // 엄폐 표시 숨김 (사망 시)
        if (characterBoxes[idx].coverIndicator != null)
            characterBoxes[idx].coverIndicator.SetActive(false);
    }

    private void HandleCoverToggle()
    {
        RefreshCoverIndicators();
    }

    private void RefreshCoverIndicators()
    {
        bool isCovering = characterManager.IsCovering;
        for (int i = 0; i < characterBoxes.Count && i < characters.Count; i++)
        {
            if (characterBoxes[i].coverIndicator == null) continue;
            if (!characters[i].IsAlive)
            {
                characterBoxes[i].coverIndicator.SetActive(false);
                continue;
            }

            bool isCurrentCharacter = characters[i] == characterManager.CurrentCharacter;

            // 엄폐 중이고 현재 캐릭터가 아닐 때만 표시 (반전 수정)
            characterBoxes[i].coverIndicator.SetActive(isCovering && !isCurrentCharacter);
        }
    }
}