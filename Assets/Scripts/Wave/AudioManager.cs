using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClip mainMenuBGM;
    [SerializeField] private AudioClip battleBGM;

    private AudioSource bgmSource;

    void Awake()
    {
        // 싱글톤 + DontDestroyOnLoad
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.2f;
    }

    public void PlayMainMenuBGM()
    {
        Play(mainMenuBGM);
    }

    public void PlayBattleBGM()
    {
        Play(battleBGM);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }
}