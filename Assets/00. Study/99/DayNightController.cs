using UnityEngine;

[ExecuteAlways]
public class DayNightController : MonoBehaviour
{
    [Header("라이트 설정")]
    [SerializeField] private Light directionalLight;

    [Header("시간 설정")]
    [Tooltip("하루(24시간)가 지난 데 걸리는 실제 시간(초)")]
    [SerializeField] private float dayDurationInSeconds = 120f; // 120초 = 하루

    [Range(0f, 24f)]
    [Tooltip("현재 게임 시간 (0시~24시)")]
    [SerializeField] private float currentTime = 6f; // 기본 아침 6시 시작

    [Header("시간별 라이트 연출")]
    [Tooltip("시간(0~24시)에 따른 태양 빛 색상")]
    [SerializeField] private Gradient lightColor;

    [Tooltip("시간(0~24시)에 따른 태양 빛 밝기 (Intensity)")]
    [SerializeField] private AnimationCurve lightIntensity;

    [Header("환경광 (Ambient Light) 연출")]
    [SerializeField] private bool updateAmbientLight = true;
    [SerializeField] private Gradient ambientColor;

    private void Start()
    {
        if (directionalLight == null)
        {
            directionalLight = GetComponent<Light>();
        }
    }

    private void Update()
    {
        // 플레이 모드일 때만 시간에 따라 진행
        if (Application.isPlaying)
        {
            currentTime += (Time.deltaTime / dayDurationInSeconds) * 24f;
            if (currentTime >= 24f)
            {
                currentTime %= 24f;
            }
        }

        UpdateSun();
    }

    private void UpdateSun()
    {
        if (directionalLight == null) return;

        // 0~1 비율 변환 (0시 = 0.0, 12시 = 0.5, 24시 = 1.0)
        float timePercent = currentTime / 24f;

        // 1. 태양 회전 각도 계산
        // 06:00 -> 0도 (지평선 일출)
        // 12:00 -> 90도 (남중 고도)
        // 18:00 -> 180도 (지평선 일몰)
        // 24:00 -> 270도 (한밤중)
        float sunAngle = (timePercent * 360f) - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // 2. 색상 (Color) 업데이트
        if (lightColor != null)
        {
            directionalLight.color = lightColor.Evaluate(timePercent);
        }

        // 3. 밝기 (Intensity) 업데이트
        if (lightIntensity != null && lightIntensity.length > 0)
        {
            directionalLight.intensity = lightIntensity.Evaluate(timePercent);
        }

        // 4. 주변 환경광 (Ambient Light) 업데이트
        if (updateAmbientLight && ambientColor != null)
        {
            RenderSettings.ambientLight = ambientColor.Evaluate(timePercent);
        }
    }

    // 컴포넌트 추가 시 기본값 자동 설정
    private void Reset()
    {
        directionalLight = GetComponent<Light>();
        SetupDefaultPresets();
    }

    [ContextMenu("기본 커브/그라데이션 재설정")]
    private void SetupDefaultPresets()
    {
        // 1. 디렉셔널 라이트 색상 기본값 설정
        lightColor = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        colorKeys[0] = new GradientColorKey(new Color(0.15f, 0.2f, 0.4f), 0.0f);   // 00시: 밤 (짙은 푸른빛)
        colorKeys[1] = new GradientColorKey(new Color(1.0f, 0.55f, 0.25f), 0.25f); // 06시: 아침 (주황빛)
        colorKeys[2] = new GradientColorKey(new Color(1.0f, 0.95f, 0.85f), 0.5f);  // 12시: 낮 (밝은 주광색)
        colorKeys[3] = new GradientColorKey(new Color(0.95f, 0.35f, 0.15f), 0.75f);// 18시: 저녁 (붉은 붉은 노을)
        colorKeys[4] = new GradientColorKey(new Color(0.15f, 0.2f, 0.4f), 1.0f);   // 24시: 밤

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

        lightColor.SetKeys(colorKeys, alphaKeys);

        // 2. 밝기(Intensity) 커브 설정 (밤: 0.1, 낮: 1.3)
        lightIntensity = new AnimationCurve(
            new Keyframe(0.0f, 0.1f),   // 00시
            new Keyframe(0.2f, 0.1f),   // 04시 48분
            new Keyframe(0.25f, 0.6f),  // 06시 (일출 시작)
            new Keyframe(0.5f, 1.3f),   // 12시 (정오 피크)
            new Keyframe(0.75f, 0.6f),  // 18시 (일몰)
            new Keyframe(0.8f, 0.1f),   // 19시 12분
            new Keyframe(1.0f, 0.1f)    // 24시
        );

        // 3. 환경광 색상 기본값
        ambientColor = new Gradient();
        GradientColorKey[] ambKeys = new GradientColorKey[5];
        ambKeys[0] = new GradientColorKey(new Color(0.02f, 0.04f, 0.1f), 0.0f);
        ambKeys[1] = new GradientColorKey(new Color(0.3f, 0.2f, 0.2f), 0.25f);
        ambKeys[2] = new GradientColorKey(new Color(0.45f, 0.45f, 0.5f), 0.5f);
        ambKeys[3] = new GradientColorKey(new Color(0.3f, 0.15f, 0.15f), 0.75f);
        ambKeys[4] = new GradientColorKey(new Color(0.02f, 0.04f, 0.1f), 1.0f);

        ambientColor.SetKeys(ambKeys, alphaKeys);
    }
}