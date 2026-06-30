using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("── 공통 ───────────────────────")]
    [SerializeField] private CanvasGroup rootGroup;

    [Header("── 게임오버 ─────────────────")]
    [SerializeField] private GameObject  gameOverRoot;
    [SerializeField] private TMP_Text    failedText;
    [SerializeField] private Button      restartButton;
    [SerializeField] private Button      mainMenuButton;

    [Header("── 미션 클리어 ──────────────")]
    [SerializeField] private GameObject      stageClearRoot;
    [SerializeField] private Image           background;
    [SerializeField] private Image           forwardground;
    [SerializeField] private TMP_Text        completedText;
    [SerializeField] private Image           characterImage;
    [SerializeField] private CharacterBase[] characters;
    [SerializeField] private Button          clearRestartButton;
    [SerializeField] private Button          clearMainMenuButton;

    [Header("── 연출 ─────────────────────")]
    [SerializeField] private float fadeInDuration    = 0.5f;
    [SerializeField] private float textPunchScale    = 1.3f;
    [SerializeField] private float textPunchDuration = 0.3f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        gameOverRoot.SetActive(false);
        stageClearRoot.SetActive(false);

        rootGroup.alpha          = 0f;
        rootGroup.interactable   = false;
        rootGroup.blocksRaycasts = false;
    }

    void OnEnable()
    {
        CharacterManager.OnGameOver += ShowGameOver;
        GameTimerManager.OnTimeUp   += ShowGameOver;
    }

    void OnDisable()
    {
        CharacterManager.OnGameOver -= ShowGameOver;
        GameTimerManager.OnTimeUp   -= ShowGameOver;
    }

    void Start()
    {
        restartButton.onClick.AddListener(OnClickRestart);
        mainMenuButton.onClick.AddListener(OnClickMainMenu);
        clearRestartButton.onClick.AddListener(OnClickRestart);
        clearMainMenuButton.onClick.AddListener(OnClickMainMenu);
        background.sprite = Black_Gradation.Create(topAlpha: 0.7f);
        forwardground.sprite = Black_Gradation.Create();
    }

    // ─────────────────────────────────────────
    //  외부 호출
    // ─────────────────────────────────────────
    public void ShowGameOver()
    {
        gameOverRoot.SetActive(true);
        failedText.color = Color.red;
        failedText.text  = "FAILED";
        StartCoroutine(ShowSequence(failedText));
    }

    // BossCinematicManager에서 호출
    public void ShowMissionComplete()
    {
        SetMVPCharacter();
        stageClearRoot.SetActive(true);
        completedText.color = new Color(0.3f, 0.6f, 1f, 1f);
        completedText.text  = "MISSION COMPLETED";
        StartCoroutine(ShowSequence(completedText));
    }

    // ─────────────────────────────────────────
    //  공통 연출
    // ─────────────────────────────────────────
    private IEnumerator ShowSequence(TMP_Text target)
    {
        yield return StartCoroutine(FadeIn());

        rootGroup.interactable   = true;
        rootGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeIn()
    {
        float elapsed   = 0f;
        rootGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed        += Time.deltaTime;
            rootGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        rootGroup.alpha = 1f;
    }

    // ─────────────────────────────────────────
    //  MVP 캐릭터
    // ─────────────────────────────────────────
    private void SetMVPCharacter()
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

        if (mvp != null)
            characterImage.sprite = mvp.CharacterPortrait;
    }

    // ─────────────────────────────────────────
    //  버튼
    // ─────────────────────────────────────────
    private void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LoadingScene");
    }

    private void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
}