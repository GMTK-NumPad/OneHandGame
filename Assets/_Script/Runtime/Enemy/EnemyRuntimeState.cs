using System;

/// <summary>
/// 개별 몬스터의 체력, 쿨다운, 기절과 캐스팅 진행 상태를 관리합니다.
/// </summary>
public sealed class EnemyRuntimeState
{
    private readonly EnemyDefinition definition;

    /// <summary>
    /// 몬스터 정의와 생성된 GameObject의 Instance ID로 런타임 상태를 생성합니다.
    /// </summary>
    public EnemyRuntimeState(
        EnemyDefinition definition,
        int instanceId,
        GoldRewardResult goldReward)
    {
        this.definition = definition != null
            ? definition
            : throw new ArgumentNullException(nameof(definition));

        InstanceId = instanceId;
        CurrentHealth = definition.MaxHealth;
        GoldReward = goldReward.Amount;
        HasJackpotReward = goldReward.IsJackpot;
    }

    public EnemyDefinition Definition => definition;
    public int InstanceId { get; }
    public int CurrentHealth { get; private set; }
    public int GoldReward { get; }
    public bool HasJackpotReward { get; }
    public int CurrentCastingProgress { get; private set; }
    public int CooldownRemaining { get; private set; }
    public int StunRemaining { get; private set; }
    public bool HasCastingTarget { get; private set; }
    public GridPosition CastingTarget { get; private set; }
    public bool IsDefeated => CurrentHealth <= 0;
    public bool IsStunned => StunRemaining > 0;
    public bool IsCasting => HasCastingTarget;
    public bool CanAttackOrCast =>
        !IsDefeated
        && !IsStunned
        && CooldownRemaining <= 0;

    /// <summary>
    /// 대상이 대각선을 포함한 일반 공격 범위 안에 있는지 확인합니다.
    /// </summary>
    public bool IsInActionRange(
        GridPosition enemyPosition,
        GridPosition targetPosition)
    {
        return enemyPosition.ChebyshevDistanceTo(targetPosition)
            <= definition.ActionRange;
    }

    /// <summary>
    /// 처음 지정한 타일을 고정 대상으로 삼아 캐스팅을 시작합니다.
    /// </summary>
    public bool TryBeginCasting(GridPosition targetPosition)
    {
        if (!definition.IsCaster || !CanAttackOrCast || IsCasting)
        {
            return false;
        }

        CastingTarget = targetPosition;
        HasCastingTarget = true;
        CurrentCastingProgress = 0;
        return true;
    }

    /// <summary>
    /// 진행 중인 캐스팅을 1 올리고 발동 준비가 끝났는지 반환합니다.
    /// </summary>
    public bool AdvanceCasting()
    {
        if (!IsCasting || IsStunned || IsDefeated)
        {
            return false;
        }

        CurrentCastingProgress++;
        return CurrentCastingProgress >= definition.RequiredCastingProgress;
    }

    /// <summary>
    /// 캐스팅 효과가 발동된 뒤 진행 상태를 지우고 행동 쿨다운을 적용합니다.
    /// </summary>
    public bool CompleteCastingAction()
    {
        if (!IsCasting)
        {
            return false;
        }

        ClearCasting();
        ApplyActionCooldown();
        return true;
    }

    /// <summary>
    /// 일반 공격을 실행한 뒤 행동 쿨다운을 적용합니다.
    /// </summary>
    public bool CompleteAttackAction()
    {
        if (!CanAttackOrCast || IsCasting)
        {
            return false;
        }

        ApplyActionCooldown();
        return true;
    }

    /// <summary>
    /// 기절을 적용하고 진행 중인 캐스팅을 취소하며 행동 쿨다운을 겁니다.
    /// </summary>
    public void ApplyStun(int turns)
    {
        int safeTurns = Math.Max(0, turns);
        if (safeTurns <= 0)
        {
            return;
        }

        StunRemaining = Math.Max(StunRemaining, safeTurns);

        if (IsCasting)
        {
            ClearCasting();
            ApplyActionCooldown();
        }
    }

    /// <summary>
    /// 카운트 처리 직전에 행동 쿨다운을 1 감소시킵니다.
    /// </summary>
    public void AdvanceCooldown()
    {
        CooldownRemaining = Math.Max(0, CooldownRemaining - 1);
    }

    /// <summary>
    /// 몬스터 턴 이후 카운트 감소 전에 기절 잔여 턴을 1 감소시킵니다.
    /// </summary>
    public void AdvanceStun()
    {
        StunRemaining = Math.Max(0, StunRemaining - 1);
    }

    /// <summary>
    /// 피해를 적용하고 실제 감소한 체력을 반환합니다.
    /// </summary>
    public int TakeDamage(int amount)
    {
        int previousHealth = CurrentHealth;
        CurrentHealth = Math.Max(
            0,
            CurrentHealth - Math.Max(0, amount));
        return previousHealth - CurrentHealth;
    }

    /// <summary>
    /// 진행 중인 캐스팅과 고정 대상 타일을 초기화합니다.
    /// </summary>
    private void ClearCasting()
    {
        CurrentCastingProgress = 0;
        HasCastingTarget = false;
        CastingTarget = default;
    }

    /// <summary>
    /// 몬스터 정의에 설정된 공격 및 캐스팅 쿨다운을 적용합니다.
    /// </summary>
    private void ApplyActionCooldown()
    {
        CooldownRemaining = Math.Max(
            CooldownRemaining,
            definition.ActionCooldownTurns);
    }
}
