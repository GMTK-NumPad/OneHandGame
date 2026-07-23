using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 몬스터의 논리 위치와 별도로 이동과 일반 공격을 순차적으로 보여주는 기본 연출을 재생합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyActionAnimator : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visualRoot = null;
    [SerializeField] private SpriteRenderer spriteRenderer = null;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveDuration = 0.05f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.2f;
    [SerializeField, Range(0f, 30f)] private float moveTiltAngle = 8f;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackDuration = 0.06f;
    [SerializeField, Min(0f)] private float attackLungeDistance = 0.2f;
    [SerializeField, Range(0f, 30f)] private float attackTiltAngle = 6f;

    /// <summary>
    /// 컴포넌트를 추가할 때 Visual 자식과 그 아래 SpriteRenderer를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        FindVisualReferences();
    }

    /// <summary>
    /// 프리팹에 참조가 지정되지 않은 경우 사용할 시각 오브젝트를 찾습니다.
    /// </summary>
    private void Awake()
    {
        FindVisualReferences();
    }

    /// <summary>
    /// 보드의 논리 이동이 끝난 뒤 시각 오브젝트를 이전 위치에서 새 위치로 점프시킵니다.
    /// </summary>
    public IEnumerator PlayMove(
        Vector3 actorStartWorld,
        Vector3 actorEndWorld)
    {
        FindVisualReferences();
        UpdateFacing(actorEndWorld.x - actorStartWorld.x);

        if (visualRoot == null
            || moveDuration <= 0f
            || !isActiveAndEnabled)
        {
            yield break;
        }

        Vector3 originalLocalPosition =
            visualRoot.localPosition;
        Quaternion originalLocalRotation =
            visualRoot.localRotation;
        Vector3 endVisualWorld = visualRoot.position;
        Vector3 startVisualWorld =
            endVisualWorld
            + actorStartWorld
            - actorEndWorld;
        float tiltDirection =
            actorEndWorld.x < actorStartWorld.x
                ? 1f
                : -1f;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(
                elapsedTime / moveDuration);
            Vector3 position = Vector3.Lerp(
                startVisualWorld,
                endVisualWorld,
                progress);
            position.y +=
                Mathf.Sin(progress * Mathf.PI)
                * jumpHeight;
            visualRoot.position = position;
            visualRoot.localRotation =
                originalLocalRotation
                * Quaternion.Euler(
                    0f,
                    0f,
                    tiltDirection
                    * moveTiltAngle
                    * Mathf.Sin(progress * Mathf.PI));
            yield return null;
        }

        visualRoot.localPosition = originalLocalPosition;
        visualRoot.localRotation = originalLocalRotation;
    }

    /// <summary>
    /// 플레이어 방향으로 짧게 돌진하고 연출의 가장 앞 지점에서 실제 공격 처리를 호출합니다.
    /// </summary>
    public IEnumerator PlayAttack(
        Vector3 targetWorld,
        Action applyImpact)
    {
        FindVisualReferences();

        float horizontalDirection =
            targetWorld.x - transform.position.x;
        UpdateFacing(horizontalDirection);

        if (visualRoot == null
            || attackDuration <= 0f
            || !isActiveAndEnabled)
        {
            applyImpact?.Invoke();
            yield break;
        }

        Vector3 originalLocalPosition =
            visualRoot.localPosition;
        Quaternion originalLocalRotation =
            visualRoot.localRotation;
        Vector3 originalWorldPosition =
            visualRoot.position;
        Vector3 attackDirection =
            targetWorld - transform.position;
        attackDirection.z = 0f;
        attackDirection =
            attackDirection.sqrMagnitude > 0f
                ? attackDirection.normalized
                : Vector3.right;
        float tiltDirection =
            horizontalDirection < 0f ? 1f : -1f;
        float elapsedTime = 0f;
        bool impactApplied = false;

        while (elapsedTime < attackDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(
                elapsedTime / attackDuration);
            float lungeProgress =
                progress <= 0.5f
                    ? progress * 2f
                    : (1f - progress) * 2f;

            visualRoot.position =
                originalWorldPosition
                + attackDirection
                * attackLungeDistance
                * lungeProgress;
            visualRoot.localRotation =
                originalLocalRotation
                * Quaternion.Euler(
                    0f,
                    0f,
                    tiltDirection
                    * attackTiltAngle
                    * lungeProgress);

            if (!impactApplied && progress >= 0.5f)
            {
                impactApplied = true;
                applyImpact?.Invoke();
            }

            yield return null;
        }

        if (!impactApplied)
        {
            applyImpact?.Invoke();
        }

        visualRoot.localPosition = originalLocalPosition;
        visualRoot.localRotation = originalLocalRotation;
    }

    /// <summary>
    /// Visual 자식을 우선 사용하고 없으면 SpriteRenderer가 있는 Transform 또는 루트 Transform을 사용합니다.
    /// </summary>
    private void FindVisualReferences()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
        }

        if (spriteRenderer == null)
        {
            Transform searchRoot =
                visualRoot != null
                    ? visualRoot
                    : transform;
            spriteRenderer =
                searchRoot.GetComponentInChildren<SpriteRenderer>();
        }

        if (visualRoot == null)
        {
            visualRoot =
                spriteRenderer != null
                    ? spriteRenderer.transform
                    : transform;
        }
    }

    /// <summary>
    /// 기본 방향인 오른쪽을 기준으로 수평 이동 또는 공격 방향에 맞춰 스프라이트를 뒤집습니다.
    /// </summary>
    private void UpdateFacing(float horizontalDirection)
    {
        if (spriteRenderer == null
            || Mathf.Approximately(
                horizontalDirection,
                0f))
        {
            return;
        }

        spriteRenderer.flipX = horizontalDirection < 0f;
    }
}
