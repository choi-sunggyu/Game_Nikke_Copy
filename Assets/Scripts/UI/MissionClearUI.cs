using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionClearUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [Header("── UI 오브젝트 ───────────────────")]
    [SerializeField] private CanvasGroup     rootGroup;         // 전체 페이드용
    [SerializeField] private TMP_Text        clearText;         // MISSION CLEAR
    [SerializeField] private TMP_Text        stageInfoText;     // 스테이지 정보
    [SerializeField] private Button          nextStageButton;
    [SerializeField] private Button          mainMenuButton;

    [Header("── 캐릭터 이미지 ──────────────")]
    [SerializeField] private Image           characterImage;  // 단일 이미지
    [SerializeField] private CharacterBase[] characters;

    [Header("── 연출 설정 ──────────────────")]
    [SerializeField] private float           fadeInDuration  = 0.5f;
    [SerializeField] private float           textPunchScale  = 1.3f;  // CLEAR 텍스트 펀치 크기
    [SerializeField] private float           textPunchDuration = 0.3f;
    [SerializeField] private string          nextSceneName   = "BattleScene";
    [SerializeField] private string          mainMenuSceneName = "MainMenuScene";

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    void Awake()
    {
        rootGroup.alpha          = 0f;
        rootGroup.interactable   = false;
        rootGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        WaveManager.OnEliteDefeated += Show;
    }

    void OnDisable()
    {
        WaveManager.OnEliteDefeated -= Show;
    }

    void Start()
    {
        nextStageButton.onClick.AddListener(OnNextStage);
        mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    // ═══════════════════════════════════════════════════════
    //  표시
    // ═══════════════════════════════════════════════════════
    private void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        // ① 스테이지 정보 세팅
        SetStageInfo();

        // ② 캐릭터 이미지 세팅
        SetCharacterImages();

        // ③ 전체 페이드인
        yield return StartCoroutine(FadeIn());

        // ④ MISSION CLEAR 텍스트 펀치 효과
        yield return StartCoroutine(PunchText());

        // ⑤ 버튼 활성화
        rootGroup.interactable   = true;
        rootGroup.blocksRaycasts = true;
    }

    // ═══════════════════════════════════════════════════════
    //  스테이지 정보 세팅
    // ═══════════════════════════════════════════════════════
    private void SetStageInfo()
    {
        string difficulty = GameSettings.SelectedDifficulty switch
        {
            WaveManager.Difficulty.Easy   => "EASY",
            WaveManager.Difficulty.Normal => "NORMAL",
            WaveManager.Difficulty.Hard   => "HARD",
            _                             => "EASY"
        };

        stageInfoText.text = $"STAGE CLEAR\n{difficulty}";
    }

    // ═══════════════════════════════════════════════════════
    //  캐릭터 이미지 세팅 — 생존 여부에 따라 흑백 처리
    // ═══════════════════════════════════════════════════════
    private void SetCharacterImages()
    {
        CharacterBase mvp    = null;
        float         maxDmg = float.MinValue;

        foreach (var c in characters)
        {
            if (c.TotalDamageDealt > maxDmg)
            {
                maxDmg = c.TotalDamageDealt;
                mvp    = c;
            }
        }

        if (mvp == null) return;

        characterImage.sprite = mvp.CharacterPortrait;
    }

    // ═══════════════════════════════════════════════════════
    //  페이드인
    // ═══════════════════════════════════════════════════════
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        rootGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed          += Time.deltaTime;
            rootGroup.alpha   = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        rootGroup.alpha = 1f;
    }

    // ═══════════════════════════════════════════════════════
    //  MISSION CLEAR 텍스트 펀치 효과
    // ═══════════════════════════════════════════════════════
    private IEnumerator PunchText()
    {
        Transform t       = clearText.transform;
        float     elapsed = 0f;
        float     half    = textPunchDuration * 0.5f;

        // 확대
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, textPunchScale, elapsed / half);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;

        // 원상복귀
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(textPunchScale, 1f, elapsed / half);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // ═══════════════════════════════════════════════════════
    //  버튼 이벤트
    // ═══════════════════════════════════════════════════════
    private void OnNextStage()
    {
        // 다음 난이도로 이동 (Easy → Normal → Hard 순환)
        WaveManager.Difficulty next = GameSettings.SelectedDifficulty switch
        {
            WaveManager.Difficulty.Easy   => WaveManager.Difficulty.Normal,
            WaveManager.Difficulty.Normal => WaveManager.Difficulty.Hard,
            WaveManager.Difficulty.Hard   => WaveManager.Difficulty.Hard, // 마지막은 유지
            _                             => WaveManager.Difficulty.Normal
        };

        GameSettings.SelectedDifficulty = next;
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private void OnMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}