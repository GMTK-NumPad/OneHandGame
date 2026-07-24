using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 라운드 시작 시 이동 가능한 빈 칸에 가중치와 최소 간격을 적용해 환경 타일을 랜덤 생성합니다.
/// </summary>
public sealed class EnvironmentTileSpawner : MonoBehaviour
{
    private readonly struct SpawnedEnvironmentTile
    {
        /// <summary>
        /// 생성 좌표와 해당 환경 타일 정의를 하나의 배치 기록으로 생성합니다.
        /// </summary>
        public SpawnedEnvironmentTile(
            GridPosition position,
            EnvironmentTileEffectDefinition definition)
        {
            Position = position;
            Definition = definition;
        }

        public GridPosition Position { get; }
        public EnvironmentTileEffectDefinition Definition { get; }
    }

    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private Tilemap environmentTilemap = null;
    [SerializeField]
    private EnvironmentTileSpawnRulesDefinition spawnRules = null;

    private readonly List<SpawnedEnvironmentTile> spawnedTiles = new();

    public int SpawnedTileCount => spawnedTiles.Count;
    public EnvironmentTileSpawnRulesDefinition SpawnRules =>
        spawnRules;

    /// <summary>
    /// 컴포넌트를 추가할 때 같은 GameObject의 BoardManager를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
    }

    /// <summary>
    /// 기존 환경 타일을 지우고 현재 규칙의 수량, 가중치와 간격에 맞춰 새 타일을 생성합니다.
    /// </summary>
    public bool GenerateEnvironmentTiles()
    {
        if (!ValidateReferences())
        {
            return false;
        }

        ClearEnvironmentTiles();

        int desiredCount = UnityEngine.Random.Range(
            spawnRules.MinimumSpawnCount,
            spawnRules.MaximumSpawnCount + 1);

        for (int index = 0; index < desiredCount; index++)
        {
            if (!TrySpawnOneTile())
            {
                break;
            }
        }

        return true;
    }

    /// <summary>
    /// 현재 환경 Tilemap과 생성된 타일 배치 기록을 모두 비웁니다.
    /// </summary>
    public void ClearEnvironmentTiles()
    {
        spawnedTiles.Clear();

        if (environmentTilemap != null)
        {
            environmentTilemap.ClearAllTiles();
        }
    }

    /// <summary>
    /// 현재 배치 상태에서 생성 가능한 정의를 가중치로 선택하고 후보 좌표 하나에 배치합니다.
    /// </summary>
    private bool TrySpawnOneTile()
    {
        IReadOnlyList<EnvironmentTileSpawnEntry> entries =
            spawnRules.Entries;

        if (entries == null || entries.Count == 0)
        {
            return false;
        }

        var eligibleEntries =
            new List<EnvironmentTileSpawnEntry>();
        var candidatesByEntry =
            new Dictionary<EnvironmentTileSpawnEntry, List<GridPosition>>();
        int totalWeight = 0;

        foreach (EnvironmentTileSpawnEntry entry in entries)
        {
            if (entry == null
                || entry.Definition == null
                || entry.Definition.Tile == null
                || entry.Weight <= 0)
            {
                continue;
            }

            List<GridPosition> candidates =
                CollectCandidates(entry.Definition);

            if (candidates.Count == 0)
            {
                continue;
            }

            eligibleEntries.Add(entry);
            candidatesByEntry.Add(entry, candidates);
            totalWeight += entry.Weight;
        }

        if (eligibleEntries.Count == 0 || totalWeight <= 0)
        {
            return false;
        }

        EnvironmentTileSpawnEntry selectedEntry =
            ChooseWeightedEntry(
                eligibleEntries,
                totalWeight);
        List<GridPosition> selectedCandidates =
            candidatesByEntry[selectedEntry];
        GridPosition selectedPosition =
            selectedCandidates[
                UnityEngine.Random.Range(
                    0,
                    selectedCandidates.Count)];

        environmentTilemap.SetTile(
            boardManager.BoardToCell(selectedPosition),
            selectedEntry.Definition.Tile);
        spawnedTiles.Add(
            new SpawnedEnvironmentTile(
                selectedPosition,
                selectedEntry.Definition));
        return true;
    }

    /// <summary>
    /// 지정한 환경 타일 정의를 현재 보드에 배치할 수 있는 모든 빈 좌표를 수집합니다.
    /// </summary>
    private List<GridPosition> CollectCandidates(
        EnvironmentTileEffectDefinition definition)
    {
        var candidates = new List<GridPosition>();

        for (int x = 0; x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                GridPosition position = new(x, y);

                if (CanSpawnAt(
                        definition,
                        position))
                {
                    candidates.Add(position);
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// 좌표가 이동 가능하고 비어 있으며 기존 환경 타일과 필요한 최소 거리를 만족하는지 확인합니다.
    /// </summary>
    private bool CanSpawnAt(
        EnvironmentTileEffectDefinition definition,
        GridPosition position)
    {
        if (!boardManager.IsWalkable(position)
            || boardManager.TryGetOccupant(
                position,
                out _)
            || environmentTilemap.HasTile(
                boardManager.BoardToCell(position)))
        {
            return false;
        }

        foreach (SpawnedEnvironmentTile spawnedTile
                 in spawnedTiles)
        {
            int requiredDistance = Math.Max(
                definition.MinimumSpawnDistanceFromEnvironmentTiles,
                spawnedTile.Definition
                    .MinimumSpawnDistanceFromEnvironmentTiles);

            if (position.ChebyshevDistanceTo(
                    spawnedTile.Position)
                < requiredDistance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 생성 가능한 후보 목록에서 설정된 정수 가중치에 따라 한 항목을 선택합니다.
    /// </summary>
    private static EnvironmentTileSpawnEntry
        ChooseWeightedEntry(
            IReadOnlyList<EnvironmentTileSpawnEntry> entries,
            int totalWeight)
    {
        int roll = UnityEngine.Random.Range(
            0,
            totalWeight);

        foreach (EnvironmentTileSpawnEntry entry in entries)
        {
            if (roll < entry.Weight)
            {
                return entry;
            }

            roll -= entry.Weight;
        }

        return entries[entries.Count - 1];
    }

    /// <summary>
    /// 환경 랜덤 생성에 필요한 보드, Tilemap과 생성 규칙 SO가 연결되었는지 확인합니다.
    /// </summary>
    private bool ValidateReferences()
    {
        if (boardManager != null
            && environmentTilemap != null
            && spawnRules != null)
        {
            return true;
        }

        Debug.LogError(
            "EnvironmentTileSpawner requires BoardManager, an Environment Tilemap, and Environment Tile Spawn Rules.",
            this);
        return false;
    }
}
