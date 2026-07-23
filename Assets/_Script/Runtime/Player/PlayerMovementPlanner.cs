using System;

/// <summary>
/// 8방향 입력과 Rampage 수치를 사용해 플레이어의 이동 및 충돌 공격을 계산합니다.
/// </summary>
public static class PlayerMovementPlanner
{
    /// <summary>
    /// 보드 상태를 변경하지 않고 한 번의 플레이어 방향 행동 결과를 계산합니다.
    /// </summary>
    public static PlayerMovementPlan CreatePlan(
        IBoardQuery board,
        int playerActorId,
        GridPosition start,
        GridPosition direction,
        int baseMoveRange,
        int rampageDistance)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        ValidateDirection(direction);

        int safeBaseMoveRange = Math.Max(1, baseMoveRange);
        int safeRampageDistance = Math.Max(0, rampageDistance);

        if (TryFindFirstTarget(
            board,
            playerActorId,
            start,
            direction,
            out int targetActorId,
            out GridPosition targetPosition,
            out int targetDistance))
        {
            return CreateRampagePlan(
                start,
                direction,
                safeBaseMoveRange,
                safeRampageDistance,
                targetActorId,
                targetPosition,
                targetDistance);
        }

        return CreateNormalMovePlan(
            board,
            start,
            direction,
            safeBaseMoveRange);
    }

    /// <summary>
    /// 장애물 없이 같은 직선에 있는 첫 번째 액터와 거리를 찾습니다.
    /// </summary>
    private static bool TryFindFirstTarget(
        IBoardQuery board,
        int playerActorId,
        GridPosition start,
        GridPosition direction,
        out int targetActorId,
        out GridPosition targetPosition,
        out int targetDistance)
    {
        int maxSearchDistance = Math.Max(board.Width, board.Height);

        for (int distance = 1; distance < maxSearchDistance; distance++)
        {
            GridPosition position = start.Offset(direction, distance);

            if (!board.IsInside(position) || !board.IsWalkable(position))
            {
                break;
            }

            if (!board.TryGetOccupant(position, out int occupantId)
                || occupantId == playerActorId)
            {
                continue;
            }

            targetActorId = occupantId;
            targetPosition = position;
            targetDistance = distance;
            return true;
        }

        targetActorId = default;
        targetPosition = default;
        targetDistance = default;
        return false;
    }

    /// <summary>
    /// 직선상의 몬스터를 기준으로 추가 이동과 남은 Rampage 공격 여부를 계산합니다.
    /// </summary>
    private static PlayerMovementPlan CreateRampagePlan(
        GridPosition start,
        GridPosition direction,
        int baseMoveRange,
        int rampageDistance,
        int targetActorId,
        GridPosition targetPosition,
        int targetDistance)
    {
        int emptyTilesBeforeTarget = targetDistance - 1;
        int movementBudget = baseMoveRange + rampageDistance;
        int movementDistance = Math.Min(
            emptyTilesBeforeTarget,
            movementBudget);
        int rampageUsedForMovement = Math.Max(
            0,
            movementDistance - baseMoveRange);
        int rampageRemaining = Math.Max(
            0,
            rampageDistance - rampageUsedForMovement);

        bool reachedTargetFront =
            movementDistance == emptyTilesBeforeTarget;
        bool targetIsAdjacent = targetDistance == 1;
        bool shouldAttack =
            targetIsAdjacent
            || reachedTargetFront && rampageRemaining > 0;

        return new PlayerMovementPlan(
            start,
            start.Offset(direction, movementDistance),
            shouldAttack,
            targetActorId,
            targetPosition,
            rampageRemaining);
    }

    /// <summary>
    /// 직선상에 몬스터가 없거나 장애물이 있을 때 기본 이동 거리만 계산합니다.
    /// </summary>
    private static PlayerMovementPlan CreateNormalMovePlan(
        IBoardQuery board,
        GridPosition start,
        GridPosition direction,
        int baseMoveRange)
    {
        int movementDistance = 0;

        for (int distance = 1; distance <= baseMoveRange; distance++)
        {
            GridPosition position = start.Offset(direction, distance);

            if (!board.CanEnter(position))
            {
                break;
            }

            movementDistance = distance;
        }

        return new PlayerMovementPlan(
            start,
            start.Offset(direction, movementDistance),
            shouldAttack: false,
            attackTargetActorId: default,
            attackTargetPosition: default,
            rampageRemaining: 0);
    }

    /// <summary>
    /// 입력 방향이 중앙을 제외한 8방향 단위 좌표인지 검사합니다.
    /// </summary>
    private static void ValidateDirection(GridPosition direction)
    {
        bool isUnitDirection =
            direction.X >= -1
            && direction.X <= 1
            && direction.Y >= -1
            && direction.Y <= 1
            && (direction.X != 0 || direction.Y != 0);

        if (!isUnitDirection)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                "Direction must be one of the eight neighboring directions.");
        }
    }
}
