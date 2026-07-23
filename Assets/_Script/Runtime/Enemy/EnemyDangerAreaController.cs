using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 살아 있는 몬스터들의 일반 공격 및 캐스팅 범위를 표시 전용 Tilemap에 동적으로 그립니다.
/// </summary>
public sealed class EnemyDangerAreaController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private EnemySpawner enemySpawner = null;
    [SerializeField] private EnemyTurnController enemyTurnController = null;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap attackRangeTilemap = null;
    [SerializeField] private Tilemap castingRangeTilemap = null;

    [Header("Tiles")]
    [SerializeField] private TileBase attackRangeTile = null;
    [SerializeField] private TileBase castingRangeTile = null;

    private readonly HashSet<GridPosition> attackPositions = new();
    private readonly HashSet<GridPosition> castingPositions = new();
    private readonly HashSet<GridPosition> enemyOccupiedPositions = new();

    /// <summary>
    /// 컴포넌트를 추가할 때 같은 GameObject의 전투 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
        enemySpawner = GetComponent<EnemySpawner>();
        enemyTurnController =
            GetComponent<EnemyTurnController>();
    }

    /// <summary>
    /// 몬스터 스폰, 처치, 전체 제거와 개별 행동 완료 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.EnemySpawned += HandleEnemyChanged;
            enemySpawner.EnemyDefeated += HandleEnemyChanged;
            enemySpawner.EnemiesCleared +=
                HandleEnemiesCleared;
        }

        if (enemyTurnController != null)
        {
            enemyTurnController.EnemyActionCompleted +=
                HandleEnemyChanged;
            enemyTurnController.EnemyCastingStarted +=
                HandleEnemyChanged;
        }
    }

    /// <summary>
    /// 시작 시 참조를 검사하고 이미 생성된 몬스터들의 범위를 표시합니다.
    /// </summary>
    private void Start()
    {
        ValidateReferences();
        RefreshDangerAreas();
    }

    /// <summary>
    /// 이벤트 구독을 해제하고 표시 전용 Tilemap을 비웁니다.
    /// </summary>
    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.EnemySpawned -= HandleEnemyChanged;
            enemySpawner.EnemyDefeated -= HandleEnemyChanged;
            enemySpawner.EnemiesCleared -=
                HandleEnemiesCleared;
        }

        if (enemyTurnController != null)
        {
            enemyTurnController.EnemyActionCompleted -=
                HandleEnemyChanged;
            enemyTurnController.EnemyCastingStarted -=
                HandleEnemyChanged;
        }

        ClearDangerAreas();
    }

    /// <summary>
    /// 현재 살아 있는 몬스터 범위를 다시 계산하여 두 표시 Tilemap을 새로 그립니다.
    /// </summary>
    [ContextMenu("Refresh Danger Areas")]
    public void RefreshDangerAreas()
    {
        ClearDangerAreas();

        if (boardManager == null
            || enemySpawner == null
            || attackRangeTilemap == null
            || castingRangeTilemap == null
            || attackRangeTile == null
            || castingRangeTile == null)
        {
            return;
        }

        foreach (EnemyActor enemy in enemySpawner.SpawnedEnemies)
        {
            if (enemy == null
                || !enemy.IsInitialized
                || enemy.IsDefeated
                || !enemy.BoardActor.IsPlaced)
            {
                continue;
            }

            enemyOccupiedPositions.Add(
                enemy.BoardActor.Position);
            AddEnemyRanges(enemy);
        }

        attackPositions.ExceptWith(
            enemyOccupiedPositions);

        DrawPositions(
            attackRangeTilemap,
            attackRangeTile,
            attackPositions);
        DrawPositions(
            castingRangeTilemap,
            castingRangeTile,
            castingPositions);
    }

    /// <summary>
    /// 일반 몬스터의 공격 범위와 현재 캐스팅 중인 몬스터의 캐스팅 범위만 전체 집합에 합칩니다.
    /// </summary>
    private void AddEnemyRanges(EnemyActor enemy)
    {
        EnemyDefinition definition = enemy.Definition;
        if (!definition.IsCaster)
        {
            attackPositions.UnionWith(
                EnemyRangeCalculator.CreateActionRange(
                    boardManager,
                    enemy.BoardActor.Position,
                    definition.ActionRange));
            return;
        }

        if (enemy.RuntimeState.IsCasting
            && definition.CastingTargetType
                == EnemyCastingTargetType.Area)
        {
            castingPositions.UnionWith(
                EnemyRangeCalculator.CreateCastingImpactRange(
                    boardManager,
                    enemy.RuntimeState.CastingTarget,
                    definition.CastingImpactShape,
                    definition.CastingImpactRange,
                    definition.CastingImpactOffsets));
        }
    }

    /// <summary>
    /// 보드 좌표 집합을 실제 Tilemap 셀로 변환해 지정한 Tile을 배치합니다.
    /// </summary>
    private void DrawPositions(
        Tilemap tilemap,
        TileBase tile,
        IEnumerable<GridPosition> positions)
    {
        foreach (GridPosition position in positions)
        {
            tilemap.SetTile(
                boardManager.BoardToCell(position),
                tile);
        }
    }

    /// <summary>
    /// 몬스터 스폰, 처치 또는 행동 완료 시 모든 위험 범위를 갱신합니다.
    /// </summary>
    private void HandleEnemyChanged(EnemyActor enemy)
    {
        RefreshDangerAreas();
    }

    /// <summary>
    /// 모든 몬스터가 제거되면 표시 Tilemap을 즉시 비웁니다.
    /// </summary>
    private void HandleEnemiesCleared()
    {
        ClearDangerAreas();
    }

    /// <summary>
    /// 표시 전용 Tilemap과 계산된 좌표 집합만 비웁니다.
    /// </summary>
    private void ClearDangerAreas()
    {
        attackPositions.Clear();
        castingPositions.Clear();
        enemyOccupiedPositions.Clear();

        if (attackRangeTilemap != null)
        {
            attackRangeTilemap.ClearAllTiles();
        }

        if (castingRangeTilemap != null)
        {
            castingRangeTilemap.ClearAllTiles();
        }
    }

    /// <summary>
    /// 범위 표시를 위해 필요한 전투, Tilemap과 Tile 참조가 모두 지정되었는지 검사합니다.
    /// </summary>
    private void ValidateReferences()
    {
        if (boardManager == null
            || enemySpawner == null
            || enemyTurnController == null
            || attackRangeTilemap == null
            || castingRangeTilemap == null
            || attackRangeTile == null
            || castingRangeTile == null)
        {
            Debug.LogError(
                "EnemyDangerAreaController requires combat components, two Tilemaps, and two display Tiles.",
                this);
        }
    }
}
