using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // 새로운 Input System을 사용하기 위한 네임스페이스 추가!

public class TMP_ScaleAnimator : MonoBehaviour
{
    [Header("Components")]
    public TextMeshPro textMesh;
    private RectTransform rectTransform;

    [Header("Settings")]
    public int targetNumber = 100;
    public float animationDuration = 0.5f;

    [Header("Anim Graph (Animation Curve)")]
    public AnimationCurve scaleCurve;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    void Start()
    {
        if (textMesh != null)
        {
            rectTransform = textMesh.GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
        }
    }

    void Update()
    {
        // 🚨 변경된 부분: 새로운 Input System 방식의 키보드 입력 감지
        // 키보드가 연결되어 있는지 확인(null 체크)한 후, P키가 이번 프레임에 눌렸는지 확인합니다.
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            textMesh.text = targetNumber.ToString();

            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(AnimateScale());
        }
    }

    private IEnumerator AnimateScale()
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = elapsedTime / animationDuration;
            float curveValue = scaleCurve.Evaluate(progress);

            rectTransform.localScale = originalScale * curveValue;

            yield return null;
        }

        rectTransform.localScale = originalScale;
    }
}