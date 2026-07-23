using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 턴에 Speed 순서대로 캐스팅, 일반 공격 또는 기본 이동을 실행하고 턴 후 상태를 처리합니다.
/// </summary>
public sealed class EnemyTurnController : MonoBehaviour
{
    [SerializeField] private CombatSceneBootstrap combatBootstrap = null;
    [SerializeField] private EnemySpawner enemySpawner = null;
    [SerializeField] private PlayerSpawner playerSpawner = null;
    [SerializeField, Min(0f)] private float delayBetweenEnemies = 0.02f;

    private RoundFlowStateMachine subscribedRoundFlow;
    private Coroutine enemyTurnCoroutine;
    private bool isResolvingTurn;

    public event Action<EnemyActor> EnemyActionCompleted;
    public event Action<EnemyActor> EnemyCastingStarted;
    public event Action<EnemyActor, int, int> EnemyCastingProgressed;
    public event Action<EnemyActor, GridPosition> EnemyCastingReleased;

    /// <summary>
    /// 컴포넌트를 처음 추가할 때 같은 GameObject의 전투 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        combatBootstrap =
            GetComponent<CombatSceneBootstrap>();
        enemySpawner = GetComponent<EnemySpawner>();
        playerSpawner = GetComponent<PlayerSpawner>();
    }

    /// <summary>
    /// 전투 초기화 완료 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        if (combatBootstrap != null)
        {
            combatBootstrap.CombatInitialized +=
                HandleCombatInitialized;
        }
    }

    /// <summary>
    /// 전투 및 라운드 상태 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (combatBootstrap != null)
        {
            combatBootstrap.CombatInitialized -=
                HandleCombatInitialized;
        }

        if (enemyTurnCoroutine != null)
        {
            StopCoroutine(enemyTurnCoroutine);
            enemyTurnCoroutine = null;
            isResolvingTurn = false;
        }

        UnsubscribeRoundFlow();
    }

    /// <summary>
    /// 초기화 완료 이벤트를 놓친 실행 순서에서도 현재 라운드 상태를 연결합니다.
    /// </summary>
    private void Start()
    {
        if (combatBootstrap != null
            && combatBootstrap.IsInitialized)
        {
            SubscribeRoundFlow();
        }
    }

    /// <summary>
    /// 전투 초기화가 완료되면 이번 실행의 라운드 상태를 구독합니다.
    /// </summary>
    private void HandleCombatInitialized()
    {
        SubscribeRoundFlow();
    }

    /// <summary>
    /// 라운드가 몬스터 턴으로 전환되었을 때 전체 몬스터 행동을 실행합니다.
    /// </summary>
    private void HandleRoundStateChanged(
        RoundFlowSnapshot snapshot)
    {
        if (snapshot.Phase == RoundPhase.EnemyTurn)
        {
            StartEnemyTurn();
        }
    }

    /// <summary>
    /// 중복 실행을 막고 몬스터 순차 행동 코루틴을 시작합니다.
    /// </summary>
    private void StartEnemyTurn()
    {
        if (isResolvingTurn)
        {
            return;
        }

        enemyTurnCoroutine =
            StartCoroutine(ResolveEnemyTurn());
    }

    /// <summary>
    /// 살아 있는 몬스터를 정렬한 뒤 한 마리의 연출이 끝날 때마다 다음 몬스터를 행동시킵니다.
    /// </summary>
    private IEnumerator ResolveEnemyTurn()
    {
        if (isResolvingTurn
            || !TryGetCombatActors(
                out BoardActor playerActor,
                out PlayerRuntimeStats playerStats)
            || subscribedRoundFlow.Phase
                != RoundPhase.EnemyTurn)
        {
            yield break;
        }

        isResolvingTurn = true;

        try
        {
            Dictionary<int, EnemyActor> actorsById =
                CreateEnemyLookup();
            List<EnemyRuntimeState> orderedEnemies =
                EnemyTurnOrder.Create(
                    CreateRuntimeStateList(actorsById));

            foreach (EnemyRuntimeState enemyState in orderedEnemies)
            {
                if (subscribedRoundFlow.Phase
                    != RoundPhase.EnemyTurn)
                {
                    break;
                }

                if (!actorsById.TryGetValue(
                        enemyState.InstanceId,
                        out EnemyActor enemy)
                    || enemy == null
                    || !enemy.BoardActor.IsPlaced)
                {
                    continue;
                }

                yield return ResolveEnemyAction(
                    enemy,
                    enemyState,
                    playerActor,
                    playerStats);
                EnemyActionCompleted?.Invoke(enemy);

                if (delayBetweenEnemies > 0f
                    && subscribedRoundFlow.Phase
                        == RoundPhase.EnemyTurn)
                {
                    yield return new WaitForSeconds(
                        delayBetweenEnemies);
                }
            }

            if (subscribedRoundFlow.Phase
                != RoundPhase.EnemyTurn)
            {
                yield break;
            }

            AdvanceTimedStates(playerStats);
            subscribedRoundFlow.CompleteEnemyTurn();
        }
        finally
        {
            isResolvingTurn = false;
            enemyTurnCoroutine = null;
        }
    }

    /// <summary>
    /// 한 몬스터의 캐스팅, 일반 공격 또는 기본 이동을 실행하고 해당 행동 연출이 끝날 때까지 기다립니다.
    /// </summary>
    private IEnumerator ResolveEnemyAction(
        EnemyActor enemy,
        EnemyRuntimeState state,
        BoardActor playerActor,
        PlayerRuntimeStats playerStats)
    {
        if (state.IsDefeated || state.IsStunned)
        {
            yield break;
        }

        if (state.IsCasting)
        {
            yield return ResolveCastingAction(
                enemy,
                state,
                playerActor,
                playerStats);
            yield break;
        }

        state.FaceTowards(
            enemy.BoardActor.Position,
            playerActor.Position);

        bool castingConditionMet =
            state.IsCastingConditionMet(
                enemy.BoardActor.Position,
                playerActor.Position);

        if (state.CanAttackOrCast
            && castingConditionMet
            && state.TryBeginCasting(
                playerActor.Position))
        {
            EnemyCastingStarted?.Invoke(enemy);
            yield return enemy.ActionAnimator
                .PlayCastingStart();
            yield break;
        }

        if (state.Definition.IsCaster
            && castingConditionMet)
        {
            yield break;
        }

        bool playerInActionRange =
            state.IsInActionRange(
                enemy.BoardActor.Position,
                playerActor.Position);

        if (!state.Definition.IsCaster
            && playerInActionRange)
        {
            if (state.CanAttackOrCast)
            {
                bool impactApplied = false;
                Action applyImpact = () =>
                {
                    if (impactApplied)
                    {
                        return;
                    }

                    impactApplied = true;
                    ApplyEnemyAttack(
                        state,
                        playerStats);
                };

                yield return enemy.ActionAnimator.PlayAttack(
                    playerActor.transform.position,
                    applyImpact);

                applyImpact();
            }

            yield break;
        }

        if (state.Definition.HasCustomMovementPattern)
        {
            yield break;
        }

        if (EnemyPathfinder.TryGetNextStep(
                enemy.BoardActor.BoardManager,
                enemy.BoardActor.GetInstanceID(),
                enemy.BoardActor.Position,
                playerActor.Position,
                out GridPosition nextStep))
        {
            Vector3 startWorld =
                enemy.transform.position;
            GridPosition startPosition =
                enemy.BoardActor.Position;
            state.SetFacingDirection(
                new GridPosition(
                    nextStep.X - startPosition.X,
                    nextStep.Y - startPosition.Y));

            if (enemy.BoardActor.TryMove(nextStep))
            {
                yield return enemy.ActionAnimator.PlayMove(
                    startWorld,
                    enemy.transform.position);
            }
        }
    }

    /// <summary>
    /// 진행 중인 캐스팅을 올리고 완료됐다면 처음 저장한 타일에 제자리 공격을 발동합니다.
    /// </summary>
    private IEnumerator ResolveCastingAction(
        EnemyActor enemy,
        EnemyRuntimeState state,
        BoardActor playerActor,
        PlayerRuntimeStats playerStats)
    {
        bool isReadyToRelease =
            state.AdvanceCasting();
        EnemyCastingProgressed?.Invoke(
            enemy,
            state.CurrentCastingProgress,
            state.Definition.RequiredCastingProgress);

        if (!isReadyToRelease)
        {
            yield return enemy.ActionAnimator
                .PlayCastingProgress();
            yield break;
        }

        GridPosition fixedTarget =
            state.CastingTarget;
        EnemyCastingReleased?.Invoke(
            enemy,
            fixedTarget);
        bool impactApplied = false;
        Action applyImpact = () =>
        {
            if (impactApplied)
            {
                return;
            }

            impactApplied = true;
            ApplyCastingEffects(
                state,
                fixedTarget,
                playerActor,
                playerStats);
        };

        yield return enemy.ActionAnimator
            .PlayCastingRelease(applyImpact);
        applyImpact();
    }

    /// <summary>
    /// 캐스팅을 완료하고 대상 지정 방식에 따라 효과 목록을 플레이어 또는 범위 내 액터에게 적용합니다.
    /// </summary>
    private void ApplyCastingEffects(
        EnemyRuntimeState state,
        GridPosition fixedTarget,
        BoardActor playerActor,
        PlayerRuntimeStats playerStats)
    {
        state.CompleteCastingAction();
        EnemyDefinition definition = state.Definition;

        if (definition.CastingTargetType
            == EnemyCastingTargetType.DirectPlayer)
        {
            ApplyCastingEffectsToPlayer(
                definition.CastingEffects,
                playerStats);
            ReportPlayerDefeatIfNeeded(playerStats);
            return;
        }

        bool playerInImpactRange =
            playerActor != null
            && playerActor.IsPlaced
            && EnemyRangeCalculator.IsInCastingImpactRange(
                fixedTarget,
                playerActor.Position,
                definition.CastingImpactShape,
                definition.CastingImpactRange,
                definition.CastingImpactOffsets);

        if (playerInImpactRange)
        {
            ApplyCastingEffectsToPlayer(
                definition.CastingEffects,
                playerStats);
        }

        int defeatedEnemyCount =
            ApplyCastingEffectsToEnemies(
                fixedTarget,
                definition);

        if (playerStats.IsDefeated)
        {
            subscribedRoundFlow.ReportPlayerDefeated();
            return;
        }

        if (defeatedEnemyCount > 0)
        {
            subscribedRoundFlow.ReportEnemyDefeated(
                defeatedEnemyCount);
        }
    }

    /// <summary>
    /// 캐스팅 효과 목록을 플레이어에게 순서대로 적용하며 패배하면 이후 효과 적용을 중단합니다.
    /// </summary>
    private static void ApplyCastingEffectsToPlayer(
        IReadOnlyList<EnemyCastingEffectData> effects,
        PlayerRuntimeStats playerStats)
    {
        if (effects == null || playerStats == null)
        {
            return;
        }

        foreach (EnemyCastingEffectData effect in effects)
        {
            if (playerStats.IsDefeated)
            {
                break;
            }

            if (effect == null)
            {
                continue;
            }

            switch (effect.EffectType)
            {
                case EnemyCastingEffectType.Stun:
                    playerStats.AddStunTurns(
                        effect.DurationTurns);
                    break;

                default:
                    playerStats.TakeAttackDamage(
                        effect.Amount);
                    break;
            }
        }
    }

    /// <summary>
    /// 범위 안의 몬스터에게 캐스팅 효과 목록을 순서대로 적용하고 이번 발동의 처치 수를 반환합니다.
    /// </summary>
    private int ApplyCastingEffectsToEnemies(
        GridPosition fixedTarget,
        EnemyDefinition definition)
    {
        var targets = new List<EnemyActor>(
            enemySpawner.SpawnedEnemies);
        int defeatedEnemyCount = 0;
        IReadOnlyList<EnemyCastingEffectData> effects =
            definition.CastingEffects;

        if (effects == null)
        {
            return 0;
        }

        foreach (EnemyActor target in targets)
        {
            if (target == null
                || !target.IsInitialized
                || target.IsDefeated
                || !target.BoardActor.IsPlaced
                || !EnemyRangeCalculator.IsInCastingImpactRange(
                    fixedTarget,
                    target.BoardActor.Position,
                    definition.CastingImpactShape,
                    definition.CastingImpactRange,
                    definition.CastingImpactOffsets))
            {
                continue;
            }

            foreach (EnemyCastingEffectData effect in effects)
            {
                if (target.IsDefeated)
                {
                    break;
                }

                if (effect == null)
                {
                    continue;
                }

                switch (effect.EffectType)
                {
                    case EnemyCastingEffectType.Stun:
                        target.RuntimeState.ApplyStun(
                            effect.DurationTurns);
                        break;

                    default:
                        target.TakeDamage(
                            effect.Amount);
                        break;
                }
            }

            if (target.IsDefeated)
            {
                defeatedEnemyCount++;
            }
        }

        return defeatedEnemyCount;
    }

    /// <summary>
    /// 플레이어 체력이 모두 소진되었다면 현재 전투 흐름에 패배를 보고합니다.
    /// </summary>
    private void ReportPlayerDefeatIfNeeded(
        PlayerRuntimeStats playerStats)
    {
        if (playerStats.IsDefeated)
        {
            subscribedRoundFlow.ReportPlayerDefeated();
        }
    }

    /// <summary>
    /// 일반 공격 연출의 타격 시점에 피해와 행동 쿨다운을 한 번만 적용합니다.
    /// </summary>
    private void ApplyEnemyAttack(
        EnemyRuntimeState state,
        PlayerRuntimeStats playerStats)
    {
        playerStats.TakeAttackDamage(
            state.Definition.AttackPower);
        state.CompleteAttackAction();

        if (playerStats.IsDefeated)
        {
            subscribedRoundFlow.ReportPlayerDefeated();
        }
    }

    /// <summary>
    /// 몬스터 쿨다운과 기절, 플레이어 무적 시간을 카운트 감소 직전에 1씩 줄입니다.
    /// </summary>
    private void AdvanceTimedStates(
        PlayerRuntimeStats playerStats)
    {
        foreach (EnemyActor enemy in enemySpawner.SpawnedEnemies)
        {
            if (enemy == null
                || !enemy.IsInitialized
                || enemy.IsDefeated)
            {
                continue;
            }

            enemy.RuntimeState.AdvanceCooldown();
            enemy.RuntimeState.AdvanceStun();
        }

        playerStats.AdvanceInvincibleTurn();
    }

    /// <summary>
    /// 현재 생성되어 살아 있는 몬스터를 Instance ID로 찾을 수 있는 사전으로 복사합니다.
    /// </summary>
    private Dictionary<int, EnemyActor> CreateEnemyLookup()
    {
        var actorsById =
            new Dictionary<int, EnemyActor>();

        foreach (EnemyActor enemy in enemySpawner.SpawnedEnemies)
        {
            if (enemy == null
                || !enemy.IsInitialized
                || enemy.IsDefeated)
            {
                continue;
            }

            actorsById[enemy.RuntimeState.InstanceId] =
                enemy;
        }

        return actorsById;
    }

    /// <summary>
    /// 정렬기에 전달할 몬스터 런타임 상태 목록을 생성합니다.
    /// </summary>
    private static List<EnemyRuntimeState>
        CreateRuntimeStateList(
            IReadOnlyDictionary<int, EnemyActor> actorsById)
    {
        var states = new List<EnemyRuntimeState>(
            actorsById.Count);

        foreach (EnemyActor enemy in actorsById.Values)
        {
            states.Add(enemy.RuntimeState);
        }

        return states;
    }

    /// <summary>
    /// 현재 플레이어 보드 액터와 런타임 능력치가 전투 가능한 상태인지 확인합니다.
    /// </summary>
    private bool TryGetCombatActors(
        out BoardActor playerActor,
        out PlayerRuntimeStats playerStats)
    {
        playerActor =
            playerSpawner != null
                ? playerSpawner.SpawnedPlayer
                : null;
        PlayerStatsController statsController =
            playerActor != null
                ? playerActor.GetComponent<PlayerStatsController>()
                : null;
        playerStats =
            statsController != null
                ? statsController.RuntimeStats
                : null;

        return combatBootstrap != null
            && enemySpawner != null
            && subscribedRoundFlow != null
            && playerActor != null
            && playerActor.IsPlaced
            && playerStats != null
            && !playerStats.IsDefeated;
    }

    /// <summary>
    /// 현재 CombatSceneBootstrap의 RoundFlow를 상태 변경 이벤트에 연결합니다.
    /// </summary>
    private void SubscribeRoundFlow()
    {
        UnsubscribeRoundFlow();
        subscribedRoundFlow =
            combatBootstrap != null
                ? combatBootstrap.RoundFlow
                : null;

        if (subscribedRoundFlow != null)
        {
            subscribedRoundFlow.StateChanged +=
                HandleRoundStateChanged;
        }
    }

    /// <summary>
    /// 이전에 연결한 RoundFlow 상태 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeRoundFlow()
    {
        if (subscribedRoundFlow != null)
        {
            subscribedRoundFlow.StateChanged -=
                HandleRoundStateChanged;
            subscribedRoundFlow = null;
        }
    }
}
