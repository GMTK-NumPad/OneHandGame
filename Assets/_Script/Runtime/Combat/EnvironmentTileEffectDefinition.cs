using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 환경 타일 효과를 받을 수 있는 액터 종류를 비트 조합으로 나타냅니다.
/// </summary>
[Flags]
public enum EnvironmentEffectTarget
{
    None = 0,
    Player = 1,
    Enemies = 2
}

/// <summary>
/// 환경 타일에서 실행할 개별 효과의 종류를 나타냅니다.
/// </summary>
public enum EnvironmentTileEffectType
{
    Damage,
    Stun,
    Bind,
    ReduceCount
}

/// <summary>
/// 환경 타일 효과 한 개의 종류, 대상과 수치를 보관합니다.
/// </summary>
[Serializable]
public sealed class EnvironmentTileEffectData
{
    [SerializeField] private EnvironmentTileEffectType effectType =
        EnvironmentTileEffectType.Damage;
    [SerializeField] private EnvironmentEffectTarget targets =
        EnvironmentEffectTarget.Player
        | EnvironmentEffectTarget.Enemies;
    [SerializeField, Min(0)] private int amount = 1;
    [SerializeField, Min(1)] private int durationTurns = 1;

    public EnvironmentTileEffectType EffectType => effectType;
    public EnvironmentEffectTarget Targets => targets;
    public int Amount => Mathf.Max(0, amount);
    public int DurationTurns => Mathf.Max(1, durationTurns);

    /// <summary>
    /// 지정한 액터 종류가 이 효과의 적용 대상에 포함되는지 확인합니다.
    /// </summary>
    public bool Affects(EnvironmentEffectTarget target)
    {
        return (targets & target) != 0;
    }
}

/// <summary>
/// Tilemap의 한 TileBase와 해당 칸에서 실행할 환경 효과 목록을 연결합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "EnvironmentTileEffect",
    menuName = "One Hand Game/Environment Tile Effect")]
public sealed class EnvironmentTileEffectDefinition
    : ScriptableObject
{
    [SerializeField] private TileBase tile = null;
    [SerializeField, Min(0)]
    private int minimumSpawnDistanceFromEnvironmentTiles;
    [SerializeField] private List<EnvironmentTileEffectData> effects =
        new();

    public TileBase Tile => tile;
    public int MinimumSpawnDistanceFromEnvironmentTiles =>
        Mathf.Max(
            0,
            minimumSpawnDistanceFromEnvironmentTiles);
    public IReadOnlyList<EnvironmentTileEffectData> Effects =>
        effects;
}
