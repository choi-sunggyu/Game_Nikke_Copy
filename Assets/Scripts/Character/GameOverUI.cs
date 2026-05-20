using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverRoot;  // 전체 패널
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    void Awake()
    {
        gameOverRoot.SetActive(false);
    }

    void OnEnable()
    {
        CharacterManager.OnGameOver += ShowGameOver;
    }

    void OnDisable()
    {
        CharacterManager.OnGameOver -= ShowGameOver;
    }

    void Start()
    {
        restartButton.onClick.AddListener(OnClickRestart);
        mainMenuButton.onClick.AddListener(OnClickMainMenu);
    }

    private void ShowGameOver()
    {
        gameOverRoot.SetActive(true);
    }

    private void OnClickRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}