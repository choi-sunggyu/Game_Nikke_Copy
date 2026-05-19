# Game_Nikke_Copy
시프트업의 건슈팅 RPG, NIKKE 모작

개요

Project BUSTER는 Goddess of Victory: Nikke 의 전투 구조를 분석하고,
핵심 시스템을 직접 재구현하며 게임 클라이언트 아키텍처 역량을 증명하기 위해 제작한 프로젝트입니다.

단순 모작이 아니라 다음 요소들을 중심으로 구조를 재설계했습니다.

State Machine 기반 전투 흐름
ScriptableObject 데이터 분리
StatusEffect 상속 구조
Object Pooling 최적화
Mobile → PC 입력 추상화
캐릭터 스위칭 기반 카메라 연출
프로젝트 정보
항목	내용
프로젝트명	Project BUSTER
장르	Mobile TPS / Cover Shooter
개발 인원	1인 개발
개발 기간	3개월
엔진	Unity
언어	C#
플랫폼	Android → PC Porting
개발 목적	전투 시스템 및 클라이언트 아키텍처 포트폴리오
핵심 전투 시스템
1. 캐릭터 스위칭 시스템

전투 중 실시간으로 3명의 캐릭터를 전환하며 플레이할 수 있습니다.

주요 기능
전환 딜레이 적용
캐릭터별 HP / 버프 상태 유지
사망 캐릭터 전환 제한
캐릭터 전환 시 카메라 Lerp 이동
구현 포인트
- State Machine 기반 상태 제어
- 입력 차단 처리
- Camera Lerp 연동
2. 버스트 게이지 시스템

팀 전체가 공유하는 단일 Burst Gauge를 사용합니다.

특징
캐릭터별 충전 효율 상이
게이지 100% 달성 시 버스트 사용 가능
사용 후 게이지 초기화
설계 의도

원작의 Full Burst 구조를 단순화하여,
전투 템포와 역할군 차이를 명확하게 드러내도록 설계했습니다.

3. 리로딩 시스템

무기 타입마다 서로 다른 리로드 구조를 가집니다.

캐릭터	무기	탄창	리로드
3버스터	Sniper Rifle	5	3.0s
2버스터	Minigun	100	2.0s
1버스터	Assault Rifle	30	1.5s
구현 요소
자동 리로딩
강제 풀 리로딩
리로드 UI 연동
입력 차단 처리
4. 적 AI 시스템

적 타입별로 서로 다른 행동 패턴을 구성했습니다.

Enemy Types
타입	특징
원거리 고정형	고정 위치 사격
위치 변경형	Waypoint 이동
근접 돌진형	플레이어 추적
구현 방식
Idle
→ Patrol
→ Attack
→ Move
→ Dead

State Machine 기반으로 동작합니다.

기술적 구현 포인트
ScriptableObject 기반 데이터 설계

캐릭터 / 무기 / 스킬 데이터를 코드에서 분리하여 관리했습니다.

장점
데이터 수정 용이
유지보수성 향상
확장성 확보
밸런싱 편의성 증가
StatusEffect 시스템

버프 / 디버프 / DOT를 하나의 구조로 통합했습니다.

구조
StatusEffect (Base)
 ├── BuffEffect
 ├── DebuffEffect
 └── DotEffect
적용 기술
상속
다형성
Component 기반 설계
Object Pooling 최적화

다음 오브젝트들을 풀링 처리했습니다.

Bullet
Enemy
Effect
Damage UI
목적
GC 최소화
모바일 성능 최적화
Instantiate/Destroy 비용 감소
입력 추상화

Unity Input System을 사용해
모바일과 PC 입력을 통합 처리했습니다.

지원 입력
Touch
Mouse
Keyboard
프로젝트 구조
Assets
├── Scripts
│   ├── Character
│   ├── Enemy
│   ├── Combat
│   ├── StatusEffect
│   ├── UI
│   └── Managers
├── ScriptableObjects
├── Prefabs
├── Animations
├── Effects
└── Resources
개발 일정
Phase	목표
Phase 1	코어 전투 시스템 구축
Phase 2	스킬 / 스위칭 / Burst 시스템
Phase 3	연출 / UI / 최적화 / PC 포팅
차별화 포인트
카메라 연출 강화

원작과 달리 캐릭터가 서로 다른 위치를 가지며,
전환 시 카메라가 자연스럽게 이동합니다.

플랫폼 이식 고려 설계

초기부터 Mobile → PC 포팅을 고려한 입력 추상화 구조로 개발했습니다.

포트폴리오 중심 구조 설계

단순 기능 구현이 아니라 다음 역량을 드러내는 데 집중했습니다.

게임 아키텍처 설계
OOP 설계
상태머신 활용
성능 최적화
유지보수 가능한 구조 설계
사용 기술
기술	내용
Engine	Unity
Language	C#
Pattern	State Machine
Architecture	Component Based
Data	ScriptableObject
Optimization	Object Pooling
Input	Unity Input System
향후 개선 예정
Full Burst 단계 시스템
보스 패턴 AI
사운드 믹싱 개선
피격 연출 강화
모바일 UI 최적화
Addressables 적용
플레이 영상
추후 추가 예정
스크린샷
추후 추가 예정
회고

본 프로젝트는 단순히 게임을 따라 만드는 것이 아니라,
상용 게임의 전투 구조를 분석하고 이를 직접 설계/구현하는 과정에 초점을 두었습니다.

특히 다음 부분에 집중했습니다.

유지보수 가능한 구조 설계
시스템 간 의존성 최소화
데이터 중심 구조
모바일 환경 성능 최적화
플랫폼 확장 고려 설계
References
Goddess of Victory: Nikke
Unity Official Website
Unity Input System Documentation
ScriptableObject Documentation
