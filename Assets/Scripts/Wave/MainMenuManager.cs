using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    void Start()
    {
        AudioManager.Instance.PlayMainMenuBGM();
        easyButton.onClick.AddListener(OnClickEasy);
        normalButton.onClick.AddListener(OnClickNormal);
        hardButton.onClick.AddListener(OnClickHard);
    }
    public void OnClickEasy()
    {
        GameSettings.SelectedDifficulty = WaveManager.Difficulty.Easy;
        SceneManager.LoadScene("BattleScene");
    }

    public void OnClickNormal()
    {
        GameSettings.SelectedDifficulty = WaveManager.Difficulty.Normal;
        SceneManager.LoadScene("BattleScene");
    }

    public void OnClickHard()
    {
        GameSettings.SelectedDifficulty = WaveManager.Difficulty.Hard;
        SceneManager.LoadScene("BattleScene");
    }
}