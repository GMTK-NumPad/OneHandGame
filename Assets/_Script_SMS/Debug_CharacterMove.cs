using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Debug_CharacterMove : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("크기를 변경할 대상 오브젝트를 넣으세요. 비워두면 이 스크립트가 붙은 오브젝트가 대상이 됩니다.")]
    public Transform targetTransform;

    [Header("Settings")]
    public float animationDuration = 0.5f; // 애니메이션 진행 시간

    [Header("Anim Graph (Animation Curve)")]
    public AnimationCurve scaleCurve; // 크기 변화 그래프

    private Vector3 originalScale; // 대상의 원래 스케일을 저장할 변수
    private Coroutine scaleCoroutine; // 코루틴 중복 실행 방지용

    void Start()
    {
        // 타겟이 지정되지 않았다면, 스크립트가 붙어있는 오브젝트를 타겟으로 자동 설정
        if (targetTransform == null)
        {
            targetTransform = this.transform;
        }

        // 대상 오브젝트의 현재 스케일을 기본값으로 저장
        originalScale = targetTransform.localScale;
    }

    void Update()
    {
        // 키보드 P 버튼이 이번 프레임에 눌렸는지 확인 (New Input System)
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            // 스케일 변경 애니메이션 실행 (기존에 실행 중이면 멈추고 새로 시작)
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(AnimateScale());
        }

        if(Keyboard.current!=null&&Keyboard.current.oKey.wasPressedThisFrame)
        {
            gameObject.transform.position=new Vector3(gameObject.transform.position.x+1,gameObject.transform.position.y,gameObject.transform.position.z);
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(AnimateScale());
        }
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            gameObject.transform.position = new Vector3(gameObject.transform.position.x - 1, gameObject.transform.position.y, gameObject.transform.position.z);
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

            // 기존 스케일에 그래프 값을 곱해서 적용
            targetTransform.localScale = originalScale * curveValue;

            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 끝나면 원래 스케일로 정확히 되돌림
        targetTransform.localScale = originalScale;
    }
}
