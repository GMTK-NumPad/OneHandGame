using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 스테이지 진행 컨트롤러가 현재 기다리고 있는 팝업 또는 전환 상태를 나타냅니다.
/// </summary>
public enum StageProgressionWaitState
{
    None,
    Shop,
    SkillSelection,
    StartingNextRound,
    Completed,
    ContentExhausted
}

/// <summary>
/// UI에 전달할 현재 스테이지와 스테이지 내부 라운드 위치를 보관합니다.
/// </summary>
public readonly struct StageProgressionSnapshot
{
    /// <summary>
    /// 현재 스테이지, 라운드와 클리어 후 전환 계획을 하나의 값으로 생성합니다.
    /// </summary>
    public StageProgressionSnapshot(
        GameStageId stageId,
        int roundInStage,
        StageTransitionPlan transitionPlan)
    {
        StageId = stageId;
        RoundInStage = roundInStage;
        TransitionPlan = transitionPlan;
    }

    public GameStageId StageId { get; }
    public int RoundInStage { get; }
    public StageTransitionPlan TransitionPlan { get; }
}

/// <summary>
/// 진행 SO에 따라 튜토리얼부터 스테이지 C까지 라운드를 생성하고 상점과 스킬 선택 순서를 제어합니다.
/// </summary>
public sealed class StageProgressionController
    : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField]
    private GameProgressionDefinition progressionDefinition =
        null;
    [SerializeField]
    private bool autoContinueWithoutUiListener = true;

    [Header("Combat")]
    [SerializeField] private PlayerSpawner playerSpawner = null;
    [SerializeField] private EnemySpawner enemySpawner = null;
    [SerializeField]
    private EnvironmentTileSpawner environmentTileSpawner =
        null;
    [SerializeField]
    private EnvironmentTileEffectController environmentTileEffects =
        null;
    [SerializeField]
    private GoldWalletController goldWalletController = null;

    private RoundFlowStateMachine roundFlow;
    private PlayerRuntimeStats playerStats;
    private StageTransitionPlan pendingPlan;
    private Coroutine nextRoundCoroutine;
    private int stageIndex;
    private int roundIndex;

    public event Action<StageProgressionSnapshot>
        RoundStarted;
    public event Action<StageProgressionSnapshot>
        ShopRequested;
    public event Action<StageProgressionSnapshot>
        SkillSelectionRequested;
    public event Action<RunResultSnapshot, RoundResolution>
        RunEnded;
    public event Action ContentExhausted;

    public StageProgressionWaitState WaitState
    {
        get;
        private set;
    }

    public GameStageId CurrentStageId
    {
        get
        {
            return TryGetCurrentStage(
                out StageDefinition stage)
                    ? stage.StageId
                    : GameStageId.Tutorial;
        }
    }

    public int CurrentRoundInStage =>
        roundIndex + 1;

    /// <summary>
    /// 컴포넌트를 추가할 때 같은 GameObject의 전투 진행 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        playerSpawner = GetComponent<PlayerSpawner>();
        enemySpawner = GetComponent<EnemySpawner>();
        environmentTileSpawner =
            GetComponent<EnvironmentTileSpawner>();
        environmentTileEffects =
            GetComponent<EnvironmentTileEffectController>();
        goldWalletController =
            GetComponent<GoldWalletController>();
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 라운드 이벤트와 예약된 다음 라운드 시작을 정리합니다.
    /// </summary>
    private void OnDisable()
    {
        if (nextRoundCoroutine != null)
        {
            StopCoroutine(nextRoundCoroutine);
            nextRoundCoroutine = null;
        }

        UnsubscribeRoundFlow();
    }

    /// <summary>
    /// 새 게임의 진행 위치를 초기화하고 튜토리얼 첫 라운드를 생성해 시작한 뒤 플레이어 능력치를 반환합니다.
    /// </summary>
    public bool TryStartRun(
        RoundFlowStateMachine flow,
        out PlayerRuntimeStats startedPlayerStats)
    {
        startedPlayerStats = null;

        if (flow == null
            || !ValidateDependencies())
        {
            return false;
        }

        UnsubscribeRoundFlow();
        roundFlow = flow;
        stageIndex = 0;
        roundIndex = 0;
        WaitState = StageProgressionWaitState.None;
        roundFlow.RoundCleared += HandleRoundCleared;
        roundFlow.StateChanged += HandleRoundStateChanged;

        if (!TryPrepareCurrentRound(
                out int enemyCount))
        {
            UnsubscribeRoundFlow();
            return false;
        }

        roundFlow.StartFirstRound(
            enemyCount,
            IsCurrentRoundRunFinale());
        startedPlayerStats = playerStats;
        RoundStarted?.Invoke(CreateSnapshot(default));
        return true;
    }

    /// <summary>
    /// 실제 상점 팝업이 닫혔음을 알리고 필요한 다음 팝업이나 라운드로 진행합니다.
    /// </summary>
    public bool CompleteShop()
    {
        if (WaitState != StageProgressionWaitState.Shop)
        {
            return false;
        }

        if (pendingPlan.RequiresSkillSelection)
        {
            RequestSkillSelection();
        }
        else
        {
            FinishPendingTransition();
        }

        return true;
    }

    /// <summary>
    /// 스킬 선택이 완료됐음을 알리고 다음 스테이지 또는 게임 결과로 진행합니다.
    /// </summary>
    public bool CompleteSkillSelection()
    {
        if (WaitState
            != StageProgressionWaitState.SkillSelection)
        {
            return false;
        }

        FinishPendingTransition();
        return true;
    }

    /// <summary>
    /// 라운드 종료 재생성, 라운드 전용 능력치 정리와 팝업 순서를 결정합니다.
    /// </summary>
    private void HandleRoundCleared(
        RoundClearReward reward)
    {
        if (reward.OverflowGold > 0)
        {
            goldWalletController.AddGold(
                reward.OverflowGold);
        }

        playerStats.ApplyRoundEndRegeneration();
        playerStats.ClearRoundAttackPower();

        if (!TryGetCurrentStage(
                out StageDefinition stage))
        {
            SetContentExhausted();
            return;
        }

        pendingPlan =
            StageProgressionCalculator.CreatePlan(
                stage.StageId,
                CurrentRoundInStage,
                stage.RoundCount);

        if (pendingPlan.RequiresShop)
        {
            RequestShop();
            return;
        }

        if (pendingPlan.RequiresSkillSelection)
        {
            RequestSkillSelection();
            return;
        }

        FinishPendingTransition();
    }

    /// <summary>
    /// 패배 또는 게임 클리어로 라운드 흐름이 종료되면 결과 화면용 누적 통계를 전달합니다.
    /// </summary>
    private void HandleRoundStateChanged(
        RoundFlowSnapshot snapshot)
    {
        if (snapshot.Phase != RoundPhase.RunEnded)
        {
            return;
        }

        WaitState = StageProgressionWaitState.Completed;
        RunEnded?.Invoke(
            CreateRunResult(),
            snapshot.Resolution);
    }

    /// <summary>
    /// 상점 팝업을 요청하고 연결된 UI가 없는 테스트 상태에서는 설정에 따라 자동으로 닫습니다.
    /// </summary>
    private void RequestShop()
    {
        WaitState = StageProgressionWaitState.Shop;
        Action<StageProgressionSnapshot> handler =
            ShopRequested;
        handler?.Invoke(CreateSnapshot(pendingPlan));

        if (handler == null
            && autoContinueWithoutUiListener)
        {
            CompleteShop();
        }
    }

    /// <summary>
    /// 스킬 선택 팝업을 요청하고 연결된 UI가 없는 테스트 상태에서는 설정에 따라 자동 완료합니다.
    /// </summary>
    private void RequestSkillSelection()
    {
        WaitState =
            StageProgressionWaitState.SkillSelection;
        Action<StageProgressionSnapshot> handler =
            SkillSelectionRequested;
        handler?.Invoke(CreateSnapshot(pendingPlan));

        if (handler == null
            && autoContinueWithoutUiListener)
        {
            CompleteSkillSelection();
        }
    }

    /// <summary>
    /// 마지막 스테이지라면 게임을 끝내고 아니면 다음 라운드 시작을 예약합니다.
    /// </summary>
    private void FinishPendingTransition()
    {
        if (!TryAdvanceProgression(
                out bool isFirstRoundOfStage))
        {
            SetContentExhausted();
            return;
        }

        WaitState =
            StageProgressionWaitState.StartingNextRound;
        nextRoundCoroutine =
            StartCoroutine(
                StartNextRoundAfterFrame(
                    isFirstRoundOfStage));
    }

    /// <summary>
    /// 마지막 몬스터 제거가 끝난 다음 프레임에 새 보드 액터와 환경을 준비하고 다음 라운드를 시작합니다.
    /// </summary>
    private IEnumerator StartNextRoundAfterFrame(
        bool isFirstRoundOfStage)
    {
        yield return null;
        nextRoundCoroutine = null;

        if (roundFlow == null
            || roundFlow.Phase
                != RoundPhase.BetweenRounds
            || !TryPrepareCurrentRound(
                out int enemyCount))
        {
            SetContentExhausted();
            yield break;
        }

        if (!roundFlow.StartNextRound(
                enemyCount,
                isFirstRoundOfStage,
                IsCurrentRoundRunFinale()))
        {
            Debug.LogError(
                "Could not start the prepared next round.",
                this);
            SetContentExhausted();
            yield break;
        }

        WaitState = StageProgressionWaitState.None;
        pendingPlan = default;
        RoundStarted?.Invoke(CreateSnapshot(default));
    }

    /// <summary>
    /// 현재 라운드 SO에 맞춰 플레이어를 중앙에 놓고 고정 몬스터와 환경 타일을 새로 생성합니다.
    /// </summary>
    private bool TryPrepareCurrentRound(
        out int enemyCount)
    {
        enemyCount = 0;

        if (!TryGetCurrentRound(
                out RoundDefinition round))
        {
            Debug.LogError(
                "The current stage round definition is missing.",
                this);
            return false;
        }

        EnvironmentTileSpawnRulesDefinition
            environmentRules =
                round.EnvironmentTileSpawnRules;

        if (environmentRules == null)
        {
            Debug.LogError(
                "RoundDefinition requires Environment Tile Spawn Rules.",
                round);
            return false;
        }

        enemySpawner.ClearSpawnedEnemies();
        BoardActor player =
            playerSpawner.SpawnOrResetPlayer();

        if (player == null)
        {
            return false;
        }

        PlayerStatsController statsController =
            player.GetComponent<PlayerStatsController>();

        if (statsController == null
            || statsController.RuntimeStats == null)
        {
            Debug.LogError(
                "The spawned player requires initialized PlayerStatsController runtime stats.",
                player);
            return false;
        }

        playerStats ??= statsController.RuntimeStats;
        enemySpawner.SetPlayerActor(player);
        environmentTileSpawner.SetSpawnRules(
            environmentRules);
        environmentTileEffects.SetSpawnRules(
            environmentRules);

        if (!enemySpawner.TrySpawnFixedEnemies(
                round.FixedEnemySpawns)
            || !environmentTileSpawner
                .GenerateEnvironmentTiles())
        {
            return false;
        }

        playerStats.ResetGuardForRound();
        enemyCount =
            enemySpawner.SpawnedEnemies.Count;
        return enemyCount > 0;
    }

    /// <summary>
    /// 현재 스테이지 안의 다음 라운드 또는 다음 스테이지의 첫 라운드로 진행 위치를 옮깁니다.
    /// </summary>
    private bool TryAdvanceProgression(
        out bool isFirstRoundOfStage)
    {
        isFirstRoundOfStage = false;

        if (!TryGetCurrentStage(
                out StageDefinition stage))
        {
            return false;
        }

        if (roundIndex + 1 < stage.RoundCount)
        {
            roundIndex++;
            return true;
        }

        stageIndex++;
        roundIndex = 0;
        isFirstRoundOfStage = true;
        return TryGetCurrentStage(out _);
    }

    /// <summary>
    /// 현재 진행 인덱스에 해당하는 유효한 스테이지 SO를 반환합니다.
    /// </summary>
    private bool TryGetCurrentStage(
        out StageDefinition stage)
    {
        stage = null;
        return progressionDefinition != null
            && progressionDefinition.TryGetStage(
                stageIndex,
                out stage);
    }

    /// <summary>
    /// 현재 진행 인덱스에 해당하는 라운드 SO를 반환합니다.
    /// </summary>
    private bool TryGetCurrentRound(
        out RoundDefinition round)
    {
        round = null;

        if (!TryGetCurrentStage(
                out StageDefinition stage)
            || roundIndex < 0
            || roundIndex >= stage.RoundCount)
        {
            return false;
        }

        round = stage.Rounds[roundIndex];
        return round != null;
    }

    /// <summary>
    /// 현재 라운드가 상점과 보상 정산 없이 바로 게임 클리어 결과로 끝나야 하는 C 스테이지의 마지막 라운드인지 확인합니다.
    /// </summary>
    private bool IsCurrentRoundRunFinale()
    {
        return TryGetCurrentStage(
                out StageDefinition stage)
            && stage.StageId == GameStageId.C
            && roundIndex == stage.RoundCount - 1;
    }

    /// <summary>
    /// UI 이벤트에 사용할 현재 스테이지 진행 정보를 생성합니다.
    /// </summary>
    private StageProgressionSnapshot CreateSnapshot(
        StageTransitionPlan transitionPlan)
    {
        return new StageProgressionSnapshot(
            CurrentStageId,
            CurrentRoundInStage,
            transitionPlan);
    }

    /// <summary>
    /// 결과 화면에 필요한 전체 진행 턴, 보너스, 피해와 획득 골드를 생성합니다.
    /// </summary>
    private RunResultSnapshot CreateRunResult()
    {
        return new RunResultSnapshot(
            roundFlow != null
                ? roundFlow.TotalTurnsPlayed
                : 0,
            roundFlow != null
                ? roundFlow.TotalBonusCountdownEarned
                : 0,
            roundFlow != null
                ? roundFlow.TotalDamageTaken
                : 0,
            goldWalletController != null
                ? goldWalletController
                    .TotalGoldEarned
                : 0);
    }

    /// <summary>
    /// 다음 스테이지 데이터가 없는 상태로 전환하고 외부 시스템에 데이터 추가 필요를 알립니다.
    /// </summary>
    private void SetContentExhausted()
    {
        WaitState =
            StageProgressionWaitState.ContentExhausted;
        Debug.LogWarning(
            "The next stage is not configured yet. Progression has stopped after the last valid stage.",
            this);
        ContentExhausted?.Invoke();
    }

    /// <summary>
    /// 스테이지 진행에 필요한 SO와 생성 및 골드 컴포넌트가 모두 연결되었는지 검사합니다.
    /// </summary>
    private bool ValidateDependencies()
    {
        if (progressionDefinition == null)
        {
            Debug.LogError(
                "StageProgressionController requires GameProgressionDefinition.",
                this);
            return false;
        }

        if (!progressionDefinition.IsValidSequence())
        {
            Debug.LogError(
                "GameProgression must contain non-null stages in Tutorial-A-B-C prefix order.",
                progressionDefinition);
            return false;
        }

        if (playerSpawner == null
            || enemySpawner == null
            || environmentTileSpawner == null
            || environmentTileEffects == null
            || goldWalletController == null)
        {
            Debug.LogError(
                "StageProgressionController requires PlayerSpawner, EnemySpawner, EnvironmentTileSpawner, EnvironmentTileEffectController, and GoldWalletController.",
                this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 현재 라운드 흐름에 등록한 클리어와 상태 변경 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeRoundFlow()
    {
        if (roundFlow != null)
        {
            roundFlow.RoundCleared -= HandleRoundCleared;
            roundFlow.StateChanged -=
                HandleRoundStateChanged;
        }

        roundFlow = null;
        playerStats = null;
    }
}
