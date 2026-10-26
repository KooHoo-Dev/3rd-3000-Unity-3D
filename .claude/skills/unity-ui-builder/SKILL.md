---
name: unity-ui-builder
description: Unity uGUI 화면을 코드로 조립하는 에디터 빌더 스크립트(MonoBehaviour + ContextMenu "Build UI / Clear UI")를 작성한다. UI 시안 스크린샷·목업 이미지를 주며 "이 화면 만들어줘", "UI 빌더 스크립트", "Canvas 코드로 생성", "uGUI 자동 생성", "로그인/로비/인벤토리/HUD 화면 스크립트" 같은 요청이 오면 — '빌더'나 '스크립트'라는 말이 없어도 Unity UI 화면을 만들어 달라는 요청이면 — 반드시 이 스킬을 사용한다. 프리팹을 손으로 만드는 안내나 UI Toolkit(UXML) 작업에는 쓰지 않는다.
---

# Unity UI Builder

UI 시안 스크린샷을 분석해, 그 화면을 씬에 만들어 내는 **Unity 에디터 스크립트 한 개**를 작성한다.
결과물은 빈 GameObject 에 붙여 컴포넌트 우클릭 메뉴로 실행하는 `MonoBehaviour` 이고,
`references/UIBuilderTemplate.cs` 의 뼈대를 그대로 쓰고 **영역별 `CreateXxx` 메서드만 채우는** 것이 작업의 전부다.

## 1. 입력 정리

필요한 값은 다섯 가지다. 사용자가 주지 않은 값은 **묻지 말고** 아래 기본값으로 채우고, 출력의 "조정 포인트"에 가정했다고 적는다.

| 항목 | 기본값 |
|---|---|
| 클래스명 | 화면 이름 + `UIBuilder` (예: `LobbyUIBuilder`) |
| 네임스페이스 | 프로젝트의 기존 스크립트가 쓰는 것. 없으면 생략 가능 |
| 루트 오브젝트 이름 | 화면 이름 + ` UI` (예: `Lobby UI`) |
| 기준 해상도 | 1920 x 1080 (세로 화면 시안이면 1080 x 1920) |
| 주석 언어 | 사용자의 언어 |

## 2. 스크린샷 읽는 순서

1. **영역으로 나눈다** — 상단 바 / 중앙 / 하단 패널 / 팝업 … 각각이 `CreateXxx` 메서드 하나가 된다.
2. **요소를 나열하고 이름을 정한다** — PascalCase, 역할이 드러나게 (`GoldText`, `StartButton`, `InventoryGrid`). 스프라이트 자리는 `Image` 색상 블록으로 대신하고 이름으로 용도를 남긴다 (`HeroPortraitImage`).
3. **어느 가장자리에 붙는지 판단한다** — 아래 표로 앵커·피벗을 정한다.
4. **크기·간격을 기준 해상도 픽셀로 추정한다** — 스크린샷 비율로 환산한다. 확신 없는 값은 조정 포인트에 적는다.

### 앵커 표 (요소가 붙는 가장자리 = 앵커)

| 배치 | anchorMin ~ anchorMax | pivot | sizeDelta 주의 |
|---|---|---|---|
| 상단 가로 띠 | (0,1) ~ (1,1) | (0.5,1) | x = 0, y = 높이 |
| 하단 가로 띠 | (0,0) ~ (1,0) | (0.5,0) | x = 0, y = 높이 |
| 좌측 세로 띠 | (0,0) ~ (0,1) | (0,0.5) | x = 폭, y = 0 |
| 하단 중앙 패널 | (0.5,0) ~ (0.5,0) | (0.5,0) | 실제 크기 |
| 우측 하단 버튼 | (1,0) ~ (1,0) | (1,0) | 실제 크기 |
| 화면 중앙 팝업 | (0.5,0.5) ~ (0.5,0.5) | (0.5,0.5) | 실제 크기 |
| 꽉 채움 | `Stretch(rect, padding)` | | |

늘어나는 축의 `sizeDelta` 는 "앵커 폭에 더하는 값"이라 0 이어야 한다. 여기에 실제 폭을 넣으면 화면 밖으로 두 배로 튀어나간다. 이것이 가장 흔한 실수다.

## 3. 코드 작성

`references/UIBuilderTemplate.cs` 를 읽고 그 구조를 유지한다. 플레이스홀더 `__CLASS__`, `__NAMESPACE__`, `__ROOT__`, `__W__`, `__H__` 를 채우고, 예시 영역(`CreateExampleTopBar`)을 실제 영역 메서드들로 바꾼다. 뼈대(진입점, Canvas, EventSystem, 도우미)는 손대지 않는다 — 아래 이유로 이미 다듬어져 있다.

**뼈대가 그렇게 생긴 이유** (바꾸고 싶을 때 참고)
- 클래스 선언은 `#if UNITY_EDITOR` 밖, 메서드만 안쪽이다. 클래스까지 감싸면 빌드에서 Missing Script 경고가 나고 `font` 필드가 사라진다.
- 루트 탐색은 `GameObject.Find` 대신 현재 씬의 루트 오브젝트를 이름으로 훑는다. `Find` 는 비활성 오브젝트를 놓치고 다른 씬까지 뒤진다.
- 루트 오브젝트 자체가 Canvas 이며 씬 루트에 놓인다. 빌더의 자식으로 두면 Clear 가 빌더까지 지우거나 Undo 그룹이 꼬인다.
- EventSystem 이 없으면 경고만 하지 않고 만든다. 없으면 버튼이 전혀 눌리지 않아 "코드가 틀렸다"고 오해하기 때문이다. 입력 시스템 설정(`ENABLE_INPUT_SYSTEM`)에 맞는 모듈을 고른다.
- `NewUI` 는 `SetParent(parent, false)` 를 쓴다. `true` 면 월드 좌표를 유지하려다 크기와 위치가 틀어진다.
- `NewText` 는 `font` 가 null 일 때 TMP 기본 폰트를 그대로 둔다. 한글이 필요하면 Inspector 에서 한글 SDF 폰트를 넣는 것이 사용자의 몫이다 (이 프로젝트에는 `Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset` 이 있다).
- `NewPanel` 은 `raycastTarget = false` 다. 장식 패널이 아래 버튼의 클릭을 가로채는 사고를 막는다. 모달 배경처럼 클릭을 막아야 하는 패널만 호출부에서 `true` 로 켠다.

**도우미 시그니처** (템플릿과 동일하게 유지)
```csharp
GameObject NewUI(string name, Transform parent)
Image      NewPanel(string name, Transform parent, Vector2 size, Color color)
TMP_Text   NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
Button     NewButton(string name, Transform parent, string label, Color background, out TMP_Text labelText)
void       Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
void       Place(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)   // 점 앵커 축약
void       Stretch(RectTransform rect, float padding = 0)
```

**영역 메서드 규칙**
- 영역마다 `CreateXxx(Transform canvas)` 하나, `#region 영역 N : 이름` 으로 묶는다. `BuildUI` 에서 **뒤에 깔리는 것부터** 호출한다 — 하이어라키 순서가 그리기 순서다.
- 모든 요소는 `Place` 또는 `Stretch` 를 정확히 한 번 거친다. 그래야 anchorMin·anchorMax·pivot·anchoredPosition·sizeDelta 가 전부 명시된다.
- 같은 모양이 반복되면(슬롯, 목록, 인벤토리 칸) 부모에 `HorizontalLayoutGroup` / `VerticalLayoutGroup` / `GridLayoutGroup` 을 붙이고 **자식에는 `Place` 를 쓰지 않는다.** 레이아웃 그룹이 자식의 위치·크기를 덮어쓰므로 명시해 봐야 의미가 없고, 읽는 사람만 헷갈린다. 자식 크기는 `GridLayoutGroup.cellSize` 또는 `LayoutElement.preferredWidth/Height` 로 준다.
- 처음에 숨겨져 있어야 할 패널은 **그 메서드의 마지막 줄**에서 `SetActive(false)` 한다. 중간에 끄면 그 아래에 만드는 자식들의 레이아웃 계산이 생략된다.
- 색은 코드에서 `Color` 로 지정한다. 외부 이미지·폰트·머티리얼·`Resources.Load` 는 쓰지 않는다 — 결과 스크립트가 어떤 프로젝트에 붙여 넣어도 바로 컴파일되어야 하기 때문이다.
- 게임 로직 스크립트를 붙이거나 참조를 연결하지 않는다. 이 스크립트는 **화면만** 만든다. 버튼의 `onClick` 연결은 사용자가 나중에 한다.

반복 요소 예시:
```csharp
Image grid = NewPanel("InventoryGrid", panel.transform, Vector2.zero, new Color(0, 0, 0, 0.3f));
Place(grid.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 380f));
var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
layout.cellSize = new Vector2(100f, 100f);
layout.spacing = new Vector2(4f, 4f);
layout.padding = new RectOffset(8, 8, 8, 8);
for (int i = 0; i < 18; i++)
    NewPanel($"Slot{i:00}", grid.transform, Vector2.zero, new Color(0.2f, 0.2f, 0.2f));   // Place 없음
```

## 4. 출력 형식

이 순서로, 이 세 블록만 낸다.

1. **하이어라키 트리** — 만들 오브젝트 구조. 들여쓰기 텍스트 트리, 각 줄에 오브젝트 이름과 (컴포넌트).
2. **C# 코드 전체** — 한 파일, 그대로 붙여넣어 쓸 수 있게. 파일명은 `클래스명.cs`.
3. **조정 포인트** — 스크린샷만으로 확신할 수 없었던 부분(크기·색·간격), 기본값으로 가정한 입력, 손으로 다듬으면 좋은 값. 사용 절차 한 줄(빈 오브젝트에 붙이기 → Font 할당 → 우클릭 Build UI)도 여기에.

## 5. 내기 전 점검

- 늘어나는 축의 `sizeDelta` 가 0 인가 (띠·꽉 채움 패널).
- 레이아웃 그룹의 자식에 `Place` 를 호출하지 않았는가.
- 숨김 패널의 `SetActive(false)` 가 메서드 마지막 줄인가.
- `BuildUI` 의 호출 순서가 배경 → 전경인가.
- 뼈대(진입점·도우미)를 바꾸지 않았는가. 바꿨다면 조정 포인트에 이유를 적었는가.
- `Place` 의 anchoredPosition 부호: 오른쪽/위쪽 가장자리에 붙는 요소는 안쪽으로 들어올 때 **음수**다.
