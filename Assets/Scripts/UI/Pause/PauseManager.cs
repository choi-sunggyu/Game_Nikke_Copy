using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button retireButton;
    [SerializeField] private Button restartButton;

    public bool IsPaused { get; private set; } = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        pauseButton.onClick.AddListener(TogglePause);
        closeButton.onClick.AddListener(Resume);
        retireButton.onClick.AddListener(GoToMainMenu);
        restartButton.onClick.AddListener(RestartBattle);
    }

    void OnDisable()
    {
        pauseButton.onClick.RemoveListener(TogglePause);
        closeButton.onClick.RemoveListener(Resume);
        retireButton.onClick.RemoveListener(GoToMainMenu);
        restartButton.onClick.RemoveListener(RestartBattle);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        InputManager.SetInputLocked(true);
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        InputManager.SetInputLocked(false);
        pausePanel.SetActive(false);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void RestartBattle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}