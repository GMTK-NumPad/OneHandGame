using UnityEngine;

/// <summary>
/// 모든 플레이어 런타임 능력치의 기준이 되는 원본 데이터를 정의합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerStats",
    menuName = "One Hand Game/Definitions/Player Stats")]
public sealed class PlayerStatsDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Player";

    [Header("Base Stats")]
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(0)] private int attackPower = 1;
    [SerializeField, Min(1)] private int moveRange = 1;

    public string DisplayName => displayName;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int AttackPower => Mathf.Max(0, attackPower);
    public int MoveRange => Mathf.Max(1, moveRange);

    /// <summary>
    /// 이 SO의 기본값을 복사해 새로운 플레이어 런타임 능력치를 생성합니다.
    /// </summary>
    public PlayerRuntimeStats CreateRuntimeStats()
    {
        return new PlayerRuntimeStats(this);
    }
}
