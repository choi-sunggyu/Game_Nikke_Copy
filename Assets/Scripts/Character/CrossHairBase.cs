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

    public Vector2 CrossHairPosition => rectTransform.position;

    protected virtual void UpdateBulletCount(int count)
    {
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
        if(isDragging)
        {
            Vector2 touchDelta = (Vector2)Input.mousePosition - currentPosition;
            Vector3 newPosition = rectTransform.position + (Vector3)touchDelta;

            // 화면 경계 클램핑
            newPosition.x = Mathf.Clamp(newPosition.x, 0f, Screen.width);
            newPosition.y = Mathf.Clamp(newPosition.y, 0f, Screen.height);

            rectTransform.position = newPosition;
            currentPosition = Input.mousePosition;
        }
    }

    protected virtual void OnFirePress() { 
        isDragging = true; 
        currentPosition = Input.mousePosition;  // 시작점 초기화
    }
    protected virtual void OnFireRelease() { isDragging = false; }  // 위치 고정

    abstract protected void OnSwitchCharacter(int index);  // 활성화/비활성화
    abstract protected void DrawCrossHair();  // 모양 생성
}