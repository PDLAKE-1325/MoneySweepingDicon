# Unity 작업 지침

## 작업 전

- 반드시 관련 Scene, Canvas/Hierarchy, UI Prefab, Inspector 연결, UI 제어 코드, Manager, ScriptableObject, 입력, 이벤트, 패키지, asmdef와 유사 구현을 조사한다.
- 코드를 쓰기 전에 이번 변경에 영향을 주는 조사 결과와 구현 방식을 짧게 보고한다.
- 기존 구현을 우선한다. 변경이 필요하면 발견한 문제, 기존 방식을 사용하지 않는 이유, 개선 방식을 먼저 설명한다.
- `Assets/Onsil`은 조사·수정·신규 재사용 대상에서 제외한다. 기존 전투 Prefab의 연결은 임의로 바꾸지 않는다.
- 사용자 변경을 보존한다. 요청 없이 커밋·푸시하지 않는다.

## Scene / UI / Editor

- 명확히 다른 게임 단계는 실제 Scene으로 분리하고 SceneManager로 전환한다. 메인/준비/진행/전투/결과를 단일 Scene의 Panel 전환으로 대체하지 않는다.
- UI는 저장된 Canvas, Hierarchy, Prefab, RectTransform, Layout Group, Legacy Text, Button, Image를 사용하고 SerializeField/UnityEvent로 연결한다.
- 런타임에 new GameObject로 Canvas/Button/Text 등 화면 전체를 조립하거나 Render 함수로 View를 재생성하지 않는다.
- 기존 Canvas·UI Prefab·Layout·Anchor를 확인하고 재사용한다. 코드에 화면 좌표 전체를 하드코딩하지 않는다.
- 동적 유닛/아이템/로그 목록 등 필요한 경우에만 UI Prefab을 Instantiate한다.
- 코드만 남기지 말고 실제 Scene/Prefab 저장과 Inspector 연결까지 완료한다. 반복적인 Editor 작업은 필요할 때만 자동화한다.
- UI 스크립트는 기존 오브젝트의 텍스트, 이미지, 버튼, 게이지, 애니메이션과 전환 요청만 제어한다.

## 코드 설계

- Unity Component 조합, 생명주기, ScriptableObject 정적 데이터와 런타임 상태 분리를 따른다.
- 기존 Command, UniTask, Animator, DOTween과 입력 방식을 우선한다. 입력과 행동은 분리하고 취소 토큰을 전체 비동기 경로에 전달한다.
- Awake는 내부 초기화, Start는 외부 연결, OnEnable/OnDisable은 이벤트 등록/해제로 사용한다.
- Inspector 참조를 우선하며 매 프레임 Find/GetComponent/LINQ/불필요한 할당을 하지 않는다.
- SOLID와 State/Strategy/Observer/Command/Pool 등은 책임 분리·확장·테스트에 실제 도움이 될 때 적용한다. 불필요한 인터페이스·DI·웹식 다계층 구조를 만들지 않는다.
- 거대한 Manager나 enum/switch에 입력/UI/데이터/전투/상태 책임을 몰아넣지 않는다. Unity 생명주기가 필요 없는 로직은 순수 C#으로 분리한다.
- 수치·효과·공격별 임시 상태로 ScriptableObject 원본을 변경하지 않는다.
- 작업 후 컴파일, 실제 씬 이동, 입력·전투·정리·재진입을 검증하고 미검증 범위를 구분한다.
