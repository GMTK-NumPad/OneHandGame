using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP 숫자를 변경하고 설정된 곡선에 따라 크기 애니메이션을 재생합니다.
/// </summary>
public sealed class TMP_ScaleAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TMP_Text textMesh = null;

    [Header("Settings")]
    [SerializeField] private int targetNumber = 100;
    [SerializeField, Min(0f)] private float animationDuration = 0.5f;

    [Header("Anim Graph")]
    [SerializeField] private AnimationCurve scaleCurve = null;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    public int TargetNumber => targetNumber;

    /// <summary>
    /// TMP와 RectTransform 참조 및 원래 크기를 준비합니다.
    /// </summary>
    private void Awake()
    {
        InitializeReferences();
    }

    /// <summary>
    /// 숫자를 즉시 갱신하고 요청된 경우 크기 애니메이션을 처음부터 재생합니다.
    /// </summary>
    public void SetNumber(
        int number,
        bool animate = true)
    {
        InitializeReferences();
        targetNumber = number;

        if (textMesh == null)
        {
            Debug.LogError(
                "TMP_ScaleAnimator requires a TMP_Text.",
                this);
            return;
        }

        textMesh.SetText(number.ToString());

        if (!animate
            || !isActiveAndEnabled
            || rectTransform == null
            || animationDuration <= 0f)
        {
            ResetScale();
            return;
        }

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }

        ResetScale();
        scaleCoroutine =
            StartCoroutine(AnimateScale());
    }

    /// <summary>
    /// 비활성화될 때 진행 중인 애니메이션 상태를 원래 크기로 복원합니다.
    /// </summary>
    private void OnDisable()
    {
        scaleCoroutine = null;
        ResetScale();
    }

    /// <summary>
    /// 지정된 TMP를 찾고 크기를 변경할 RectTransform과 원래 크기를 저장합니다.
    /// </summary>
    private void InitializeReferences()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TMP_Text>();
        }

        if (textMesh == null)
        {
            return;
        }

        RectTransform currentRectTransform =
            textMesh.rectTransform;

        if (rectTransform == currentRectTransform)
        {
            return;
        }

        rectTransform = currentRectTransform;
        originalScale = rectTransform.localScale;
    }

    /// <summary>
    /// 설정된 시간 동안 AnimationCurve의 배율을 TMP 원래 크기에 적용합니다.
    /// </summary>
    private IEnumerator AnimateScale()
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration);
            float curveValue =
                scaleCurve != null
                    ? scaleCurve.Evaluate(progress)
                    : 1f;
            rectTransform.localScale =
                originalScale * curveValue;
            yield return null;
        }

        ResetScale();
        scaleCoroutine = null;
    }

    /// <summary>
    /// TMP RectTransform을 애니메이션 시작 전 원래 크기로 되돌립니다.
    /// </summary>
    private void ResetScale()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }
}
