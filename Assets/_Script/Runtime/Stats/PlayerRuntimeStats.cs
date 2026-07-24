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
    private int roundAttackPowerBonus;

    /// <summary>
    /// 플레이어 기본 능력치 SO를 기준으로 런타임 능력치를 생성합니다.
    /// </summary>
    public PlayerRuntimeStats(PlayerStatsDefinition definition)
    {
        this.definition = definition != null
            ? definition
            : throw new ArgumentNullException(nameof(definition));

        CurrentHealth = MaxHealth;
        InvincibleTurnsRemaining = 0;
        GuardCount = 0;
        StunTurnsRemaining = 0;
        BindTurnsRemaining = 0;
    }

    public int CurrentHealth { get; private set; }
    public int InvincibleTurnsRemaining { get; private set; }
    public int GuardCount { get; private set; }
    public int StunTurnsRemaining { get; private set; }
    public int BindTurnsRemaining { get; private set; }
    public int RoundAttackPowerBonus => roundAttackPowerBonus;
    public bool IsDefeated => CurrentHealth <= 0;
    public bool IsStunned => StunTurnsRemaining > 0;
    public bool IsBound => BindTurnsRemaining > 0;

    public int MaxHealth => Mathf.Max(
        1,
        definition.MaxHealth
        + equipmentModifiers.maxHealth
        + temporaryModifiers.maxHealth);

    public int AttackPower => Mathf.Max(
        0,
        definition.AttackPower
        + equipmentModifiers.attackPower
        + temporaryModifiers.attackPower
        + roundAttackPowerBonus);

    public int MoveRange => Mathf.Max(
        1,
        definition.MoveRange
        + equipmentModifiers.moveRange
        + temporaryModifiers.moveRange);

    public int RampageDistance => Mathf.Max(
        0,
        equipmentModifiers.rampageDistance
        + temporaryModifiers.rampageDistance);

    public int RegenPerRound => Mathf.Max(
        0,
        equipmentModifiers.regenPerRound
        + temporaryModifiers.regenPerRound);

    public int GuardPerRound => Mathf.Max(
        0,
        equipmentModifiers.guardPerRound
        + temporaryModifiers.guardPerRound);

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
    /// 현재 라운드에만 적용할 추가 공격력을 누적합니다.
    /// </summary>
    public void AddRoundAttackPower(int amount)
    {
        roundAttackPowerBonus = Mathf.Max(
            0,
            roundAttackPowerBonus + amount);
    }

    /// <summary>
    /// 라운드가 끝날 때 라운드 전용 추가 공격력을 제거합니다.
    /// </summary>
    public void ClearRoundAttackPower()
    {
        roundAttackPowerBonus = 0;
    }

    /// <summary>
    /// 무적 잔여 턴을 추가합니다.
    /// </summary>
    public void AddInvincibleTurns(int turns)
    {
        InvincibleTurnsRemaining += Mathf.Max(0, turns);
    }

    /// <summary>
    /// 카운트 처리 직전에 무적 잔여 턴을 1 감소시킵니다.
    /// </summary>
    public void AdvanceInvincibleTurn()
    {
        InvincibleTurnsRemaining = Mathf.Max(
            0,
            InvincibleTurnsRemaining - 1);
    }

    /// <summary>
    /// 플레이어에게 적용할 기절 잔여 턴을 현재 값과 새 값 중 큰 값으로 갱신합니다.
    /// </summary>
    public void AddStunTurns(int turns)
    {
        StunTurnsRemaining = Mathf.Max(
            StunTurnsRemaining,
            Mathf.Max(0, turns));
    }

    /// <summary>
    /// 기절이 남아 있다면 플레이어 턴 한 번을 소비하고 건너뛰어야 함을 반환합니다.
    /// </summary>
    public bool TryConsumeStunnedTurn()
    {
        if (StunTurnsRemaining <= 0)
        {
            return false;
        }

        StunTurnsRemaining--;
        return true;
    }

    /// <summary>
    /// 플레이어에게 적용할 속박 잔여 턴을 현재 값과 새 값 중 큰 값으로 갱신합니다.
    /// </summary>
    public void AddBindTurns(int turns)
    {
        BindTurnsRemaining = Mathf.Max(
            BindTurnsRemaining,
            Mathf.Max(0, turns));
    }

    /// <summary>
    /// 속박이 남아 있다면 이동이 제한된 플레이어 행동 한 번을 소비하며 잔여 턴을 감소시킵니다.
    /// </summary>
    public bool TryConsumeBoundTurn()
    {
        if (BindTurnsRemaining <= 0)
        {
            return false;
        }

        BindTurnsRemaining--;
        return true;
    }

    /// <summary>
    /// 모든 공격을 막는 Guard 횟수를 추가합니다.
    /// </summary>
    public void AddGuard(int amount)
    {
        GuardCount += Mathf.Max(0, amount);
    }

    /// <summary>
    /// 라운드 시작 시 현재 Guard 횟수를 장비가 제공하는 라운드 충전량으로 초기화합니다.
    /// </summary>
    public void ResetGuardForRound()
    {
        GuardCount = GuardPerRound;
    }

    /// <summary>
    /// Guard가 남아 있다면 1회 소비하고 공격 방어 성공 여부를 반환합니다.
    /// </summary>
    private bool TryConsumeGuard()
    {
        if (GuardCount <= 0)
        {
            return false;
        }

        GuardCount--;
        return true;
    }

    /// <summary>
    /// 몬스터 공격 피해를 적용하며 무적 또는 Guard로 막았다면 0을 반환합니다.
    /// </summary>
    public int TakeAttackDamage(int amount)
    {
        return TakeDefendableDamage(amount);
    }

    /// <summary>
    /// 피해형 환경 타일의 피해를 무적과 Guard 순서로 방어한 뒤 적용합니다.
    /// </summary>
    public int TakeEnvironmentDamage(int amount)
    {
        return TakeDefendableDamage(amount);
    }

    /// <summary>
    /// 무적을 먼저 확인하고 Guard가 남아 있으면 1회 소비하여 피해를 막습니다.
    /// </summary>
    private int TakeDefendableDamage(int amount)
    {
        if (InvincibleTurnsRemaining > 0 || TryConsumeGuard())
        {
            return 0;
        }

        return ApplyHealthDamage(amount);
    }

    /// <summary>
    /// 방어 판정이 끝난 피해를 현재 체력에 적용합니다.
    /// </summary>
    private int ApplyHealthDamage(int amount)
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
    /// 라운드가 끝났을 때 Regen 수치만큼 체력을 회복합니다.
    /// </summary>
    public int ApplyRoundEndRegeneration()
    {
        return Heal(RegenPerRound);
    }

    /// <summary>
    /// 최대 체력이 바뀌었을 때 현재 체력을 유효한 범위로 제한합니다.
    /// </summary>
    private void ClampCurrentHealth()
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }
}
