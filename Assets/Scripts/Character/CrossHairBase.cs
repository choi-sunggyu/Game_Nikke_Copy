using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class CrossHairBase : MonoBehaviour
{
    // 변수
    protected RectTransform rectTransform;
    protected Text bulletCountText; // 현재 조작하는 캐릭터의 bulletCount 표시용 텍스트
    protected Vector2 currentPosition;
    protected bool isDragging;  // 터치 중인지
    protected bool isActive;

    protected virtual void UpdateBulletCount(int count)
    {
        if(bulletCountText != null)
            bulletCountText.text = count.ToString();
    }
    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
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
        // 터치 중이면 마우스 위치 따라 이동
        if(isDragging)
        {
            currentPosition = Input.mousePosition;
            rectTransform.position = currentPosition;
        }
    }

    protected virtual void OnFirePress() { isDragging = true; }
    protected virtual void OnFireRelease() { isDragging = false; }  // 위치 고정

    abstract protected void OnSwitchCharacter(int index);  // 활성화/비활성화
    abstract protected void DrawCrossHair();  // 모양 생성
}