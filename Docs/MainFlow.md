# 메인 플로우 재구현

2026-09-14 사용자 Unity 작업 지침에 따라 씬을 실제로 분리했다. 런타임 UI 생성기나 단일 씬 View 렌더러를 사용하지 않는다.

## 실행과 편집

1. `Assets/Scenes/MainScene.unity`를 열고 Play한다.
2. 출격 준비 → 유닛 선택 → 출격 → 일반 전투 → 지도 → 최종 전투 → 결과 → 메인으로 이동한다.
3. 전투는 기존 Q/W/E, 대상 클릭, A 확정, Esc 선택 취소를 사용한다. 작전 포기 버튼은 공격/대상 선택 대기를 취소한 뒤 결과 씬으로 이동한다.
4. `JangTest`에서 직접 Play하면 기본 편성으로 첫 전투를 시작한다. 기존 TestScript는 씬에서 비활성화했으며 파일은 보존했다.

| Scene | Inspector에서 편집할 대상 |
|---|---|
| MainScene | Canvas/MainMenu, MainSceneUI, PrepareButton OnClick |
| BattleReadyScene | Canvas/UnitPanel, RunePanel, Navigation, BattleReadySceneUI |
| StageMapScene | Canvas/StageInfo, RoomRoute, Footer, StageMapSceneUI |
| JangTest | 기존 전투 Canvas/Managers/배치 위치, 추가 StageBattleHUD와 BattleSceneController |
| ResultScene | Canvas/MissionResult, ResultSceneUI, ReturnMainButton OnClick |

- 새 씬은 JangTest의 환경·카메라·Canvas·EventSystem 설정을 바탕으로 편집 모드에서 작성해 저장했다. 전투 Manager/Units는 메뉴 씬에 남기지 않았다.
- 버튼은 기존 TurnDisplayObject 기반 `MenuButton.prefab` Variant, 유닛 목록은 기존 ActionDisplay 기반 `UnitSelection.prefab` Variant다.
- Anchor와 Layout Group은 Scene/Prefab 안에 저장된다. 일반 버튼 연결은 영구 UnityEvent, UI 참조는 SerializeField다.
- 런타임 UI Instantiate는 유닛 목록과 기존 턴/행동/컷인 Prefab에만 사용한다.
- `GameFlowSettings.asset`에서 씬 경로와 실제 유닛·적 Prefab을 변경한다.
- `GameSession.prefab`만 씬 사이에 유지된다. 편성과 순수 C# StageSession을 보관하며 전투 GameObject/UI는 보관하지 않는다. SceneTransition은 씬 로딩만 담당한다.
- BattleManager가 정리 후 결과를 한 번 전달하고 BattleSceneController가 지도/결과 이동을 요청한다. 정산 알림은 GameSession.StageSettled다. 재화 지급은 구현하지 않았다.

## 전투 보완

- 팀별 생존 판정, 승패/포기/오류 구분, 종료 후 유닛·턴 UI·컷인·대상 선택·카메라·명령 이력 정리.
- Nora → TargetManager → TurnAction → BattleAction까지 취소 전달.
- 공격별 애니메이션 상태를 SO에서 제거하고 실행 지역 변수로 분리. 타임아웃 타이머를 취소 소스보다 먼저 해제.
- 스킬별 타깃 데이터, 효과 동시 만료/제거 콜백/사망 알림 순서 보완.
- AV 조회와 실제 진행 분리, 속도 변경 시 남은 AV 환산.
- 런타임 능력치 복사, 음수 유효 방어 하한, 런타임 UnityEditor 참조 제거.

## 검증

- 메뉴: `Tools > Game > Run Scene Flow Tests`.
- Editor 전용 테스트가 실제 버튼의 Inspector OnClick, 실제 Scene 로드, 기존 Nora 공격 애니메이션을 사용한다. 빠른 승리 검증에 한해 생성된 적 인스턴스 HP만 1로 낮춘다. 원본 SO/Prefab 수치는 변경하지 않는다.
- 실행 결과와 화면 캡처: `Logs/SceneFlowValidation/` (로컬 검증 산출물, Git 제외).
- 최종 실행: 2026-09-14 22:47 KST. 110개 assertion 통과(씬별 반복 확인 포함), 런타임 오류 0개. 일반/최종 승리, 패배, 공격 중 포기, 대상 선택 취소/포기, 지도 포기, 편성 유지/잠금, 재출격, 중복 정산 방지, 효과 동시 만료, AV 조회, 실제 궁극기 컷인과 종료 후 31초 지연 콜백을 검증했다.
- 저장된 다섯 씬은 각각 Canvas 1개이며 Missing Script 0개, 정적 버튼 8개의 Inspector OnClick 연결을 확인했다. 1920×1080 플레이 화면 캡처를 확인했다.
- Unity 스크립트 컴파일/플레이 검증 완료. 플레이어 배포 빌드는 실행하지 않았다.

## 범위와 임시 규칙

- 단계 0/0B의 작은 2개 방 플로우이며 완성 게임이나 완성 아트가 아니다.
- 현재 구현된 아군/적 Prefab을 사용한다. 동시 출전 상수 4를 유지하며 존재하지 않는 5명 로스터를 만들지 않았다.
- 일반/최종 방은 같은 기존 적 편성이다. 전투마다 HP를 초기화한다. 동시 전멸은 패배로 처리한다. 이들은 프로토타입 규칙이다.
- 룬/모듈 효과, 보상 재화, 저장·이어하기, 상대 속도 패링, PVP는 이번 재구현 범위 밖이다.
- Assets/Onsil은 조사·수정·신규 아트 재사용에서 제외했다. 기존 전투 Prefab의 기존 의존 관계는 변경하지 않았다.
