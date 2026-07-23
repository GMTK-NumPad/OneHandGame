using System.Collections;
using UnityEngine;

/// <summary>
/// 숫자패드 방향 입력을 플레이어 이동 또는 공격으로 실행하고 라운드 상태를 넘깁니다.
/// </summary>
public sealed class PlayerTurnController : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private NumpadInputController inputController = null;
    [SerializeField] private CombatSceneBootstrap combatBootstrap = null;
    [SerializeField] private PlayerSpawner playerSpawner = null;

    private BoardActor playerActor;
    private PlayerStatsController playerStatsController;
    private RoundFlowStateMachine subscribedRoundFlow;
    private Coroutine stunnedTurnCoroutine;
    private bool isResolvingAction;

    public BoardActor PlayerActor => playerActor;
    public PlayerRuntimeStats PlayerStats =>
        playerStatsController != null
            ? playerStatsController.RuntimeStats
            : null;

    /// <summary>
    /// 컴포넌트를 처음 추가할 때 같은 GameObject와 씬에서 필요한 참조를 찾아 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
        combatBootstrap = GetComponent<CombatSceneBootstrap>();
        playerSpawner = GetComponent<PlayerSpawner>();
        inputController =
            FindFirstObjectByType<NumpadInputController>();
    }

    /// <summary>
    /// 방향 입력과 전투 초기화 완료 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        if (inputController != null)
        {
            inputController.DirectionPressed +=
                HandleDirectionPressed;
        }

        if (combatBootstrap != null)
        {
            combatBootstrap.CombatInitialized +=
                HandleCombatInitialized;
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 등록했던 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (inputController != null)
        {
            inputController.DirectionPressed -=
                HandleDirectionPressed;
        }

        if (combatBootstrap != null)
        {
            combatBootstrap.CombatInitialized -=
                HandleCombatInitialized;
        }

        if (stunnedTurnCoroutine != null)
        {
            StopCoroutine(stunnedTurnCoroutine);
            stunnedTurnCoroutine = null;
        }

        UnsubscribeRoundFlow();
    }

    /// <summary>
    /// 다른 컴포넌트보다 늦게 시작했더라도 이미 생성된 플레이어 참조를 연결합니다.
    /// </summary>
    private void Start()
    {
        if (combatBootstrap != null
            && combatBootstrap.IsInitialized)
        {
            BindSpawnedPlayer();
            SubscribeRoundFlow();
        }
    }

    /// <summary>
    /// 한 번의 8방향 입력을 이동 또는 공격으로 실행하고 행동이 성립했는지 반환합니다.
    /// </summary>
    public bool TryPerformDirectionAction(
        GridPosition direction)
    {
        if (isResolvingAction
            || !CanAcceptPlayerAction())
        {
            return false;
        }

        PlayerRuntimeStats stats =
            playerStatsController.RuntimeStats;
        PlayerMovementPlan plan =
            PlayerMovementPlanner.CreatePlan(
                boardManager,
                playerActor.GetInstanceID(),
                playerActor.Position,
                direction,
                stats.MoveRange,
                stats.RampageDistance);

        if (!plan.CanAct)
        {
            return false;
        }

        isResolvingAction = true;

        try
        {
            if (plan.ShouldMove
                && !playerActor.TryMove(plan.Destination))
            {
                Debug.LogError(
                    $"Player movement failed from {plan.Start} to {plan.Destination}.",
                    this);
                return false;
            }

            if (plan.ShouldAttack)
            {
                ExecuteAttack(plan, stats.AttackPower);
            }

            RoundFlowStateMachine roundFlow =
                combatBootstrap.RoundFlow;

            if (roundFlow.Phase == RoundPhase.PlayerTurn)
            {
                roundFlow.CompletePlayerTurn();
            }

            return true;
        }
        finally
        {
            isResolvingAction = false;
        }
    }

    /// <summary>
    /// 숫자패드에서 받은 방향을 플레이어 행동 실행 메서드로 전달합니다.
    /// </summary>
    private void HandleDirectionPressed(
        GridPosition direction)
    {
        TryPerformDirectionAction(direction);
    }

    /// <summary>
    /// 전투 초기화가 끝나면 생성된 플레이어와 능력치 컴포넌트를 연결합니다.
    /// </summary>
    private void HandleCombatInitialized()
    {
        BindSpawnedPlayer();
        SubscribeRoundFlow();
    }

    /// <summary>
    /// PlayerSpawner가 생성한 보드 액터와 같은 GameObject의 능력치 컴포넌트를 가져옵니다.
    /// </summary>
    private void BindSpawnedPlayer()
    {
        playerActor =
            playerSpawner != null
                ? playerSpawner.SpawnedPlayer
                : null;

        playerStatsController =
            playerActor != null
                ? playerActor.GetComponent<PlayerStatsController>()
                : null;

        if (playerActor == null
            || playerStatsController == null
            || playerStatsController.RuntimeStats == null)
        {
            Debug.LogError(
                "PlayerTurnController could not bind the spawned player and runtime stats.",
                this);
        }
    }

    /// <summary>
    /// 현재 전투의 라운드 상태 변경을 구독하고 이미 시작된 플레이어 턴도 확인합니다.
    /// </summary>
    private void SubscribeRoundFlow()
    {
        RoundFlowStateMachine roundFlow =
            combatBootstrap != null
                ? combatBootstrap.RoundFlow
                : null;

        if (ReferenceEquals(
                subscribedRoundFlow,
                roundFlow))
        {
            return;
        }

        UnsubscribeRoundFlow();
        subscribedRoundFlow = roundFlow;

        if (subscribedRoundFlow == null)
        {
            return;
        }

        subscribedRoundFlow.StateChanged +=
            HandleRoundStateChanged;
        HandleRoundStateChanged(
            subscribedRoundFlow.Snapshot);
    }

    /// <summary>
    /// 현재 구독 중인 라운드 상태 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeRoundFlow()
    {
        if (subscribedRoundFlow == null)
        {
            return;
        }

        subscribedRoundFlow.StateChanged -=
            HandleRoundStateChanged;
        subscribedRoundFlow = null;
    }

    /// <summary>
    /// 기절한 상태로 플레이어 턴이 시작되면 다음 프레임에 해당 턴을 건너뛰도록 예약합니다.
    /// </summary>
    private void HandleRoundStateChanged(
        RoundFlowSnapshot snapshot)
    {
        if (snapshot.Phase != RoundPhase.PlayerTurn
            || PlayerStats == null
            || !PlayerStats.IsStunned
            || stunnedTurnCoroutine != null)
        {
            return;
        }

        stunnedTurnCoroutine =
            StartCoroutine(SkipStunnedPlayerTurn());
    }

    /// <summary>
    /// 이전 몬스터 턴 코루틴이 끝난 뒤 기절 1턴을 소비하고 플레이어 행동 없이 몬스터 턴으로 전환합니다.
    /// </summary>
    private IEnumerator SkipStunnedPlayerTurn()
    {
        yield return null;

        try
        {
            PlayerRuntimeStats stats = PlayerStats;

            if (subscribedRoundFlow == null
                || subscribedRoundFlow.Phase
                    != RoundPhase.PlayerTurn
                || stats == null
                || !stats.TryConsumeStunnedTurn())
            {
                yield break;
            }

            subscribedRoundFlow.CompletePlayerTurn();
        }
        finally
        {
            stunnedTurnCoroutine = null;
        }
    }

    /// <summary>
    /// 공격 계획에 기록된 보드 액터를 확인하고 몬스터에게 플레이어 공격력을 적용합니다.
    /// </summary>
    private void ExecuteAttack(
        PlayerMovementPlan plan,
        int attackPower)
    {
        if (!boardManager.TryGetActor(
                plan.AttackTargetPosition,
                out BoardActor targetActor)
            || targetActor.GetInstanceID()
                != plan.AttackTargetActorId)
        {
            Debug.LogError(
                $"Player attack target is no longer at {plan.AttackTargetPosition}.",
                this);
            return;
        }

        EnemyActor enemy = targetActor.GetComponent<EnemyActor>();

        if (enemy == null)
        {
            Debug.LogError(
                "Player attack target does not have an EnemyActor.",
                targetActor);
            return;
        }

        EnemyDamageResult damageResult =
            enemy.TakeDamage(attackPower);

        if (damageResult.DidDefeat)
        {
            combatBootstrap.RoundFlow.ReportEnemyDefeated();
        }
    }

    /// <summary>
    /// 필요한 참조와 런타임 상태가 준비되었고 현재 단계가 플레이어 턴인지 확인합니다.
    /// </summary>
    private bool CanAcceptPlayerAction()
    {
        if (boardManager == null
            || inputController == null
            || combatBootstrap == null
            || playerSpawner == null
            || !combatBootstrap.IsInitialized
            || combatBootstrap.RoundFlow == null
            || combatBootstrap.RoundFlow.Phase
                != RoundPhase.PlayerTurn
            || playerActor == null
            || !playerActor.IsPlaced
            || playerStatsController == null
            || playerStatsController.RuntimeStats == null
            || playerStatsController.RuntimeStats.IsDefeated
            || playerStatsController.RuntimeStats.IsStunned)
        {
            return false;
        }

        return true;
    }
}
