using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private CanvasGroup battleUICanvasGroup; // battleUIRoot에 CanvasGroup 컴포넌트 추가

    void Awake()
    {
        if(Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        HideBattleUI();
    }

    void OnEnable()
    {
        BattleIntroManager.OnBattleIntroComplete += ShowBattleUI;
        CharacterManager.OnGameOver              += HideBattleUI;
        GameTimerManager.OnTimeUp                += HideBattleUI;
        EnemyBase.OnBossDefeated                 += _ => HideBattleUI();
    }

    void OnDisable()
    {
        BattleIntroManager.OnBattleIntroComplete -= ShowBattleUI;
        CharacterManager.OnGameOver              -= HideBattleUI;
        GameTimerManager.OnTimeUp                -= HideBattleUI;
        EnemyBase.OnBossDefeated                 -= _ => HideBattleUI();
    }

    private void ShowBattleUI()
    {
        battleUICanvasGroup.alpha          = 1f;
        battleUICanvasGroup.interactable   = true;
        battleUICanvasGroup.blocksRaycasts = true;
    }

    private void HideBattleUI()
    {
        battleUICanvasGroup.alpha          = 0f;
        battleUICanvasGroup.interactable   = false;
        battleUICanvasGroup.blocksRaycasts = false;
    }
}