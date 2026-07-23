using System;
using UnityEngine;

/// <summary>
/// 한 번의 몬스터 피해 처리에서 실제 피해량과 이번 공격의 처치 여부를 보관합니다.
/// </summary>
public readonly struct EnemyDamageResult
{
    /// <summary>
    /// 실제 피해량과 이번 피해로 처치되었는지를 이용해 결과를 생성합니다.
    /// </summary>
    public EnemyDamageResult(
        int appliedDamage,
        bool didDefeat)
    {
        AppliedDamage = appliedDamage;
        DidDefeat = didDefeat;
    }

    public int AppliedDamage { get; }
    public bool DidDefeat { get; }
}

/// <summary>
/// 씬에 생성된 몬스터 오브젝트와 보드 위치, 개별 런타임 상태를 연결합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoardActor))]
public sealed class EnemyActor : MonoBehaviour
{
    private BoardActor boardActor;
    private EnemyActionAnimator actionAnimator;

    public BoardActor BoardActor => GetBoardActor();
    public EnemyActionAnimator ActionAnimator =>
        GetActionAnimator();
    public EnemyDefinition Definition { get; private set; }
    public EnemyRuntimeState RuntimeState { get; private set; }
    public bool IsInitialized => RuntimeState != null;
    public bool IsDefeated =>
        IsInitialized && RuntimeState.IsDefeated;

    public event Action<EnemyActor, int> Damaged;
    public event Action<EnemyActor> Defeated;

    /// <summary>
    /// 같은 GameObject에 있는 보드 액터 참조를 미리 가져옵니다.
    /// </summary>
    private void Awake()
    {
        boardActor = GetComponent<BoardActor>();
        actionAnimator = GetActionAnimator();
    }

    /// <summary>
    /// 몬스터 정의와 스폰 시 결정된 골드 보상으로 개별 런타임 상태를 생성합니다.
    /// </summary>
    public bool Initialize(
        EnemyDefinition definition,
        GoldRewardResult goldReward)
    {
        if (definition == null)
        {
            Debug.LogError(
                "EnemyActor requires an EnemyDefinition.",
                this);
            return false;
        }

        if (IsInitialized)
        {
            Debug.LogWarning(
                "EnemyActor is already initialized.",
                this);
            return false;
        }

        Definition = definition;
        RuntimeState = definition.CreateRuntimeState(
            gameObject.GetInstanceID(),
            goldReward);
        return true;
    }

    /// <summary>
    /// 몬스터 런타임 체력에 피해를 적용하고 처음 처치된 순간 보드에서 제거합니다.
    /// </summary>
    public EnemyDamageResult TakeDamage(int amount)
    {
        if (!IsInitialized || IsDefeated)
        {
            return new EnemyDamageResult(
                appliedDamage: 0,
                didDefeat: false);
        }

        int appliedDamage = RuntimeState.TakeDamage(amount);

        if (appliedDamage > 0)
        {
            Damaged?.Invoke(this, appliedDamage);
        }

        bool didDefeat = RuntimeState.IsDefeated;

        if (didDefeat)
        {
            BoardActor.RemoveFromBoard();
            Defeated?.Invoke(this);
        }

        return new EnemyDamageResult(
            appliedDamage,
            didDefeat);
    }

    /// <summary>
    /// 캐시가 비어 있다면 같은 GameObject에서 BoardActor를 다시 찾아 반환합니다.
    /// </summary>
    private BoardActor GetBoardActor()
    {
        if (boardActor == null)
        {
            boardActor = GetComponent<BoardActor>();
        }

        return boardActor;
    }

    /// <summary>
    /// 기존 프리팹에도 기본 행동 연출을 적용할 수 있도록 컴포넌트를 찾거나 자동으로 추가합니다.
    /// </summary>
    private EnemyActionAnimator GetActionAnimator()
    {
        if (actionAnimator == null)
        {
            actionAnimator =
                GetComponent<EnemyActionAnimator>();

            if (actionAnimator == null)
            {
                actionAnimator =
                    gameObject.AddComponent<EnemyActionAnimator>();
            }
        }

        return actionAnimator;
    }
}
