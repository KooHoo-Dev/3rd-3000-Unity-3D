using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;

namespace Study.Utilities
{
    // SimpleInput 이 제대로 동작하는지 눈으로 확인하기 위한 디버깅 컴포넌트 입니다.
    // SimpleInputDebugUI 프리펩을 씬에 올려두고 플레이하면
    //  - 키보드 : 누르고 있는 키, 마지막 Down / Up 키, AnyKeyDown
    //  - 축     : GetAxisHorizontalRaw / GetAxisVerticalRaw / GetMoveAxisRaw
    //  - 마우스 : 위치, 이동량(delta), 휠, 버튼
    //  - 로그   : 어떤 입력이 몇 번째 프레임에 들어왔는지
    // 를 실시간으로 보여줍니다.
    // 개별 키 칸의 색상 표시는 SimpleInputKeyIndicator 가 담당합니다.
    public class SimpleInputDebugger : MonoBehaviour
    {
        [Header("표시 토글")]
        public Canvas canvas;
        [Tooltip("이 키를 누르면 디버그 UI 를 숨기거나 보여줍니다. None 이면 사용하지 않습니다.")]
        public Key toggleKey = Key.F12;

        [Header("장치 상태")]
        public TMP_Text deviceStatusText;

        [Header("키보드")]
        public TMP_Text keyboardText;

        [Header("축 (WASD / 방향키)")]
        public RectTransform axisPad;
        public RectTransform axisDot;
        public TMP_Text axisText;

        [Header("마우스")]
        public TMP_Text mouseText;
        public RectTransform deltaPad;
        public RectTransform deltaDot;
        [Tooltip("한 프레임에 이 값(px) 만큼 움직이면 점이 패드 끝까지 이동합니다.")]
        public float deltaVisualRange = 40f;
        [Tooltip("마우스 위치를 따라다니는 표시. 화면 좌표가 맞게 들어오는지 확인할 수 있습니다.")]
        public RectTransform cursorMarker;
        public TMP_Text cursorMarkerText;

        [Header("이벤트 로그")]
        public TMP_Text logText;
        public int maxLogLines = 12;

        private static readonly SimpleInput.MouseButton[] mouseButtons =
        {
            SimpleInput.MouseButton.Left,
            SimpleInput.MouseButton.Right,
            SimpleInput.MouseButton.Middle,
        };

        // Key.ToString() 은 호출할 때마다 문자열을 새로 만들기 때문에 한 번만 만들어 둡니다.
        private readonly Dictionary<Key, string> keyNames = new Dictionary<Key, string>();
        private readonly Queue<string> logLines = new Queue<string>();
        private readonly StringBuilder builder = new StringBuilder();

        private bool logDirty;
        private bool keyboardDirty = true;
        private bool? lastKeyboardConnected;
        private bool? lastMouseConnected;

        private Key lastDownKey = Key.None;
        private Key lastUpKey = Key.None;
        private int lastDownFrame;
        private int lastUpFrame;
        private int anyKeyDownCount;

        private Vector2 lastMoveAxis = new Vector2(float.NaN, float.NaN);

        private Vector2 lastMousePosition = new Vector2(float.NaN, float.NaN);
        private Vector2 lastMouseDelta = new Vector2(float.NaN, float.NaN);
        private float lastScrollRawThisFrame = float.NaN;
        private float lastScrollRaw;
        private float lastScrollStep;
        private int lastScrollFrame;
        private int scrollStepSum;
        private bool mouseDirty = true;

        private void Reset()
        {
            canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && toggleKey != Key.None && SimpleInput.GetKeyDown(toggleKey) && canvas != null)
            {
                canvas.enabled = !canvas.enabled;

                // 숨겨져 있는 동안 바뀐 값이 있을 수 있으니 다시 보일 때 전부 갱신합니다.
                if (canvas.enabled) MarkAllDirty();
            }

            if (canvas != null && !canvas.enabled) return;

            UpdateDeviceStatus(keyboard != null, mouse != null);

            if (keyboard != null)
            {
                UpdateKeyboard(keyboard);
                UpdateAxis();
            }

            if (mouse != null)
            {
                UpdateMouse();
            }

            if (logDirty && logText != null)
            {
                logText.text = string.Join("\n", logLines);
                logDirty = false;
            }
        }

        #region 장치 상태

        private void UpdateDeviceStatus(bool keyboardConnected, bool mouseConnected)
        {
            if (lastKeyboardConnected == keyboardConnected && lastMouseConnected == mouseConnected) return;

            lastKeyboardConnected = keyboardConnected;
            lastMouseConnected = mouseConnected;

            if (deviceStatusText == null) return;

            deviceStatusText.text =
                $"Keyboard : {ConnectedLabel(keyboardConnected)}     Mouse : {ConnectedLabel(mouseConnected)}";
        }

        private static string ConnectedLabel(bool connected)
        {
            return connected ? "<color=#5CFF73>연결됨</color>" : "<color=#FF5555>없음 (SimpleInput 호출 시 예외)</color>";
        }

        #endregion

        #region 키보드

        private void UpdateKeyboard(Keyboard keyboard)
        {
            // 모든 키를 돌면서 SimpleInput 의 GetKeyDown / GetKeyUp 을 직접 호출합니다.
            foreach (KeyControl keyControl in keyboard.allKeys)
            {
                Key key = keyControl.keyCode;

                if (SimpleInput.GetKeyDown(key))
                {
                    lastDownKey = key;
                    lastDownFrame = Time.frameCount;
                    keyboardDirty = true;
                    AddLog($"<color=#5CFF73>Key Down</color>  {GetKeyName(key)}");
                }

                if (SimpleInput.GetKeyUp(key))
                {
                    lastUpKey = key;
                    lastUpFrame = Time.frameCount;
                    keyboardDirty = true;
                    AddLog($"<color=#FF9933>Key Up</color>    {GetKeyName(key)}");
                }
            }

            if (SimpleInput.AnyKeyDown())
            {
                anyKeyDownCount++;
                keyboardDirty = true;
            }

            // 누르고 있는 키 목록은 Down / Up 이 있을 때만 바뀌므로 그때만 다시 만듭니다.
            if (!keyboardDirty || keyboardText == null) return;
            keyboardDirty = false;

            builder.Clear();
            builder.Append("누르고 있는 키 (GetKey) : ");

            int heldCount = 0;
            foreach (KeyControl keyControl in keyboard.allKeys)
            {
                if (!SimpleInput.GetKey(keyControl.keyCode)) continue;

                if (heldCount > 0) builder.Append(" + ");
                builder.Append("<color=#66B3FF>").Append(GetKeyName(keyControl.keyCode)).Append("</color>");
                heldCount++;
            }
            if (heldCount == 0) builder.Append("<color=#777777>없음</color>");

            builder.Append("\n마지막 Down (GetKeyDown) : ").Append(FormatKeyFrame(lastDownKey, lastDownFrame));
            builder.Append("\n마지막 Up (GetKeyUp) : ").Append(FormatKeyFrame(lastUpKey, lastUpFrame));
            builder.Append("\nAnyKeyDown 누적 : ").Append(anyKeyDownCount);

            keyboardText.text = builder.ToString();
        }

        private string FormatKeyFrame(Key key, int frame)
        {
            if (key == Key.None) return "<color=#777777>-</color>";
            return $"{GetKeyName(key)}  <color=#888888>(frame {frame})</color>";
        }

        private string GetKeyName(Key key)
        {
            if (!keyNames.TryGetValue(key, out string keyName))
            {
                keyName = key.ToString();
                keyNames.Add(key, keyName);
            }
            return keyName;
        }

        #endregion

        #region 축

        private void UpdateAxis()
        {
            float horizontal = SimpleInput.GetAxisHorizontalRaw();
            float vertical = SimpleInput.GetAxisVerticalRaw();
            Vector2 move = SimpleInput.GetMoveAxisRaw();

            if (axisPad != null && axisDot != null)
            {
                axisDot.anchoredPosition = move * GetPadRadius(axisPad, axisDot);
            }

            if (move == lastMoveAxis || axisText == null) return;
            lastMoveAxis = move;

            axisText.text =
                $"GetAxisHorizontalRaw : {horizontal:0}\n" +
                $"GetAxisVerticalRaw : {vertical:0}\n" +
                $"GetMoveAxisRaw : ({move.x:0.00}, {move.y:0.00})\n" +
                $"길이 (magnitude) : {move.magnitude:0.00}";
        }

        #endregion

        #region 마우스

        private void UpdateMouse()
        {
            foreach (SimpleInput.MouseButton button in mouseButtons)
            {
                if (SimpleInput.GetMouseButtonDown(button))
                    AddLog($"<color=#5CFF73>Mouse Down</color>  {button}");

                if (SimpleInput.GetMouseButtonUp(button))
                    AddLog($"<color=#FF9933>Mouse Up</color>    {button}");
            }

            Vector2 position = SimpleInput.GetMousePosition();
            Vector2 delta = SimpleInput.GetMouseDelta();
            float scrollRaw = SimpleInput.GetMouseScrollDeltaRaw();
            float scrollStep = SimpleInput.GetMouseScrollDelta();

            if (!Mathf.Approximately(scrollStep, 0f))
            {
                lastScrollRaw = scrollRaw;
                lastScrollStep = scrollStep;
                lastScrollFrame = Time.frameCount;
                scrollStepSum += (int)scrollStep;
                AddLog($"<color=#FFE066>Scroll</color>  {scrollStep:+0;-0}  (raw {scrollRaw:0.###})");
            }

            UpdateCursorMarker(position);

            if (deltaPad != null && deltaDot != null && deltaVisualRange > 0f)
            {
                Vector2 normalized = Vector2.ClampMagnitude(delta / deltaVisualRange, 1f);
                deltaDot.anchoredPosition = normalized * GetPadRadius(deltaPad, deltaDot);
            }

            // 값이 바뀐 프레임에만 텍스트를 다시 만듭니다.
            if (position != lastMousePosition || delta != lastMouseDelta || scrollRaw != lastScrollRawThisFrame)
            {
                lastMousePosition = position;
                lastMouseDelta = delta;
                lastScrollRawThisFrame = scrollRaw;
                mouseDirty = true;
            }

            if (!mouseDirty) return;
            mouseDirty = false;

            if (cursorMarkerText != null)
                cursorMarkerText.text = $"({position.x:0}, {position.y:0})";

            if (mouseText == null) return;

            Vector2 viewport = new Vector2(
                Screen.width > 0 ? position.x / Screen.width : 0f,
                Screen.height > 0 ? position.y / Screen.height : 0f);

            mouseText.text =
                $"GetMousePosition : ({position.x:0}, {position.y:0}) px\n" +
                $"<color=#888888>  화면 {Screen.width} x {Screen.height} / 비율 ({viewport.x:0.00}, {viewport.y:0.00})</color>\n" +
                $"GetMouseDelta : ({delta.x:0.0}, {delta.y:0.0})\n" +
                $"GetMouseScrollDeltaRaw : {scrollRaw:0.###}\n" +
                $"GetMouseScrollDelta : {SignedStep(scrollStep)}\n" +
                $"<color=#888888>  마지막 휠 : {SignedStep(lastScrollStep)} (raw {lastScrollRaw:0.###}, frame {lastScrollFrame}) / 누적 {scrollStepSum:+0;-0;0}</color>";
        }

        private static string SignedStep(float step)
        {
            return step.ToString("+0;-0;0");
        }

        private void UpdateCursorMarker(Vector2 screenPosition)
        {
            if (cursorMarker == null || canvas == null) return;

            RectTransform parent = cursorMarker.parent as RectTransform;
            if (parent == null) return;

            // Overlay 캔버스는 카메라가 필요 없고, Camera / World 캔버스는 지정된 카메라를 사용합니다.
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPosition, eventCamera, out Vector3 worldPosition))
            {
                cursorMarker.position = worldPosition;
            }
        }

        #endregion

        #region 공용

        private void MarkAllDirty()
        {
            keyboardDirty = true;
            mouseDirty = true;
            lastMoveAxis = new Vector2(float.NaN, float.NaN);
        }

        // 정사각형 패드 안에서 점이 움직일 수 있는 최대 거리
        private static float GetPadRadius(RectTransform pad, RectTransform dot)
        {
            return Mathf.Max(0f, (pad.rect.width - dot.rect.width) * 0.5f);
        }

        private void AddLog(string message)
        {
            logLines.Enqueue($"<color=#888888>[{Time.frameCount}]</color>  {message}");

            while (logLines.Count > Mathf.Max(1, maxLogLines))
                logLines.Dequeue();

            logDirty = true;
        }

        [ContextMenu("Clear Log")]
        public void ClearLog()
        {
            logLines.Clear();
            logDirty = true;
        }

        #endregion
    }
}
