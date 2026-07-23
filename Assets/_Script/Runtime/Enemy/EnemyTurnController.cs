using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 턴에 Speed 순서대로 일반 공격 또는 기본 이동을 실행하고 턴 후 상태를 처리합니다.
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
    /// 한 몬스터의 일반 공격 또는 기본 이동을 실행하고 해당 행동 연출이 끝날 때까지 기다립니다.
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

        bool playerInActionRange =
            state.IsInActionRange(
                enemy.BoardActor.Position,
                playerActor.Position);

        if (playerInActionRange)
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

            if (enemy.BoardActor.TryMove(nextStep))
            {
                yield return enemy.ActionAnimator.PlayMove(
                    startWorld,
                    enemy.transform.position);
            }
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
