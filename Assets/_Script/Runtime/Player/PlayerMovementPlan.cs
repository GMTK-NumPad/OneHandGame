/// <summary>
/// 한 번의 방향 입력으로 결정된 플레이어 이동과 공격 결과를 보관합니다.
/// </summary>
public readonly struct PlayerMovementPlan
{
    /// <summary>
    /// 계산된 이동 위치와 공격 대상 정보를 이용해 행동 계획을 생성합니다.
    /// </summary>
    public PlayerMovementPlan(
        GridPosition start,
        GridPosition destination,
        bool shouldAttack,
        int attackTargetActorId,
        GridPosition attackTargetPosition,
        int rampageRemaining)
    {
        Start = start;
        Destination = destination;
        ShouldAttack = shouldAttack;
        AttackTargetActorId = attackTargetActorId;
        AttackTargetPosition = attackTargetPosition;
        RampageRemaining = rampageRemaining;
    }

    public GridPosition Start { get; }
    public GridPosition Destination { get; }
    public bool ShouldMove => Start != Destination;
    public bool ShouldAttack { get; }
    public bool CanAct => ShouldMove || ShouldAttack;
    public int AttackTargetActorId { get; }
    public GridPosition AttackTargetPosition { get; }
    public int RampageRemaining { get; }
}
