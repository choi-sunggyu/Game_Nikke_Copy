using UnityEngine;
using UnityEngine.UI;

public class ReloadProgressBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;   // Fill 타입 Image
    [SerializeField] private GameObject barRoot; // 바 전체 오브젝트 (숨김/표시용)

    private CharacterManager characterManager;

    void Awake()
    {
        characterManager = FindAnyObjectByType<CharacterManager>();
        barRoot.SetActive(false); // 시작 시 숨김
    }

    void OnEnable()
    {
        CharacterBase.OnReloadProgress += HandleReloadProgress;
        CharacterBase.OnBulletCountChanged += HandleBulletCountChanged;
    }

    void OnDisable()
    {
        CharacterBase.OnReloadProgress -= HandleReloadProgress;
        CharacterBase.OnBulletCountChanged -= HandleBulletCountChanged;
    }

    private void HandleReloadProgress(CharacterBase sender, float progress)
    {
        // 현재 조작 중인 캐릭터만 표시
        if (characterManager.CurrentCharacter != sender) return;

        if (progress < 0f) // 취소 신호
        {
            barRoot.SetActive(false);
            return;
        }

        barRoot.SetActive(true);
        fillImage.fillAmount = progress;
    }

    private void HandleBulletCountChanged(CharacterBase sender, int count)
    {
        // 리로드 완료 시 바 숨김
        if (characterManager.CurrentCharacter != sender) return;
        if (sender.CurrentState != CharacterState.Reload)
            barRoot.SetActive(false);
    }
}