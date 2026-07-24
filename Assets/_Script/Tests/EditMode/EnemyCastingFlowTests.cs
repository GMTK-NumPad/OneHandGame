using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 캐스팅 범위 조건, 고정 대상, 진행도 완료와 기절 취소 규칙을 검사합니다.
/// </summary>
public sealed class EnemyCastingFlowTests
{
    /// <summary>
    /// 플레이어 타일이 현재 방향의 캐스팅 범위에 포함되면 인식 범위와 관계없이 조건이 성립하는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingCondition_UsesOnlyCastingRange()
    {
        EnemyDefinition definition =
            CreateCasterDefinition(
                requiredProgress: 1,
                actionRange: 1,
                actionCooldown: 0);

        try
        {
            EnemyRuntimeState state =
                definition.CreateRuntimeState(
                    instanceId: 1,
                    goldReward: new GoldRewardResult(
                        amount: 0,
                        isJackpot: false));
            GridPosition origin = new(3, 3);
            GridPosition target = new(5, 5);

            state.FaceTowards(origin, target);

            Assert.That(
                state.IsCastingConditionMet(
                    origin,
                    target),
                Is.True);
            Assert.That(
                state.IsCastingConditionMet(
                    origin,
                    new GridPosition(5, 4)),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    /// <summary>
    /// 캐스팅 중 플레이어가 이동하더라도 처음 저장한 대상이 유지되고 필요 진행도에서 완료되는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingTarget_RemainsFixedUntilRelease()
    {
        EnemyDefinition definition =
            CreateCasterDefinition(
                requiredProgress: 1,
                actionRange: 2,
                actionCooldown: 2);

        try
        {
            EnemyRuntimeState state =
                definition.CreateRuntimeState(
                    instanceId: 1,
                    goldReward: new GoldRewardResult(
                        amount: 0,
                        isJackpot: false));
            GridPosition fixedTarget = new(1, 3);

            Assert.That(
                state.TryBeginCasting(fixedTarget),
                Is.True);
            Assert.That(state.CastingTarget, Is.EqualTo(fixedTarget));
            Assert.That(state.AdvanceCasting(), Is.True);
            Assert.That(state.CastingTarget, Is.EqualTo(fixedTarget));
            Assert.That(state.CompleteCastingAction(), Is.True);
            Assert.That(state.IsCasting, Is.False);
            Assert.That(state.CooldownRemaining, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    /// <summary>
    /// 필요 진행도가 2라면 캐스팅 시작 다음 두 번의 몬스터 행동 후 발동 준비가 되는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingProgress_ReleasesAtRequiredProgress()
    {
        EnemyDefinition definition =
            CreateCasterDefinition(
                requiredProgress: 2,
                actionRange: 2,
                actionCooldown: 0);

        try
        {
            EnemyRuntimeState state =
                definition.CreateRuntimeState(
                    instanceId: 1,
                    goldReward: new GoldRewardResult(
                        amount: 0,
                        isJackpot: false));

            state.TryBeginCasting(new GridPosition(1, 3));

            Assert.That(state.AdvanceCasting(), Is.False);
            Assert.That(state.CurrentCastingProgress, Is.EqualTo(1));
            Assert.That(state.AdvanceCasting(), Is.True);
            Assert.That(state.CurrentCastingProgress, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    /// <summary>
    /// 캐스팅 도중 기절하면 대상과 진행도가 지워지고 행동 쿨다운이 적용되는지 검사합니다.
    /// </summary>
    [Test]
    public void Stun_CancelsCastingAndAppliesCooldown()
    {
        EnemyDefinition definition =
            CreateCasterDefinition(
                requiredProgress: 2,
                actionRange: 2,
                actionCooldown: 3);

        try
        {
            EnemyRuntimeState state =
                definition.CreateRuntimeState(
                    instanceId: 1,
                    goldReward: new GoldRewardResult(
                        amount: 0,
                        isJackpot: false));

            state.TryBeginCasting(new GridPosition(1, 3));
            state.AdvanceCasting();
            state.ApplyStun(turns: 1);

            Assert.That(state.IsCasting, Is.False);
            Assert.That(state.CurrentCastingProgress, Is.Zero);
            Assert.That(state.CooldownRemaining, Is.EqualTo(3));
            Assert.That(state.IsStunned, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    /// <summary>
    /// 새 캐스터 정의가 기본적으로 범위 대상과 피해 효과 한 개를 제공하는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingEffects_DefaultToAreaWithDamageEffect()
    {
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            Assert.That(
                definition.CastingTargetType,
                Is.EqualTo(EnemyCastingTargetType.Area));
            Assert.That(definition.CastingEffects.Count, Is.EqualTo(1));
            Assert.That(
                definition.CastingEffects[0].EffectType,
                Is.EqualTo(EnemyCastingEffectType.Damage));
            Assert.That(
                definition.CastingEffects[0].Amount,
                Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    /// <summary>
    /// 몬스터 속박은 기절시키지 않고 이동 제한 상태만 유지하며 지속시간을 감소시키는지 검사합니다.
    /// </summary>
    [Test]
    public void Bind_DoesNotPreventEnemyAttackOrCasting()
    {
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyRuntimeState state =
                definition.CreateRuntimeState(
                    instanceId: 1,
                    goldReward: new GoldRewardResult(
                        amount: 0,
                        isJackpot: false));

            state.ApplyBind(2);

            Assert.That(state.IsBound, Is.True);
            Assert.That(state.IsStunned, Is.False);
            Assert.That(state.CanAttackOrCast, Is.True);

            state.AdvanceBind();
            Assert.That(state.BindRemaining, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    /// <summary>
    /// 테스트에 필요한 캐스터 SO를 생성하고 비공개 직렬화 필드를 설정합니다.
    /// </summary>
    private static EnemyDefinition CreateCasterDefinition(
        int requiredProgress,
        int actionRange,
        int actionCooldown)
    {
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();

        SetField(definition, "isCaster", true);
        SetField(
            definition,
            "requiredCastingProgress",
            requiredProgress);
        SetField(definition, "actionRange", actionRange);
        SetField(
            definition,
            "actionCooldownTurns",
            actionCooldown);
        SetField(
            definition,
            "castingRangeShape",
            EnemyCastingRangeShape.ForwardLine);
        SetField(definition, "castingRange", 2);
        return definition;
    }

    /// <summary>
    /// EnemyDefinition의 Inspector 직렬화 필드를 테스트 설정값으로 변경합니다.
    /// </summary>
    private static void SetField<T>(
        EnemyDefinition definition,
        string fieldName,
        T value)
    {
        FieldInfo field = typeof(EnemyDefinition).GetField(
            fieldName,
            BindingFlags.Instance
            | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(definition, value);
    }
}
