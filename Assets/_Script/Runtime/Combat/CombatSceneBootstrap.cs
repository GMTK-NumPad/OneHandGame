using System;
using UnityEngine;

/// <summary>
/// 전투 씬의 보드와 플레이어를 초기화하고 스테이지 진행 컨트롤러를 통해 첫 라운드를 시작합니다.
/// </summary>
public sealed class CombatSceneBootstrap : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField]
    private StageProgressionController stageProgressionController =
        null;
    [SerializeField] private RoundRulesDefinition roundRules = null;
    [SerializeField] private bool initializeOnStart = true;

    private PlayerRuntimeStats subscribedPlayerStats;

    public event Action CombatInitialized;
    public RoundFlowStateMachine RoundFlow { get; private set; }
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 컴포넌트를 처음 추가했을 때 같은 GameObject의 관련 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
        stageProgressionController =
            GetComponent<StageProgressionController>();
    }

    /// <summary>
    /// 자동 초기화가 켜져 있으면 모든 Awake 호출 이후 전투 씬 초기화를 실행합니다.
    /// </summary>
    private void Start()
    {
        if (initializeOnStart)
        {
            TryInitializeCombat();
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 플레이어 피해 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeRuntimeEvents();
    }

    /// <summary>
    /// 보드, 플레이어, 라운드 상태와 스테이지 진행을 순서대로 한 번만 초기화합니다.
    /// </summary>
    public bool TryInitializeCombat()
    {
        if (IsInitialized)
        {
            Debug.LogWarning(
                "Combat scene is already initialized.",
                this);
            return false;
        }

        if (stageProgressionController == null)
        {
            stageProgressionController =
                GetComponent<StageProgressionController>();
        }

        if (!ValidateDependencies())
        {
            return false;
        }

        boardManager.RebuildBoard();
        RoundFlow = roundRules.CreateStateMachine();

        if (!stageProgressionController.TryStartRun(
                RoundFlow,
                out PlayerRuntimeStats playerStats))
        {
            RoundFlow = null;
            return false;
        }

        SubscribeRuntimeEvents(playerStats);
        IsInitialized = true;
        CombatInitialized?.Invoke();
        return true;
    }

    /// <summary>
    /// 전투 씬 초기화에 필요한 모든 컴포넌트와 라운드 규칙이 지정되어 있는지 확인합니다.
    /// </summary>
    private bool ValidateDependencies()
    {
        if (boardManager == null
            || stageProgressionController == null
            || roundRules == null)
        {
            Debug.LogError(
                "CombatSceneBootstrap requires BoardManager, StageProgressionController, and RoundRulesDefinition.",
                this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 실제 소모품 사용이 성공했을 때 이번 라운드의 소모품 사용 기록을 남깁니다.
    /// </summary>
    public bool ReportConsumableUsed()
    {
        return RoundFlow != null
            && RoundFlow.ReportConsumableUsed();
    }

    /// <summary>
    /// 생성된 플레이어의 실제 피해 이벤트를 현재 전투 흐름에 연결합니다.
    /// </summary>
    private void SubscribeRuntimeEvents(
        PlayerRuntimeStats stats)
    {
        UnsubscribeRuntimeEvents();
        subscribedPlayerStats = stats;

        if (subscribedPlayerStats != null)
        {
            subscribedPlayerStats.HealthDamaged +=
                HandlePlayerHealthDamaged;
        }

    }

    /// <summary>
    /// 이전 플레이어 능력치에 등록한 피해 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeRuntimeEvents()
    {
        if (subscribedPlayerStats != null)
        {
            subscribedPlayerStats.HealthDamaged -=
                HandlePlayerHealthDamaged;
            subscribedPlayerStats = null;
        }
    }

    /// <summary>
    /// 방어 후 실제로 감소한 체력을 현재 라운드와 결과 통계에 전달합니다.
    /// </summary>
    private void HandlePlayerHealthDamaged(int amount)
    {
        RoundFlow?.ReportPlayerDamageTaken(amount);
    }

}
