// ============================================================================
//  __CLASS__ — uGUI 화면을 코드로 조립하는 에디터 빌더
//  사용법 : 빈 GameObject 에 붙이고 컴포넌트 우클릭 → "1. Build UI" / "2. Clear UI"
//  Inspector 의 Font 칸에 한글 TMP 폰트(SDF)를 넣어야 한글이 보입니다.
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace __NAMESPACE__
{
    /// <summary>
    /// 클래스 선언은 #if UNITY_EDITOR 밖에 둔다.
    /// 클래스까지 감싸면 빌드에서 이 컴포넌트가 "Missing Script" 가 되고 font 필드도 사라진다.
    /// 에디터 전용으로 묶는 것은 메서드(구현)뿐이다.
    /// </summary>
    public class __CLASS__ : MonoBehaviour
    {
        [Tooltip("생성되는 모든 텍스트에 적용할 폰트. 비워두면 TMP 기본 폰트(한글 글리프 없음)가 쓰인다.")]
        [SerializeField] private TMP_FontAsset font;

        private const string RootName = "__ROOT__";
        private static readonly Vector2 ReferenceResolution = new Vector2(__W__, __H__);

#if UNITY_EDITOR
        // ------------------------------------------------------------------ 진입점

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            if (FindRoot() != null)
            {
                Debug.LogWarning($"[{nameof(__CLASS__)}] '{RootName}' 가 이미 씬에 있습니다. 먼저 '2. Clear UI' 를 실행하세요.", this);
                return;
            }
            if (font == null)
                Debug.LogWarning($"[{nameof(__CLASS__)}] Font 가 비어 있습니다. 한글이 □ 로 보이면 Inspector 에 한글 SDF 폰트를 넣고 Clear → Build 하세요.", this);

            Undo.SetCurrentGroupName("Build UI");
            int undoGroup = Undo.GetCurrentGroup();

            EnsureEventSystem();
            Transform canvas = CreateCanvasRoot();

            // ---- 영역별 생성 : 뒤에 깔리는 것 → 위에 얹히는 것 순서로 호출 (하이어라키 순서 = 그리기 순서) ----
            CreateExampleTopBar(canvas);
            // CreateCenter(canvas);
            // CreateBottomPanel(canvas);
            // CreatePopup(canvas);   // 처음에 숨길 패널은 해당 메서드 마지막 줄에서 SetActive(false)

            Undo.CollapseUndoOperations(undoGroup);           // Ctrl+Z 한 번으로 전체 되돌리기
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[{nameof(__CLASS__)}] '{RootName}' 생성 완료", this);
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject root = FindRoot();
            if (root == null)
            {
                Debug.Log($"[{nameof(__CLASS__)}] 지울 '{RootName}' 가 없습니다.", this);
                return;
            }
            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[{nameof(__CLASS__)}] '{RootName}' 삭제", this);
        }

        /// <summary>
        /// 이 컴포넌트가 속한 씬의 루트 오브젝트 중 이름이 RootName 인 것.
        /// GameObject.Find 는 비활성 오브젝트를 못 찾고 다른 씬·프리팹 스테이지까지 뒤지므로 쓰지 않는다.
        /// </summary>
        private GameObject FindRoot()
        {
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
                if (go.name == RootName) return go;
            return null;
        }

        // ------------------------------------------------------------------ Canvas / EventSystem

        /// <summary>루트 오브젝트 = Canvas. 빌더의 자식이 아니라 씬 루트에 둔다(Clear 가 빌더 자신을 지우지 않도록).</summary>
        private Transform CreateCanvasRoot()
        {
            var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Build UI");   // 자식들은 루트와 함께 되돌려진다
            root.layer = LayerMask.NameToLayer("UI");

            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return root.transform;
        }

        /// <summary>EventSystem 이 없으면 버튼이 전혀 눌리지 않으므로 경고만 하지 않고 만들어 준다. 프로젝트의 입력 설정에 맞는 모듈을 붙인다.</summary>
        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "Build UI");
            Debug.LogWarning($"[{nameof(__CLASS__)}] 씬에 EventSystem 이 없어 새로 만들었습니다.", es);
        }

        // ------------------------------------------------------------------ 도우미

        /// <summary>RectTransform 만 가진 빈 UI 오브젝트. SetParent(parent, false) 가 핵심 — true 면 월드 좌표를 유지하려다 크기·위치가 틀어진다.</summary>
        private GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>단색 Image 패널. 스프라이트가 들어갈 자리는 전부 이것으로 대신하고 이름으로 용도를 드러낸다.</summary>
        private Image NewPanel(string name, Transform parent, Vector2 size, Color color)
        {
            var img = NewUI(name, parent).AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;      // 장식 패널이 아래 버튼의 클릭을 가로채지 않도록. 모달 배경은 호출부에서 true 로.
            img.rectTransform.sizeDelta = size;
            return img;
        }

        /// <summary>TextMeshProUGUI 텍스트. font 가 비어 있으면 TMP 기본 폰트를 그대로 둔다.</summary>
        private TMP_Text NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = NewUI(name, parent).AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (font != null) t.font = font;
            return t;
        }

        /// <summary>배경 Image + Button + 가운데 라벨. 라벨은 out 으로 돌려주어 호출부에서 색·크기를 바꿀 수 있다.</summary>
        private Button NewButton(string name, Transform parent, string label, Color background, out TMP_Text labelText)
        {
            Image img = NewPanel(name, parent, new Vector2(200f, 60f), background);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            labelText = NewText("Label", img.transform, label, 28f, Color.white, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform);
            return button;
        }

        /// <summary>
        /// 앵커·피벗·위치·크기를 한 번에 명시한다. 모든 요소는 Place 또는 Stretch 를 거친다.
        /// 한 축이 늘어나는 앵커(min.x != max.x)라면 그 축의 size 는 0 — sizeDelta 는 "앵커 폭에 더하는 값"이다.
        /// </summary>
        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        /// <summary>점 앵커 축약형 : anchorMin = anchorMax = pivot. 예) 우측 하단 버튼 → Place(rt, new Vector2(1, 0), new Vector2(-40, 40), size)</summary>
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
            => Place(rect, anchor, anchor, anchor, anchoredPosition, size);

        /// <summary>부모를 꽉 채운다. padding 만큼 안쪽으로 들어온다.</summary>
        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static RectTransform Rect(Component c) => (RectTransform)c.transform;

        // ------------------------------------------------------------------ 영역별 생성

        #region 영역 1 : 상단 바 (예시 — 실제 화면에 맞게 교체)
        private void CreateExampleTopBar(Transform canvas)
        {
            // 화면 윗변에 붙어 가로로 늘어나는 띠 : anchor (0,1)~(1,1), pivot (0.5,1), size.x = 0
            Image bar = NewPanel("TopBar", canvas, Vector2.zero, new Color(0.10f, 0.10f, 0.12f, 0.9f));
            Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 80f));

            // 띠의 왼쪽 가운데에 붙는 제목
            TMP_Text title = NewText("TitleText", bar.transform, "LOBBY", 36f, Color.white, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(400f, 60f));

            // 띠의 오른쪽 가운데에 붙는 버튼
            Button settings = NewButton("SettingsButton", bar.transform, "설정", new Color(0.25f, 0.25f, 0.30f), out _);
            Place(Rect(settings), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(160f, 56f));
        }
        #endregion
#endif
    }
}
