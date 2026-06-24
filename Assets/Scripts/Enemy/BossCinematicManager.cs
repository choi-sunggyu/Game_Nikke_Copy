using System.Collections;
using UnityEngine;

public class BossCinematicManager : MonoBehaviour
{
    public static BossCinematicManager Instance { get; private set; }

    // [DEPRECATED] 카메라 컷씬 파라미터는 UIManager 로 이전됨.
    //   기존 appearZoomFov / appearDuration / deathZoomFov / deathZoomDuration
    //   → UIManager 인스펙터의 같은 이름 필드에서 설정.

    [Header("슬로우 모션")]
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowDuration    = 1.5f;

    // 카메라 직접 참조 제거 — UIManager.Instance.Trigger*() 통해서만 호출 (월권 차단)
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
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

        // 카메라 컷씬은 UIManager facade 를 통해 호출 — 직접 CameraController 호출 금지.
        UIManager.Instance?.TriggerBossAppearCinematic(boss, () =>
        {
            InputManager.SetInputLocked(false);
        });
    }

    private void HandleBossDeath(EnemyBase boss)
    {
        InputManager.SetInputLocked(true);
        GameTimerManager.Instance?.StopTimer();

        Vector3 bossPos = boss.transform.position;

        UIManager.Instance?.TriggerBossDeathCinematic(bossPos, () =>
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