using System;
using System.Collections.Generic;

/// <summary>
/// 대각선을 포함한 이동 가능 타일을 탐색하여 몬스터의 다음 한 칸을 계산합니다.
/// </summary>
public static class EnemyPathfinder
{
    private static readonly GridPosition[] Directions =
    {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
        new(1, 1),
        new(1, -1),
        new(-1, -1),
        new(-1, 1)
    };

    /// <summary>
    /// 현재 위치에서 대상 위치까지 가장 짧은 경로를 찾아 첫 번째 이동 좌표를 반환합니다.
    /// </summary>
    public static bool TryGetNextStep(
        IBoardQuery board,
        int movingActorId,
        GridPosition start,
        GridPosition target,
        out GridPosition nextStep)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        nextStep = start;

        if (start == target
            || !board.IsInside(start)
            || !board.IsInside(target))
        {
            return false;
        }

        var frontier = new Queue<GridPosition>();
        var visited = new HashSet<GridPosition>();
        var previousPositions =
            new Dictionary<GridPosition, GridPosition>();

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            GridPosition current = frontier.Dequeue();

            foreach (GridPosition direction in Directions)
            {
                GridPosition neighbor =
                    current.Offset(direction);

                if (visited.Contains(neighbor)
                    || !CanTraverse(
                        board,
                        movingActorId,
                        neighbor,
                        target))
                {
                    continue;
                }

                visited.Add(neighbor);
                previousPositions.Add(neighbor, current);

                if (neighbor == target)
                {
                    return TryReconstructFirstStep(
                        start,
                        target,
                        previousPositions,
                        out nextStep);
                }

                frontier.Enqueue(neighbor);
            }
        }

        return false;
    }

    /// <summary>
    /// 이동 가능한 지형이며 다른 액터가 점유하지 않았거나 최종 대상 타일인지 확인합니다.
    /// </summary>
    private static bool CanTraverse(
        IBoardQuery board,
        int movingActorId,
        GridPosition position,
        GridPosition target)
    {
        if (!board.IsInside(position)
            || !board.IsWalkable(position))
        {
            return false;
        }

        if (position == target)
        {
            return true;
        }

        return !board.TryGetOccupant(
                position,
                out int occupantId)
            || occupantId == movingActorId;
    }

    /// <summary>
    /// 대상에서 시작점까지 경로를 역추적하여 실제로 이동할 첫 번째 빈 타일을 반환합니다.
    /// </summary>
    private static bool TryReconstructFirstStep(
        GridPosition start,
        GridPosition target,
        IReadOnlyDictionary<GridPosition, GridPosition> previousPositions,
        out GridPosition nextStep)
    {
        GridPosition current = target;

        while (previousPositions.TryGetValue(
                   current,
                   out GridPosition previous)
               && previous != start)
        {
            current = previous;
        }

        nextStep = current;
        return nextStep != start && nextStep != target;
    }
}
