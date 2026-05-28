using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverRoot;  // 전체 패널
    [SerializeField] private GameObject stageClearRoot;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;    
    [SerializeField] private Button clearRestartButton;
    [SerializeField] private Button clearMainMenuButton;

    void Awake()
    {
        gameOverRoot.SetActive(false);
        stageClearRoot.SetActive(false);
    }

    void OnEnable()
    {
        CharacterManager.OnGameOver += ShowGameOver;
        WaveManager.OnStageClear += ShowStageClear;
    }

    void OnDisable()
    {
        CharacterManager.OnGameOver -= ShowGameOver;
        WaveManager.OnStageClear -= ShowStageClear;
    }

    void Start()
    {
        restartButton.onClick.AddListener(OnClickRestart);
        mainMenuButton.onClick.AddListener(OnClickMainMenu);
        clearRestartButton.onClick.AddListener(OnClickRestart);
        clearMainMenuButton.onClick.AddListener(OnClickMainMenu);
    }

    private void ShowGameOver()
    {
        gameOverRoot.SetActive(true);
    }

    private void ShowStageClear()
    {
        stageClearRoot.SetActive(true);
    }

    public void OnClickRestart()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}