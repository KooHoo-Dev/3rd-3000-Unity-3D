using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Study.Utilities
{
    // SimpleInput 의 입력 하나(키 / 마우스 버튼 / 휠 / AnyKey)를
    // 색상으로 보여주는 UI 칸(셀) 입니다.
    //  - 누르고 있는 동안 (GetKey)      : heldColor
    //  - 누른 그 프레임 (GetKeyDown)     : downColor 로 번쩍 → 서서히 원래 색으로
    //  - 뗀 그 프레임   (GetKeyUp)       : upColor 로 번쩍 → 서서히 원래 색으로
    // 셀을 복제해서 key 값만 바꾸면 테스트할 키를 얼마든지 늘릴 수 있습니다.
    public class SimpleInputKeyIndicator : MonoBehaviour
    {
        public enum Source
        {
            Keyboard,       // SimpleInput.GetKey / GetKeyDown / GetKeyUp
            MouseButton,    // SimpleInput.GetMouseButton / Down / Up
            ScrollUp,       // SimpleInput.GetMouseScrollDelta() > 0
            ScrollDown,     // SimpleInput.GetMouseScrollDelta() < 0
            AnyKey,         // SimpleInput.AnyKeyDown()
        }

        [Header("검사할 입력")]
        public Source source = Source.Keyboard;
        public Key key = Key.Space;
        public SimpleInput.MouseButton mouseButton = SimpleInput.MouseButton.Left;

        [Tooltip("비워두면 key / mouseButton 이름을 그대로 표시합니다.")]
        public string displayName;

        [Header("UI 참조")]
        public Image background;
        public TMP_Text label;

        [Header("색상")]
        public Color idleColor = new Color(0.20f, 0.22f, 0.26f, 1f);
        public Color heldColor = new Color(0.20f, 0.55f, 1.00f, 1f);
        public Color downColor = new Color(0.35f, 1.00f, 0.45f, 1f);
        public Color upColor = new Color(1.00f, 0.60f, 0.20f, 1f);
        public Color disconnectedColor = new Color(0.45f, 0.10f, 0.10f, 1f);
        public float flashDuration = 0.25f;

        private Color flashColor;
        private float flashTimer;

        // 디버거가 로그를 남길 때 사용할 수 있도록 공개해 둡니다.
        public bool IsHeld { get; private set; }
        public int DownCount { get; private set; }

        private void Reset()
        {
            background = GetComponent<Image>();
            label = GetComponentInChildren<TMP_Text>();
        }

        private void Awake()
        {
            // 셀을 복제해서 key 만 바꿔도 실행 시 라벨이 자동으로 맞춰집니다.
            if (label != null)
                label.text = GetDisplayName();
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(displayName)) return displayName;

            switch (source)
            {
                case Source.Keyboard: return key.ToString();
                case Source.MouseButton: return mouseButton.ToString();
                case Source.ScrollUp: return "Scroll +";
                case Source.ScrollDown: return "Scroll -";
                case Source.AnyKey: return "AnyKeyDown";
            }

            return name;
        }

        private void Update()
        {
            if (background == null) return;

            if (!TryReadInput(out bool down, out bool up, out bool held))
            {
                IsHeld = false;
                background.color = disconnectedColor;
                return;
            }

            IsHeld = held;

            if (down)
            {
                DownCount++;
                Flash(downColor);
            }
            if (up)
            {
                Flash(upColor);
            }

            Color baseColor = held ? heldColor : idleColor;

            if (flashTimer > 0f)
            {
                // 디버그 UI 는 Time.timeScale = 0 (일시정지) 에서도 동작해야 하므로
                // unscaledDeltaTime 을 사용합니다.
                flashTimer -= Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(flashTimer / flashDuration);
                background.color = Color.Lerp(baseColor, flashColor, t);
            }
            else
            {
                background.color = baseColor;
            }
        }

        private void Flash(Color color)
        {
            flashColor = color;
            flashTimer = flashDuration;
        }

        // SimpleInput 은 장치가 없으면(Keyboard.current == null) 예외가 나므로
        // 장치 연결 여부를 먼저 확인한 뒤 호출합니다.
        private bool TryReadInput(out bool down, out bool up, out bool held)
        {
            down = up = held = false;

            switch (source)
            {
                case Source.Keyboard:
                    if (Keyboard.current == null || key == Key.None) return false;
                    down = SimpleInput.GetKeyDown(key);
                    up = SimpleInput.GetKeyUp(key);
                    held = SimpleInput.GetKey(key);
                    return true;

                case Source.MouseButton:
                    if (Mouse.current == null) return false;
                    down = SimpleInput.GetMouseButtonDown(mouseButton);
                    up = SimpleInput.GetMouseButtonUp(mouseButton);
                    held = SimpleInput.GetMouseButton(mouseButton);
                    return true;

                case Source.ScrollUp:
                    if (Mouse.current == null) return false;
                    down = SimpleInput.GetMouseScrollDelta() > 0f;
                    return true;

                case Source.ScrollDown:
                    if (Mouse.current == null) return false;
                    down = SimpleInput.GetMouseScrollDelta() < 0f;
                    return true;

                case Source.AnyKey:
                    if (Keyboard.current == null) return false;
                    down = SimpleInput.AnyKeyDown();
                    return true;
            }

            return false;
        }
    }
}
