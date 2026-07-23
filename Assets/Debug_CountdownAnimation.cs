using System.Collections;
using TMPro;
using UnityEngine;

public class Debug_CountdownAnimation : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI textMesh; // 변경할 텍스트 컴포넌트
    private RectTransform rectTransform;

    [Header("Settings")]
    public int targetNumber = 100; // P를 눌렀을 때 바뀔 숫자
    public float animationDuration = 0.5f; // 애니메이션이 진행될 시간 (초)

    [Header("Anim Graph (Animation Curve)")]
    // 이 그래프의 값(Y축)에 기존 Scale(1, 0.8)이 곱해집니다.
    public AnimationCurve scaleCurve;

    private Vector3 originalScale; // 원래 스케일 (X:1, Y:0.8)을 저장할 변수
    private Coroutine scaleCoroutine; // 코루틴 중복 실행 방지용

    void Start()
    {
        // RectTransform 컴포넌트를 가져옵니다.
        if (textMesh != null)
        {
            rectTransform = textMesh.GetComponent<RectTransform>();
            // 현재 설정된 스케일(X=1, Y=0.8, Z=1)을 기본값으로 저장해둡니다.
            originalScale = rectTransform.localScale;
        }
    }

    void Update()
    {
        // 키보드 P 버튼을 눌렀을 때
        if (Input.GetKeyDown(KeyCode.P))
        {
            // 1. 텍스트 값을 내가 정한 변수(TargetNumber)로 변경
            textMesh.text = targetNumber.ToString();

            // 2. 스케일 변경 애니메이션 실행 (기존에 실행중이면 멈추고 새로 시작)
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(AnimateScale());
        }
    }

    // 시간에 따라 스케일을 변경하는 코루틴
    private IEnumerator AnimateScale()
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            // 진행도(0.0 ~ 1.0) 계산
            float progress = elapsedTime / animationDuration;

            // Animation Curve 그래프에서 현재 진행도에 해당하는 값을 가져옴
            float curveValue = scaleCurve.Evaluate(progress);

            // 기존 스케일(1, 0.8)에 그래프 값을 곱해서 적용
            rectTransform.localScale = originalScale * curveValue;

            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 끝나면 원래 스케일로 정확히 되돌림
        rectTransform.localScale = originalScale;
    }
}
