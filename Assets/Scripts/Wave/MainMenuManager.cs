using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    //[SerializeField] private Button bossButton;

    void Start()
    {
        AudioManager.Instance.PlayMainMenuBGM();
        easyButton.onClick.AddListener(OnClickEasy);
        normalButton.onClick.AddListener(OnClickNormal);
        hardButton.onClick.AddListener(OnClickHard);
        //bossButton.onClick.AddListener(OnClickBoss);
    }
    public void OnClickEasy()
    {
        GameSettings.SelectedDifficulty = WaveManager.Difficulty.Easy;
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnClickNormal()
    {
        GameSettings.SelectedDifficulty = WaveManager.Difficulty.Normal;
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnClickHard()
    {
        GameSettings.SelectedDifficulty = WaveManager.Difficulty.Hard;
        SceneManager.LoadScene("LoadingScene");
    }

    // public void OnClickBoss()
    // {
    //     GameSettings.SelectedDifficulty = WaveManager.Difficulty.Boss;
    //     SceneManager.LoadScene("LoadingScene");
    // }
}