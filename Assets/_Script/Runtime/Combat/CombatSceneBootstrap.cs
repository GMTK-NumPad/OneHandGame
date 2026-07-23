using System;
using UnityEngine;

/// <summary>
/// 전투 씬의 보드를 초기화하고 플레이어와 고정 몬스터를 생성한 뒤 첫 라운드를 시작합니다.
/// </summary>
public sealed class CombatSceneBootstrap : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private PlayerSpawner playerSpawner = null;
    [SerializeField] private EnemySpawner enemySpawner = null;
    [SerializeField] private RoundRulesDefinition roundRules = null;
    [SerializeField] private bool initializeOnStart = true;

    public event Action CombatInitialized;
    public RoundFlowStateMachine RoundFlow { get; private set; }
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 컴포넌트를 처음 추가했을 때 같은 GameObject의 관련 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
        playerSpawner = GetComponent<PlayerSpawner>();
        enemySpawner = GetComponent<EnemySpawner>();
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
    /// 보드, 플레이어, 몬스터와 라운드 상태를 순서대로 한 번만 초기화합니다.
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

        if (!ValidateDependencies())
        {
            return false;
        }

        enemySpawner.ClearSpawnedEnemies();
        boardManager.RebuildBoard();

        BoardActor player = playerSpawner.SpawnOrResetPlayer();

        if (player == null)
        {
            return false;
        }

        enemySpawner.SetPlayerActor(player);

        if (!enemySpawner.TrySpawnFixedEnemies())
        {
            return false;
        }

        RoundFlow = roundRules.CreateStateMachine();
        RoundFlow.StartFirstRound(
            enemySpawner.SpawnedEnemies.Count);
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
            || playerSpawner == null
            || enemySpawner == null
            || roundRules == null)
        {
            Debug.LogError(
                "CombatSceneBootstrap requires BoardManager, PlayerSpawner, EnemySpawner, and RoundRulesDefinition.",
                this);
            return false;
        }

        return true;
    }
}
