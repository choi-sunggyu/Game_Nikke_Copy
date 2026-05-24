using System.Collections;
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

        // --- 런타임 전용(직렬화 X) ---
        [System.NonSerialized] public Coroutine heightCoroutine; // 박스 높이 애니메이션
        [System.NonSerialized] public Coroutine coverCoroutine;  // 엄폐 인디케이터 페이드
        [System.NonSerialized] public CanvasGroup coverGroup;    // 엄폐 인디케이터 페이드용
    }

    [SerializeField] private List<CharacterBox> characterBoxes;
    [SerializeField] private CharacterManager characterManager;

    // 현재 캐릭터 박스 크기
    [SerializeField] private float activeBoxHeight = 130f;   // 조작 중인 캐릭터 박스 높이
    [SerializeField] private float inactiveBoxHeight = 100f; // 기본(비조작) 박스 높이

    [Header("애니메이션")]
    [SerializeField] private float heightAnimDuration = 0.2f; // 박스 높이 전환 시간
    [SerializeField] private float coverFadeDuration = 0.25f; // 엄폐 인디케이터 페이드 시간

    private List<CharacterBase> characters;

    void Start()
    {
        characters = characterManager.Characters;
        InitBoxes();
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

    // ── 초기화: 모든 박스의 배경/엄폐 인디케이터를 동일하게 세팅 ──
    private void InitBoxes()
    {
        int gradientApplied = 0;

        foreach (var box in characterBoxes)
        {
            // [4번] background 그라데이션 — 연결된 모든 박스에 동일하게 적용
            if (box.background != null)
            {
                box.background.type = Image.Type.Simple; // 인스펙터에서 Sliced/Filled로 바뀌어도 강제 통일
                box.background.sprite = CreateGradientSprite(64, 128, Color.black, 1f, 0f);
                gradientApplied++;
            }
            else
            {
                Debug.LogWarning("[BottomUI] background가 연결되지 않은 박스가 있습니다. 인스펙터를 확인하세요.");
            }

            // 사망 오버레이는 시작 시 숨김
            if (box.disconnectedOverlay != null)
                box.disconnectedOverlay.SetActive(false);

            // [2번] 엄폐 인디케이터: 하늘색 그라데이션 + 페이드용 CanvasGroup
            if (box.coverIndicator != null)
            {
                Image coverImg = box.coverIndicator.GetComponent<Image>();
                if (coverImg != null)
                {
                    coverImg.type = Image.Type.Simple;
                    coverImg.color = Color.white; // 스프라이트 원색이 그대로 보이도록 흰색으로
                    // 아래=하늘색 A 70%, 위로 갈수록 A 0
                    coverImg.sprite = CreateGradientSprite(64, 128, new Color(0.51f, 0.65f, 1f), 0.7f, 0f);
                }

                box.coverGroup = box.coverIndicator.GetComponent<CanvasGroup>();
                if (box.coverGroup == null)
                    box.coverGroup = box.coverIndicator.AddComponent<CanvasGroup>();
                box.coverGroup.alpha = 0f;

                box.coverIndicator.SetActive(false);
            }
        }

        Debug.Log($"[BottomUI] 그라데이션 적용 박스: {gradientApplied}/{characterBoxes.Count}");
    }

    // 세로 방향 알파 그라데이션 스프라이트 생성
    // baseColor: 색상(RGB), bottomAlpha: 아래쪽 알파, topAlpha: 위쪽 알파
    Sprite CreateGradientSprite(int width, int height, Color baseColor, float bottomAlpha, float topAlpha)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.wrapMode = TextureWrapMode.Clamp; // 가장자리 늘어남 방지

        for (int y = 0; y < height; y++)
        {
            // 텍스처는 y=0이 아래쪽 → t=0(아래) ~ t=1(위)
            float t = (height <= 1) ? 0f : (float)y / (height - 1);
            float alpha = Mathf.Lerp(bottomAlpha, topAlpha, t);
            Color color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, color);
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

        // [1번] 조작 중인 캐릭터는 activeBoxHeight, 그 외는 inactiveBoxHeight
        float targetHeight = isActive ? activeBoxHeight : inactiveBoxHeight;

        // 실제로 눈에 보이는 것은 root가 아니라 background 자식이므로 background를 직접 리사이즈한다.
        if (box.background != null)
        {
            if (box.heightCoroutine != null) StopCoroutine(box.heightCoroutine);
            box.heightCoroutine = StartCoroutine(AnimateBoxHeight(box.background.rectTransform, targetHeight));
        }

        box.hpBar.fillAmount = c.HpRatio;
        box.shieldBar.fillAmount = c.ShieldRatio;
    }

    private IEnumerator AnimateBoxHeight(RectTransform rt, float targetHeight)
    {
        if (rt == null) yield break;

        float startHeight = rt.sizeDelta.y;
        // 시작 시점의 '아래 모서리 Y'를 한 번 계산해 고정 기준으로 삼는다.
        // 점 앵커(min==max) 기준: 아래 모서리 = anchoredPosition.y - pivot.y * height
        float bottomY = rt.anchoredPosition.y - rt.pivot.y * startHeight;
        float elapsed = 0f;

        while (elapsed < heightAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / heightAnimDuration); // 가감속으로 자연스럽게
            float newHeight = Mathf.Lerp(startHeight, targetHeight, t);
            ApplyHeightKeepBottom(rt, newHeight, bottomY);
            yield return null;
        }

        ApplyHeightKeepBottom(rt, targetHeight, bottomY);
    }

    // 높이를 바꾸되 아래 모서리를 bottomY에 고정 → 박스가 위로만 자란다(아래로만 줄어든다).
    private void ApplyHeightKeepBottom(RectTransform rt, float height, float bottomY)
    {
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

        Vector2 pos = rt.anchoredPosition;
        pos.y = bottomY + rt.pivot.y * height; // 아래 모서리 고정식을 역으로 푼 값
        rt.anchoredPosition = pos;
    }

    private void HandleSwitchCharacter(int index)
    {
        RefreshAll();
    }

    private void HandleStatChanged(CharacterBase sender)
    {
        int idx = characters.IndexOf(sender);
        Debug.Log($"HandleStatChanged / sender: {sender.name} / idx: {idx} / shieldRatio: {sender.ShieldRatio}");
        if (idx < 0 || idx >= characterBoxes.Count) return;

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
        SetCoverIndicator(characterBoxes[idx], false);
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
            var box = characterBoxes[i];
            if (box.coverIndicator == null) continue;

            bool shouldShow;
            if (!characters[i].IsAlive)
            {
                shouldShow = false;
            }
            else
            {
                bool isCurrentCharacter = characters[i] == characterManager.CurrentCharacter;
                // 엄폐 중이고 현재 캐릭터가 아닐 때만 표시
                shouldShow = isCovering && !isCurrentCharacter;
            }

            SetCoverIndicator(box, shouldShow);
        }
    }

    // 엄폐 인디케이터를 페이드인/페이드아웃으로 전환
    private void SetCoverIndicator(CharacterBox box, bool show)
    {
        if (box.coverIndicator == null || box.coverGroup == null) return;

        if (box.coverCoroutine != null) StopCoroutine(box.coverCoroutine);
        box.coverCoroutine = StartCoroutine(FadeCoverIndicator(box, show));
    }

    // [2번] 스페이스 입력 시 Fade in 효과로 엄폐 인디케이터 등장
    private IEnumerator FadeCoverIndicator(CharacterBox box, bool show)
    {
        CanvasGroup cg = box.coverGroup;

        // 이미 목표 상태면 굳이 애니메이션하지 않음
        if (!show && !box.coverIndicator.activeSelf) yield break;

        if (show) box.coverIndicator.SetActive(true);

        float start = cg.alpha;
        float target = show ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < coverFadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, target, elapsed / coverFadeDuration);
            yield return null;
        }
        cg.alpha = target;

        if (!show) box.coverIndicator.SetActive(false);
    }
}
