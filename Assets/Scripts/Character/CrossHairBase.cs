using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class CrossHairBase : MonoBehaviour
{
    // 변수
    protected RectTransform rectTransform;
    protected TextMeshProUGUI bulletCountText; // 현재 조작하는 캐릭터의 bulletCount 표시용 텍스트
    protected Vector2 currentPosition;
    protected bool isDragging;  // 터치 중인지
    protected bool isActive;

    // ★ 이 한 줄로 PC/모바일 조작 전환 (true = PC, false = 모바일)
    [Header("Platform Mode")]
    [SerializeField] protected static bool isPCMode = true;

    // CharacterManager 캐싱 (매번 Find 방지)
    private CharacterManager cachedCM;
    protected CharacterManager CM
    {
        get
        {
            if(cachedCM == null)
                cachedCM = FindAnyObjectByType<CharacterManager>();
            return cachedCM;
        }
    }

    public Vector2 CrossHairPosition => rectTransform.position;

    protected virtual void UpdateBulletCount(CharacterBase sender, int count)
    {
        if(!isActive) return;
        // 현재 활성 캐릭터가 보낸 이벤트만 처리
        if(CM != null && sender != CM.CurrentCharacter) return;
        if(bulletCountText != null)
            bulletCountText.text = count.ToString();
    }
    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    protected virtual void Start()
    {
        // 시작 시 화면 중앙으로 설정
        currentPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        rectTransform.position = currentPosition;
    }

    protected virtual void OnEnable()
    {
        // 이벤트 구독
        InputManager.OnFirePress += OnFirePress;
        InputManager.OnFireRelease += OnFireRelease;
        InputManager.OnSwitchCharacter += OnSwitchCharacter;
        CharacterBase.OnBulletCountChanged += UpdateBulletCount;
    }

    protected virtual void OnDisable()
    {
        // 이벤트 해제
        InputManager.OnFirePress -= OnFirePress;
        InputManager.OnFireRelease -= OnFireRelease;
        InputManager.OnSwitchCharacter -= OnSwitchCharacter;
        CharacterBase.OnBulletCountChanged -= UpdateBulletCount;
    }

    protected virtual void Update()
    {
        if(isPCMode)
        {
            // PC: 항상 마우스 위치 = 크로스헤어 위치 (클릭 여부 무관)
            Vector3 newPosition = Input.mousePosition;
            newPosition.x = Mathf.Clamp(newPosition.x, 0f, Screen.width);
            newPosition.y = Mathf.Clamp(newPosition.y, 0f, Screen.height);
            rectTransform.position = newPosition;
        }
        else if(isDragging)
        {
            // 모바일: 드래그 delta만큼 상대 이동
            Vector2 touchDelta = (Vector2)Input.mousePosition - currentPosition;
            Vector3 newPosition = rectTransform.position + (Vector3)touchDelta;
            newPosition.x = Mathf.Clamp(newPosition.x, 0f, Screen.width);
            newPosition.y = Mathf.Clamp(newPosition.y, 0f, Screen.height);
            rectTransform.position = newPosition;
            currentPosition = Input.mousePosition;
        }
    }

    protected virtual void OnFirePress() {
        isDragging = true;
        currentPosition = Input.mousePosition;
    }
    protected virtual void OnFireRelease() { isDragging = false; }  // 위치 고정

    abstract protected void OnSwitchCharacter(int index);  // 활성화/비활성화
    abstract protected void DrawCrossHair();  // 모양 생성
}