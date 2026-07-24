using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼처럼 고정 구성으로 진행할 한 라운드의 몬스터와 환경 타일 규칙을 정의합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "RoundDefinition",
    menuName = "One Hand Game/Progression/Round Definition")]
public sealed class RoundDefinition : ScriptableObject
{
    [SerializeField]
    private List<FixedEnemySpawnEntry> fixedEnemySpawns =
        new();
    [SerializeField]
    private EnvironmentTileSpawnRulesDefinition
        environmentTileSpawnRules = null;

    public IReadOnlyList<FixedEnemySpawnEntry>
        FixedEnemySpawns => fixedEnemySpawns;
    public EnvironmentTileSpawnRulesDefinition
        EnvironmentTileSpawnRules =>
            environmentTileSpawnRules;
}
