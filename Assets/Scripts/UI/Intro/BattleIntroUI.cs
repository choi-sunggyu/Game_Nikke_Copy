using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleIntroUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private float displayDuration = 2.5f; // 표시 유지
    [SerializeField] private float fadeDuration = 0.8f;    // 페이드 아웃

    void Start()
    {
        // 게임 입력 잠금
        InputManager inputManager = FindAnyObjectByType<InputManager>();
        if (inputManager != null) inputManager.enabled = false;

        StartCoroutine(PlayIntro(inputManager));
    }

    private IEnumerator PlayIntro(InputManager inputManager)
    {
        canvasGroup.alpha = 1f;

        // 표시 유지
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        // 입력 잠금 해제
        if (inputManager != null) inputManager.enabled = true;
    }
}