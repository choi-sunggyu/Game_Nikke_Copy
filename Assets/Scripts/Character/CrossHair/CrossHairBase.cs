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

    [Header("Owner")]
    [SerializeField] protected CharacterBase owner;

    [Header("에임 어시스트")]
    [SerializeField] protected float aimAssistOuterRadius = 150f; // 흡착 시작 반경
    [SerializeField] protected float aimAssistInnerRadius = 40f;  // 완전 고정 반경
    [SerializeField] protected float aimAssistStrength = 8f;      // Lerp 강도

    private WaveManager cachedWM;

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

        // Inspector에서 owner가 미할당된 경우, CrossHair를 참조하는 CharacterBase를 자동 탐색
        if (owner == null)
        {
            foreach (var c in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
            {
                if (c.CrossHair == this)
                {
                    owner = c;
                    break;
                }
            }
        }
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
        CharacterManager.OnCharacterSwitchConfirmed += OnSwitchCharacter;
    }

    protected virtual void OnDisable()
    {
        // 이벤트 해제
        InputManager.OnFirePress -= OnFirePress;
        InputManager.OnFireRelease -= OnFireRelease;
        InputManager.OnSwitchCharacter -= OnSwitchCharacter;
        CharacterBase.OnBulletCountChanged -= UpdateBulletCount;
        CharacterManager.OnCharacterSwitchConfirmed -= OnSwitchCharacter;
    }

    protected virtual void Update()
    {
        if (!isActive) return;
        if (CharacterAI.IsAiControllingCrosshair) 
        {
            return; 
        }

        if(isPCMode)
        {
            // PC: 항상 마우스 위치 = 크로스헤어 위치 (클릭 여부 무관)
            Vector3 newPosition = Input.mousePosition;
            newPosition.x = Mathf.Clamp(newPosition.x, 0f, Screen.width);
            newPosition.y = Mathf.Clamp(newPosition.y, 0f, Screen.height);

            // 마우스 위치 계산 직후 에임 어시스트 적용
            bool isClicking = Input.GetMouseButton(0);
            Vector2 assisted = ApplyAimAssist(newPosition, isClicking);
            rectTransform.position = assisted;
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

    
    protected WaveManager WM
    {
        get
        {
            if (cachedWM == null)
                cachedWM = FindAnyObjectByType<WaveManager>();
            return cachedWM;
        }
    }

    public Vector2 ApplyAimAssist(Vector2 screenPos, bool isClicking)
    {
        EnemyBase closest = GetClosestEnemyToScreenPos(screenPos);
        if (closest == null || closest.MuzzlePoint == null) return screenPos;

        // transform 대신 MuzzlePoint 기준
        Vector2 muzzleScreenPos = Camera.main.WorldToScreenPoint(closest.MuzzlePoint.position);
        float dist = Vector2.Distance(screenPos, muzzleScreenPos);

        if (dist < aimAssistInnerRadius)
            return muzzleScreenPos; // 클릭 중이어도 innerRadius 내면 고정 유지

        if (!isClicking && dist < aimAssistOuterRadius)
        {
            float t = 1f - (dist - aimAssistInnerRadius) / (aimAssistOuterRadius - aimAssistInnerRadius);
            return Vector2.Lerp(screenPos, muzzleScreenPos, t * aimAssistStrength * Time.deltaTime);
        }

        return screenPos;
    }

    private EnemyBase GetClosestEnemyToScreenPos(Vector2 screenPos)
    {
        var enemies = WM.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return null;

        EnemyBase closest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive || enemy.MuzzlePoint == null) continue;
            Vector2 muzzleScreen = Camera.main.WorldToScreenPoint(enemy.MuzzlePoint.position);
            float dist = Vector2.Distance(screenPos, muzzleScreen);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }
        return closest;
    }

    protected virtual void OnFirePress() {
        isDragging = true;
        currentPosition = Input.mousePosition;
    }
    protected virtual void OnFireRelease() { isDragging = false; }  // 위치 고정

    abstract protected void OnSwitchCharacter(int index);  // 활성화/비활성화
    abstract protected void DrawCrossHair();  // 모양 생성
}