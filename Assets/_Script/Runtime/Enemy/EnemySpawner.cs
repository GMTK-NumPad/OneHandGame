using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 프리팹을 논리 보드 좌표에 생성하고 런타임 상태와 보드 점유를 등록합니다.
/// </summary>
public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private GoldRewardRulesDefinition goldRewardRules = null;
    [SerializeField] private Transform actorRoot = null;
    [SerializeField] private List<FixedEnemySpawnEntry> fixedSpawns = new();

    private readonly List<EnemyActor> spawnedEnemies = new();
    private BoardActor playerActor;

    public event Action<EnemyActor> EnemySpawned;
    public event Action<EnemyActor> EnemyDefeated;
    public event Action EnemiesCleared;
    public IReadOnlyList<EnemyActor> SpawnedEnemies => spawnedEnemies;
    public BoardActor PlayerActor => playerActor;

    /// <summary>
    /// 컴포넌트를 처음 추가했을 때 같은 GameObject의 BoardManager를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        boardManager = GetComponent<BoardManager>();
    }

    /// <summary>
    /// 코드에서 스포너를 구성할 때 사용할 보드, 골드 규칙과 생성 부모를 지정합니다.
    /// </summary>
    public void Configure(
        BoardManager manager,
        GoldRewardRulesDefinition rewardRules,
        Transform spawnRoot = null)
    {
        boardManager = manager;
        goldRewardRules = rewardRules;
        actorRoot = spawnRoot;
    }

    /// <summary>
    /// 몬스터 최소 스폰 거리를 계산할 기준 플레이어를 지정합니다.
    /// </summary>
    public void SetPlayerActor(BoardActor actor)
    {
        playerActor = actor;
    }

    /// <summary>
    /// Inspector에 등록한 모든 고정 스폰을 검증한 뒤 순서대로 생성합니다.
    /// </summary>
    public bool TrySpawnFixedEnemies()
    {
        if (spawnedEnemies.Count > 0)
        {
            Debug.LogError(
                "Clear previously spawned enemies before spawning a fixed batch.",
                this);
            return false;
        }

        if (!ValidateDependencies() || !ValidateFixedSpawns())
        {
            return false;
        }

        foreach (FixedEnemySpawnEntry entry in fixedSpawns)
        {
            if (TrySpawnEnemy(
                    entry.EnemyPrefab,
                    entry.Definition,
                    entry.Position,
                    out _))
            {
                continue;
            }

            ClearSpawnedEnemies();
            return false;
        }

        return true;
    }

    /// <summary>
    /// 지정한 몬스터 프리팹을 한 좌표에 생성하여 보드와 런타임 상태에 등록합니다.
    /// </summary>
    public bool TrySpawnEnemy(
        EnemyActor enemyPrefab,
        EnemyDefinition definition,
        GridPosition position,
        out EnemyActor spawnedEnemy)
    {
        spawnedEnemy = null;

        if (!ValidateDependencies())
        {
            return false;
        }

        if (enemyPrefab == null || definition == null)
        {
            Debug.LogError(
                "EnemySpawner requires both an Enemy prefab and EnemyDefinition.",
                this);
            return false;
        }

        if (!ValidateSpawnPosition(definition, position))
        {
            return false;
        }

        EnemyActor instance = Instantiate(enemyPrefab, actorRoot);

        if (!boardManager.TryPlaceActor(instance.BoardActor, position))
        {
            Debug.LogError(
                $"Enemy placement failed at {position}.",
                this);
            DestroyEnemyObject(instance);
            return false;
        }

        GoldRewardResult goldReward =
            goldRewardRules.Roll(definition.BaseGoldReward);

        if (!instance.Initialize(definition, goldReward))
        {
            instance.BoardActor.RemoveFromBoard();
            DestroyEnemyObject(instance);
            return false;
        }

        spawnedEnemies.Add(instance);
        instance.Defeated += HandleEnemyDefeated;
        spawnedEnemy = instance;
        EnemySpawned?.Invoke(instance);
        return true;
    }

    /// <summary>
    /// 이 스포너가 생성한 모든 몬스터를 보드에서 제거하고 GameObject를 파괴합니다.
    /// </summary>
    public void ClearSpawnedEnemies()
    {
        foreach (EnemyActor enemy in spawnedEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            enemy.Defeated -= HandleEnemyDefeated;
            enemy.BoardActor.RemoveFromBoard();
            DestroyEnemyObject(enemy);
        }

        spawnedEnemies.Clear();
        EnemiesCleared?.Invoke();
    }

    /// <summary>
    /// 보드와 골드 보상 규칙이 지정되어 있는지 확인합니다.
    /// </summary>
    private bool ValidateDependencies()
    {
        if (boardManager == null)
        {
            Debug.LogError(
                "EnemySpawner requires a BoardManager.",
                this);
            return false;
        }

        if (goldRewardRules == null)
        {
            Debug.LogError(
                "EnemySpawner requires GoldRewardRulesDefinition.",
                this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 처치된 몬스터를 스포너 목록에서 제거하고 외부 시스템에 알린 뒤 파괴합니다.
    /// </summary>
    private void HandleEnemyDefeated(EnemyActor enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.Defeated -= HandleEnemyDefeated;

        if (!spawnedEnemies.Remove(enemy))
        {
            return;
        }

        EnemyDefeated?.Invoke(enemy);
        DestroyEnemyObject(enemy);
    }

    /// <summary>
    /// 고정 스폰 목록의 누락된 참조, 중복 좌표와 배치 불가능한 좌표를 검사합니다.
    /// </summary>
    private bool ValidateFixedSpawns()
    {
        if (fixedSpawns == null || fixedSpawns.Count == 0)
        {
            Debug.LogError(
                "EnemySpawner requires at least one fixed spawn entry.",
                this);
            return false;
        }

        var reservedPositions = new HashSet<GridPosition>();

        for (int index = 0; index < fixedSpawns.Count; index++)
        {
            FixedEnemySpawnEntry entry = fixedSpawns[index];

            if (entry == null
                || entry.EnemyPrefab == null
                || entry.Definition == null)
            {
                Debug.LogError(
                    $"Fixed enemy spawn entry {index} is incomplete.",
                    this);
                return false;
            }

            if (!reservedPositions.Add(entry.Position))
            {
                Debug.LogError(
                    $"Fixed enemy spawn position {entry.Position} is duplicated.",
                    this);
                return false;
            }

            if (!boardManager.CanEnter(entry.Position))
            {
                Debug.LogError(
                    $"Fixed enemy spawn position {entry.Position} is unavailable.",
                    this);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 지정 좌표가 비어 있고 플레이어 및 다른 몬스터와의 최소 거리를 만족하는지 검사합니다.
    /// </summary>
    private bool ValidateSpawnPosition(
        EnemyDefinition definition,
        GridPosition position)
    {
        if (!boardManager.CanEnter(position))
        {
            Debug.LogError(
                $"Enemy cannot spawn at {position}. The tile is blocked, occupied, or outside the board.",
                this);
            return false;
        }

        if (playerActor != null
            && playerActor.IsPlaced
            && position.ChebyshevDistanceTo(playerActor.Position)
                < definition.MinimumSpawnDistanceFromPlayer)
        {
            Debug.LogError(
                $"Enemy spawn at {position} is too close to the player.",
                this);
            return false;
        }

        foreach (EnemyActor otherEnemy in spawnedEnemies)
        {
            if (otherEnemy == null
                || !otherEnemy.IsInitialized
                || !otherEnemy.BoardActor.IsPlaced)
            {
                continue;
            }

            int requiredDistance = Math.Max(
                definition.MinimumSpawnDistanceFromEnemies,
                otherEnemy.Definition.MinimumSpawnDistanceFromEnemies);

            if (position.ChebyshevDistanceTo(
                    otherEnemy.BoardActor.Position)
                < requiredDistance)
            {
                Debug.LogError(
                    $"Enemy spawn at {position} is too close to another enemy.",
                    this);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 플레이 모드 여부에 맞는 방식으로 생성된 몬스터 GameObject를 파괴합니다.
    /// </summary>
    private static void DestroyEnemyObject(EnemyActor enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(enemy.gameObject);
        }
        else
        {
            DestroyImmediate(enemy.gameObject);
        }
    }
}
