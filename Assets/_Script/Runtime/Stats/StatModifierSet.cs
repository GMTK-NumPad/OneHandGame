using System;

/// <summary>
/// 장비와 상태효과가 플레이어 기본 능력치에 더하는 보정값 묶음입니다.
/// </summary>
[Serializable]
public struct StatModifierSet
{
    public int maxHealth;
    public int attackPower;
    public int moveRange;
    public int rampageDistance;
    public int regenPerRound;
    public int guardPerRound;

    /// <summary>
    /// 각 능력치에 적용할 보정값을 지정해 묶음을 생성합니다.
    /// </summary>
    public StatModifierSet(
        int maxHealth,
        int attackPower,
        int moveRange,
        int rampageDistance,
        int regenPerRound,
        int guardPerRound)
    {
        this.maxHealth = maxHealth;
        this.attackPower = attackPower;
        this.moveRange = moveRange;
        this.rampageDistance = rampageDistance;
        this.regenPerRound = regenPerRound;
        this.guardPerRound = guardPerRound;
    }
}
