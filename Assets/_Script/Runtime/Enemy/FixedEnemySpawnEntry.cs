using System;
using UnityEngine;

/// <summary>
/// 테스트 라운드와 초반 고정 라운드에 사용할 몬스터 종류와 보드 좌표를 보관합니다.
/// </summary>
[Serializable]
public sealed class FixedEnemySpawnEntry
{
    [SerializeField] private EnemyActor enemyPrefab = null;
    [SerializeField] private EnemyDefinition definition = null;
    [SerializeField] private Vector2Int boardPosition;

    public EnemyActor EnemyPrefab => enemyPrefab;
    public EnemyDefinition Definition => definition;
    public GridPosition Position =>
        new(boardPosition.x, boardPosition.y);
}
