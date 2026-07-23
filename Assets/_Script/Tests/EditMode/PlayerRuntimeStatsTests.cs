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

        stats.SetEquipmentModifiers(new StatModifierSet(2, 3, 0, 1));

        Assert.That(stats.MaxHealth, Is.EqualTo(definition.MaxHealth + 2));
        Assert.That(stats.AttackPower, Is.EqualTo(originalAttackPower + 3));
        Assert.That(stats.AttackRange, Is.EqualTo(definition.AttackRange + 1));
        Assert.That(definition.AttackPower, Is.EqualTo(originalAttackPower));
    }

    /// <summary>
    /// 피해와 회복 결과가 0과 최대 체력 사이로 제한되는지 검사합니다.
    /// </summary>
    [Test]
    public void DamageAndHealing_AreClampedToValidHealthRange()
    {
        PlayerRuntimeStats stats = definition.CreateRuntimeStats();

        stats.TakeDamage(stats.MaxHealth + 10);
        Assert.That(stats.CurrentHealth, Is.Zero);
        Assert.That(stats.IsDefeated, Is.True);

        stats.Heal(stats.MaxHealth + 10);
        Assert.That(stats.CurrentHealth, Is.EqualTo(stats.MaxHealth));
        Assert.That(stats.IsDefeated, Is.False);
    }
}
