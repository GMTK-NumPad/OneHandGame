using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 환경 Tilemap에서 액터의 현재 칸을 조회하고 정의된 피해, 기절과 카운트 감소 효과를 적용합니다.
/// </summary>
public sealed class EnvironmentTileEffectController
    : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private CombatSceneBootstrap combatBootstrap = null;
    [SerializeField] private PlayerSpawner playerSpawner = null;
    [SerializeField] private EnemySpawner enemySpawner = null;

    [Header("Environment")]
    [SerializeField] private Tilemap environmentTilemap = null;
    [SerializeField]
    private EnvironmentTileSpawnRulesDefinition spawnRules = null;

    private readonly Dictionary<TileBase, EnvironmentTileEffectDefinition>
        definitionsByTile = new();

    public EnvironmentTileSpawnRulesDefinition SpawnRules =>
        spawnRules;

    /// <summary>
    /// 컴포넌트를 추가할 때 같은 GameObject의 전투 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
        combatBootstrap =
            GetComponent<CombatSceneBootstrap>();
        playerSpawner = GetComponent<PlayerSpawner>();
        enemySpawner = GetComponent<EnemySpawner>();
    }

    /// <summary>
    /// 시작 시 Inspector에 등록한 환경 타일 정의를 TileBase 기준 조회 사전으로 구성합니다.
    /// </summary>
    private void Awake()
    {
        RebuildDefinitionLookup();
    }

    /// <summary>
    /// 환경 효과 실행에 필요한 전투 컴포넌트와 환경 Tilemap 참조가 연결되었는지 확인합니다.
    /// </summary>
    private void Start()
    {
        if (boardManager == null
            || combatBootstrap == null
            || playerSpawner == null
            || enemySpawner == null
            || environmentTilemap == null
            || spawnRules == null)
        {
            Debug.LogError(
                "EnvironmentTileEffectController requires combat references, an Environment Tilemap, and Environment Tile Spawn Rules.",
                this);
        }
    }

    /// <summary>
    /// Inspector 설정이 바뀌었을 때 환경 타일 정의 조회 사전을 다시 구성합니다.
    /// </summary>
    private void OnValidate()
    {
        RebuildDefinitionLookup();
    }

    /// <summary>
    /// 플레이어의 현재 환경 타일 효과를 적용하고 전투 상태에 패배 또는 카운트 만료를 보고합니다.
    /// </summary>
    public void ApplyPlayerTileEffects()
    {
        if (!TryGetPlayerContext(
                out BoardActor playerActor,
                out PlayerRuntimeStats playerStats,
                out RoundFlowStateMachine roundFlow)
            || !TryGetDefinitionAt(
                playerActor.Position,
                out EnvironmentTileEffectDefinition definition))
        {
            return;
        }

        IReadOnlyList<EnvironmentTileEffectData> effects =
            definition.Effects;

        if (effects == null)
        {
            return;
        }

        foreach (EnvironmentTileEffectData effect in effects)
        {
            if (effect == null
                || !effect.Affects(
                    EnvironmentEffectTarget.Player)
                || roundFlow.Phase
                    != RoundPhase.PlayerTurn
                || playerStats.IsDefeated)
            {
                continue;
            }

            ApplyPlayerEffect(
                effect,
                playerStats,
                roundFlow);
        }

        if (playerStats.IsDefeated
            && roundFlow.Phase == RoundPhase.PlayerTurn)
        {
            roundFlow.ReportPlayerDefeated();
        }
    }

    /// <summary>
    /// 살아 있는 모든 몬스터의 현재 환경 타일 효과를 적용하고 처치 수를 전투 흐름에 보고합니다.
    /// </summary>
    public void ApplyEnemyTileEffects()
    {
        if (enemySpawner == null
            || combatBootstrap == null
            || !combatBootstrap.IsInitialized
            || combatBootstrap.RoundFlow == null
            || combatBootstrap.RoundFlow.Phase
                != RoundPhase.EnemyTurn)
        {
            return;
        }

        RoundFlowStateMachine roundFlow =
            combatBootstrap.RoundFlow;
        var targets = new List<EnemyActor>(
            enemySpawner.SpawnedEnemies);
        int defeatedEnemyCount = 0;

        foreach (EnemyActor enemy in targets)
        {
            if (enemy == null
                || !enemy.IsInitialized
                || enemy.IsDefeated
                || !enemy.BoardActor.IsPlaced
                || !TryGetDefinitionAt(
                    enemy.BoardActor.Position,
                    out EnvironmentTileEffectDefinition definition))
            {
                continue;
            }

            ApplyEnemyEffects(
                enemy,
                definition.Effects);

            if (enemy.IsDefeated)
            {
                defeatedEnemyCount++;
            }
        }

        if (defeatedEnemyCount > 0
            && roundFlow.Phase == RoundPhase.EnemyTurn)
        {
            roundFlow.ReportEnemyDefeated(
                defeatedEnemyCount);
        }
    }

    /// <summary>
    /// Inspector에 등록한 유효한 정의를 TileBase 기준으로 저장하고 중복 등록을 경고합니다.
    /// </summary>
    private void RebuildDefinitionLookup()
    {
        definitionsByTile.Clear();

        IReadOnlyList<EnvironmentTileSpawnEntry> entries =
            spawnRules != null
                ? spawnRules.Entries
                : null;

        if (entries == null)
        {
            return;
        }

        foreach (EnvironmentTileSpawnEntry entry in entries)
        {
            EnvironmentTileEffectDefinition definition =
                entry != null
                    ? entry.Definition
                    : null;

            if (definition == null || definition.Tile == null)
            {
                continue;
            }

            if (!definitionsByTile.TryAdd(
                    definition.Tile,
                    definition))
            {
                Debug.LogWarning(
                    $"Environment tile '{definition.Tile.name}' has more than one effect definition.",
                    this);
            }
        }
    }

    /// <summary>
    /// 플레이어와 런타임 능력치, 현재 라운드 상태가 환경 효과를 처리할 수 있는지 확인합니다.
    /// </summary>
    private bool TryGetPlayerContext(
        out BoardActor playerActor,
        out PlayerRuntimeStats playerStats,
        out RoundFlowStateMachine roundFlow)
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
        roundFlow =
            combatBootstrap != null
                ? combatBootstrap.RoundFlow
                : null;

        return environmentTilemap != null
            && boardManager != null
            && combatBootstrap != null
            && combatBootstrap.IsInitialized
            && playerActor != null
            && playerActor.IsPlaced
            && playerStats != null
            && roundFlow != null
            && roundFlow.Phase == RoundPhase.PlayerTurn;
    }

    /// <summary>
    /// 지정한 보드 좌표의 TileBase에 연결된 환경 효과 정의를 반환합니다.
    /// </summary>
    private bool TryGetDefinitionAt(
        GridPosition position,
        out EnvironmentTileEffectDefinition definition)
    {
        definition = null;

        if (environmentTilemap == null
            || boardManager == null)
        {
            return false;
        }

        TileBase tile = environmentTilemap.GetTile(
            boardManager.BoardToCell(position));

        return tile != null
            && definitionsByTile.TryGetValue(
                tile,
                out definition);
    }

    /// <summary>
    /// 플레이어 대상 환경 효과 한 개를 현재 능력치 또는 라운드 상태에 적용합니다.
    /// </summary>
    private static void ApplyPlayerEffect(
        EnvironmentTileEffectData effect,
        PlayerRuntimeStats playerStats,
        RoundFlowStateMachine roundFlow)
    {
        switch (effect.EffectType)
        {
            case EnvironmentTileEffectType.Stun:
                playerStats.AddStunTurns(
                    effect.DurationTurns);
                break;

            case EnvironmentTileEffectType.Bind:
                playerStats.AddBindTurns(
                    effect.DurationTurns);
                break;

            case EnvironmentTileEffectType.ReduceCount:
                if (effect.Amount > 0)
                {
                    roundFlow.ReduceRemainingCount(
                        effect.Amount);
                }
                break;

            default:
                playerStats.TakeEnvironmentDamage(
                    effect.Amount);
                break;
        }
    }

    /// <summary>
    /// 한 몬스터에게 적용 가능한 환경 효과를 순서대로 실행하며 처치되면 이후 효과를 중단합니다.
    /// </summary>
    private static void ApplyEnemyEffects(
        EnemyActor enemy,
        IReadOnlyList<EnvironmentTileEffectData> effects)
    {
        if (effects == null)
        {
            return;
        }

        foreach (EnvironmentTileEffectData effect in effects)
        {
            if (enemy.IsDefeated)
            {
                break;
            }

            if (effect == null
                || !effect.Affects(
                    EnvironmentEffectTarget.Enemies))
            {
                continue;
            }

            switch (effect.EffectType)
            {
                case EnvironmentTileEffectType.Stun:
                    enemy.RuntimeState.ApplyStun(
                        effect.DurationTurns);
                    break;

                case EnvironmentTileEffectType.Bind:
                    enemy.RuntimeState.ApplyBind(
                        effect.DurationTurns);
                    break;

                case EnvironmentTileEffectType.Damage:
                    enemy.TakeDamage(
                        effect.Amount);
                    break;
            }
        }
    }
}
