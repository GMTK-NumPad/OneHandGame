using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 랜덤 생성 후보 한 종류의 환경 타일 정의와 선택 가중치를 보관합니다.
/// </summary>
[Serializable]
public sealed class EnvironmentTileSpawnEntry
{
    [SerializeField]
    private EnvironmentTileEffectDefinition definition = null;
    [SerializeField, Min(0)] private int weight = 1;

    public EnvironmentTileEffectDefinition Definition =>
        definition;
    public int Weight => Mathf.Max(0, weight);
}

/// <summary>
/// 한 라운드에 생성할 환경 타일 수와 가중치 기반 후보 목록을 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "EnvironmentTileSpawnRules",
    menuName = "One Hand Game/Environment Tile Spawn Rules")]
public sealed class EnvironmentTileSpawnRulesDefinition
    : ScriptableObject
{
    [SerializeField, Min(0)] private int minimumSpawnCount;
    [SerializeField, Min(0)] private int maximumSpawnCount;
    [SerializeField] private List<EnvironmentTileSpawnEntry> entries =
        new();

    public int MinimumSpawnCount =>
        Mathf.Max(0, minimumSpawnCount);
    public int MaximumSpawnCount =>
        Mathf.Max(MinimumSpawnCount, maximumSpawnCount);
    public IReadOnlyList<EnvironmentTileSpawnEntry> Entries =>
        entries;

    /// <summary>
    /// 최소와 최대 생성 수가 올바른 순서를 유지하도록 Inspector 입력값을 보정합니다.
    /// </summary>
    private void OnValidate()
    {
        minimumSpawnCount = Mathf.Max(
            0,
            minimumSpawnCount);
        maximumSpawnCount = Mathf.Max(
            minimumSpawnCount,
            maximumSpawnCount);
    }
}
