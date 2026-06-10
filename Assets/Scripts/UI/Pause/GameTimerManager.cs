using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimerManager : MonoBehaviour
{
    public static GameTimerManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float totalTime = 90f; // 1분 30초

    private float remainingTime;
    private bool isRunning = false;

    public static event Action OnTimeUp; // 시간 초과 이벤트

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        BattleIntroManager.OnBattleIntroComplete += StartTimer;
    }

    void OnDisable()
    {
        BattleIntroManager.OnBattleIntroComplete -= StartTimer;
    }

    private void StartTimer()
    {
        remainingTime = totalTime;
        isRunning = true;
    }

    public void StopTimer() => isRunning = false;

    void Update()
    {
        if (!isRunning) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            UpdateTimerUI(0f);
            OnTimeUp?.Invoke();
            return;
        }

        UpdateTimerUI(remainingTime);
    }

    private void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:0}:{seconds:00}";

        // 30초 이하면 빨간색으로 경고
        timerText.color = time <= 30f ? Color.red : Color.white;
    }
}