using System;
using UnityEngine;

/// <summary>
/// 플레이 중 변하는 현재 체력과 장비 및 임시 능력치 보정을 관리합니다.
/// </summary>
public sealed class PlayerRuntimeStats
{
    private readonly PlayerStatsDefinition definition;
    private StatModifierSet equipmentModifiers;
    private StatModifierSet temporaryModifiers;

    /// <summary>
    /// 플레이어 기본 능력치 SO를 기준으로 런타임 능력치를 생성합니다.
    /// </summary>
    public PlayerRuntimeStats(PlayerStatsDefinition definition)
    {
        this.definition = definition != null
            ? definition
            : throw new ArgumentNullException(nameof(definition));

        CurrentHealth = MaxHealth;
    }

    public int CurrentHealth { get; private set; }
    public bool IsDefeated => CurrentHealth <= 0;

    public int MaxHealth => Mathf.Max(
        1,
        definition.MaxHealth
        + equipmentModifiers.maxHealth
        + temporaryModifiers.maxHealth);

    public int AttackPower => Mathf.Max(
        0,
        definition.AttackPower
        + equipmentModifiers.attackPower
        + temporaryModifiers.attackPower);

    public int MoveRange => Mathf.Max(
        1,
        definition.MoveRange
        + equipmentModifiers.moveRange
        + temporaryModifiers.moveRange);

    public int AttackRange => Mathf.Max(
        1,
        definition.AttackRange
        + equipmentModifiers.attackRange
        + temporaryModifiers.attackRange);

    /// <summary>
    /// 현재 장착한 모든 장비의 능력치 보정값을 적용합니다.
    /// </summary>
    public void SetEquipmentModifiers(StatModifierSet modifiers)
    {
        equipmentModifiers = modifiers;
        ClampCurrentHealth();
    }

    /// <summary>
    /// 버프와 상태효과 등 일시적인 능력치 보정값을 적용합니다.
    /// </summary>
    public void SetTemporaryModifiers(StatModifierSet modifiers)
    {
        temporaryModifiers = modifiers;
        ClampCurrentHealth();
    }

    /// <summary>
    /// 피해를 적용하고 실제로 감소한 체력을 반환합니다.
    /// </summary>
    public int TakeDamage(int amount)
    {
        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
        return previousHealth - CurrentHealth;
    }

    /// <summary>
    /// 최대 체력을 넘지 않도록 회복하고 실제 회복량을 반환합니다.
    /// </summary>
    public int Heal(int amount)
    {
        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + Mathf.Max(0, amount));
        return CurrentHealth - previousHealth;
    }

    /// <summary>
    /// 현재 체력을 적용된 최대 체력까지 모두 회복합니다.
    /// </summary>
    public void RestoreToFullHealth()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// 최대 체력이 바뀌었을 때 현재 체력을 유효한 범위로 제한합니다.
    /// </summary>
    private void ClampCurrentHealth()
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }
}
