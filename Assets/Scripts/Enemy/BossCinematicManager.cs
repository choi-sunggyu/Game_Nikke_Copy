using System.Collections;
using UnityEngine;

public class BossCinematicManager : MonoBehaviour
{
    public static BossCinematicManager Instance { get; private set; }

    [Header("보스 출현")]
    [SerializeField] private float appearZoomFov   = 10f;
    [SerializeField] private float appearDuration  = 2f;

    [Header("보스 사망")]
    [SerializeField] private float deathZoomFov    = 35f;
    [SerializeField] private float deathZoomDuration = 0.4f;
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowDuration    = 1.5f;

    private CameraController _camController;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _camController = FindAnyObjectByType<CameraController>();
    }

    void OnEnable()
    {
        WaveManager.OnBossPhaseStart += HandleBossAppear;
        EnemyBase.OnBossDefeated     += HandleBossDeath;
    }

    void OnDisable()
    {
        WaveManager.OnBossPhaseStart -= HandleBossAppear;
        EnemyBase.OnBossDefeated     -= HandleBossDeath;
    }

    private void HandleBossAppear(EnemyBase boss)
    {
        InputManager.SetInputLocked(true);
        _camController.StartBossAppearCinematic(boss, appearDuration, appearZoomFov, () =>
        {
            InputManager.SetInputLocked(false);
        });
    }

    private void HandleBossDeath(EnemyBase boss)
    {
        // 모든 UI 비활성화
        
        InputManager.SetInputLocked(true);
        GameTimerManager.Instance?.StopTimer();

        Vector3 bossPos = boss.transform.position;

        _camController.StartBossDeathCinematic(deathZoomFov, deathZoomDuration, bossPos, () =>
        {
            StartCoroutine(SlowMotionThenResult());
        });
    }

    private IEnumerator SlowMotionThenResult()
    {
        Time.timeScale = slowMotionScale;

        float elapsed = 0f;
        while (elapsed < slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(0.3f);

        GameOverUI.Instance?.ShowMissionComplete();
    }
}