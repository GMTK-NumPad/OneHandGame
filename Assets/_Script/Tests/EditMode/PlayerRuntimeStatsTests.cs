using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 플레이어 런타임 능력치의 보정, 피해, 회복 규칙을 검사합니다.
/// </summary>
public sealed class PlayerRuntimeStatsTests
{
    private PlayerStatsDefinition definition;

    /// <summary>
    /// 각 테스트에서 사용할 임시 플레이어 능력치 SO를 생성합니다.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance<PlayerStatsDefinition>();
    }

    /// <summary>
    /// 테스트가 끝난 뒤 생성한 임시 SO를 제거합니다.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(definition);
    }

    /// <summary>
    /// 장비 보정값이 런타임에만 적용되고 원본 SO를 변경하지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void EquipmentModifiers_AffectRuntimeValuesWithoutChangingDefinition()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();
        int originalAttackPower = definition.AttackPower;

        stats.SetEquipmentModifiers(
            new StatModifierSet(
                maxHealth: 2,
                attackPower: 3,
                moveRange: 0,
                rampageDistance: 1,
                regenPerRound: 2,
                guardPerRound: 2));

        Assert.That(stats.MaxHealth, Is.EqualTo(definition.MaxHealth + 2));
        Assert.That(stats.AttackPower, Is.EqualTo(originalAttackPower + 3));
        Assert.That(
            stats.RampageDistance,
            Is.EqualTo(1));
        Assert.That(
            stats.RegenPerRound,
            Is.EqualTo(2));
        Assert.That(stats.GuardPerRound, Is.EqualTo(2));
        Assert.That(definition.AttackPower, Is.EqualTo(originalAttackPower));
    }

    /// <summary>
    /// 라운드 공격력 보너스가 추가되고 라운드 종료 시 초기화되는지 검사합니다.
    /// </summary>
    [Test]
    public void RoundAttackPower_IsClearedAtRoundEnd()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();
        int baseAttackPower = stats.AttackPower;

        stats.AddRoundAttackPower(4);
        Assert.That(stats.AttackPower, Is.EqualTo(baseAttackPower + 4));

        stats.ClearRoundAttackPower();
        Assert.That(stats.AttackPower, Is.EqualTo(baseAttackPower));
    }

    /// <summary>
    /// 무적과 Guard의 잔여 횟수가 추가 및 소비되는지 검사합니다.
    /// </summary>
    [Test]
    public void DefensiveResources_AreAdvancedAndConsumed()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();

        stats.AddInvincibleTurns(2);
        stats.AdvanceInvincibleTurn();
        Assert.That(
            stats.InvincibleTurnsRemaining,
            Is.EqualTo(1));

        stats.AddGuard(1);
        Assert.That(stats.TakeAttackDamage(1), Is.Zero);
        Assert.That(stats.GuardCount, Is.Zero);
    }

    /// <summary>
    /// 플레이어 기절은 더 긴 지속시간을 유지하고 실제로 건너뛴 턴마다 1씩 소비되는지 검사합니다.
    /// </summary>
    [Test]
    public void Stun_IsConsumedOnlyWhenPlayerTurnIsSkipped()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();

        stats.AddStunTurns(2);
        stats.AddStunTurns(1);

        Assert.That(stats.IsStunned, Is.True);
        Assert.That(stats.StunTurnsRemaining, Is.EqualTo(2));
        Assert.That(stats.TryConsumeStunnedTurn(), Is.True);
        Assert.That(stats.StunTurnsRemaining, Is.EqualTo(1));
        Assert.That(stats.TryConsumeStunnedTurn(), Is.True);
        Assert.That(stats.IsStunned, Is.False);
        Assert.That(stats.TryConsumeStunnedTurn(), Is.False);
    }

    /// <summary>
    /// 무적은 Guard를 소비하지 않고 공격 피해를 막는지 검사합니다.
    /// </summary>
    [Test]
    public void Invincible_BlocksAttackBeforeGuard()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();
        stats.AddInvincibleTurns(1);
        stats.AddGuard(1);

        Assert.That(stats.TakeAttackDamage(1), Is.Zero);
        Assert.That(stats.GuardCount, Is.EqualTo(1));
        Assert.That(stats.CurrentHealth, Is.EqualTo(stats.MaxHealth));
    }

    /// <summary>
    /// 피해형 환경 타일도 무적과 Guard 순서로 방어되는지 검사합니다.
    /// </summary>
    [Test]
    public void EnvironmentDamage_ConsumesGuardWhenNotInvincible()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();
        stats.AddGuard(1);

        Assert.That(stats.TakeEnvironmentDamage(1), Is.Zero);
        Assert.That(stats.GuardCount, Is.Zero);
        Assert.That(stats.CurrentHealth, Is.EqualTo(stats.MaxHealth));
    }

    /// <summary>
    /// 라운드 시작 시 장비의 Guard 충전량으로 현재 횟수가 초기화되는지 검사합니다.
    /// </summary>
    [Test]
    public void RoundStart_ResetsGuardFromEquipment()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();
        stats.SetEquipmentModifiers(
            new StatModifierSet(0, 0, 0, 0, 0, guardPerRound: 2));

        stats.ResetGuardForRound();
        Assert.That(stats.GuardCount, Is.EqualTo(2));

        stats.TakeAttackDamage(1);
        stats.ResetGuardForRound();
        Assert.That(stats.GuardCount, Is.EqualTo(2));
    }

    /// <summary>
    /// 피해와 회복 결과가 0과 최대 체력 사이로 제한되는지 검사합니다.
    /// </summary>
    [Test]
    public void DamageAndHealing_AreClampedToValidHealthRange()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();

        stats.TakeEnvironmentDamage(stats.MaxHealth + 10);
        Assert.That(stats.CurrentHealth, Is.Zero);
        Assert.That(stats.IsDefeated, Is.True);

        stats.Heal(stats.MaxHealth + 10);
        Assert.That(stats.CurrentHealth, Is.EqualTo(stats.MaxHealth));
        Assert.That(stats.IsDefeated, Is.False);
    }
}
