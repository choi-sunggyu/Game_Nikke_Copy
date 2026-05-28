using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float minimumLoadTime = 4f; // 개발자용 최소 로딩 시간 (나중에 0으로 바꿔)

    void Start()
    {
        StartCoroutine(LoadBattleScene());
    }

    private IEnumerator LoadBattleScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("BattleScene");
        op.allowSceneActivation = false; // 로드 완료해도 바로 전환 안 함

        float elapsed = 0f;

        while (!op.isDone)
        {
            elapsed += Time.deltaTime;

            // 실제 로딩 진행도와 최소 시간 중 느린 쪽을 기준으로 표시
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsed / minimumLoadTime);
            float displayProgress = Mathf.Min(realProgress, timeProgress);

            if (progressBar != null)
                progressBar.fillAmount = displayProgress;

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(displayProgress * 100f)}%";

            // 실제 로딩 완료 + 최소 시간 경과 둘 다 충족해야 전환
            if (op.progress >= 0.9f && elapsed >= minimumLoadTime)
                op.allowSceneActivation = true;

            yield return null;
        }
    }
}