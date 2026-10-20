using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Study3D.FpsGame.UI
{
    // 3차원 좌표를 활용해서 스크린 좌표를 계산 한 후
    // 스크린의 캔버스(Canvas)에서 애니메이팅(떠오르는) 되는 UI입니다
    public class FloatingText : MonoBehaviour, IObjectPoolItem
    {
        [Tooltip("초당 위로 움직이는 거리. 단위는 m")]
        [SerializeField] private float riseSpeed = 2.0f;

        // UI관련 멤버 변수
        private Canvas canvas;
        private TMP_Text text;
        private RectTransform rectTransform;
        
        // 좌표계 변환을 위한 멤버 변수
        private Camera mainCamera;
        private Vector3 worldPosition;
        private float currentDuration;

        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            rectTransform = GetComponent<RectTransform>();
            
            mainCamera = Camera.main;
            canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (currentDuration < 0)
            {
                ReturnToPool();
                return;
            }
            currentDuration -= Time.deltaTime;
            
            // worldPosition 점을 이동시키면서 위로 상승하는 효과를 줄겁니다.
            // 월드 포지션을 기반으로 (riseSpeed * Time.deltaTime) 썼기 때문에
            // 실제로 움직이는 거리는 월드 기준 초당 riseSpeed 만큼 입니다. 
            worldPosition += Vector3.up * (riseSpeed * Time.deltaTime);
            
            // worldPosition을 screen좌표계로 바꿔줍니다.
            // Camera.WorldToScreenPoint(Vector3 position) 함수를 활용합니다.
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            // 스크린 좌표계에서 z값이 0보다 크다면, 카메라 앞에 있다는 뜻. 렌더링합니다.
            // 스크린 좌표계에서 z값이 0보다 작다면, 카메라 뒤에 있다는 뜻. 렌더링하지 않습니다.
            text.enabled = (screenPoint.z > 0);

            // 스크린 좌표계로 전환된 값을 캔버스(UI) 좌표로 전환합니다.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPoint, mainCamera, 
                out Vector2 localPoint);

            //나의 rectTransform의 anchor위치에 해당 좌표를 적용해 줍니다.
            rectTransform.anchoredPosition = localPoint;
        }

        // 외부에서 Set에 출력할 메시지를 전달하는 것으로 동작 합니다.
        public void Set(Vector3 position, string content, float duration, Color color)
        {
            worldPosition = position;
            text.SetText(content);
            currentDuration = duration;
            text.color = color;
        }

        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(this);
        }
    }
}