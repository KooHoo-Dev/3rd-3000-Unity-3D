using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.FpsGame
{
    public class Crosshair : MonoBehaviour
    {
        [Header("조준선 막대")]
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;

        [Header("벌어짐")]
        [SerializeField] private float defaultSpread = 20.0f;
        [SerializeField] private float maxSpread = 50.0f;
        [SerializeField] private float spreadSpeed = 5.0f;

        private float currentSpread;

        private void Start()
        {
            currentSpread = defaultSpread;
            UpdateCrosshairPosition();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.isPressed)
            {
                currentSpread = maxSpread;
            }

            currentSpread = Mathf.Lerp(currentSpread, defaultSpread, spreadSpeed * Time.deltaTime);
            UpdateCrosshairPosition();
        }

        private void UpdateCrosshairPosition()
        {
            top.anchoredPosition = new Vector2(0, currentSpread);
            bottom.anchoredPosition = new Vector2(0, -currentSpread);

            left.anchoredPosition = new Vector2(-currentSpread, 0);
            right.anchoredPosition = new Vector2(currentSpread, 0);
        }
    }
}
