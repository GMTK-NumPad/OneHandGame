using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터의 일반 공격과 Shape 기반 캐스팅 범위를 실제 판정과 화면 표시가 공유하도록 계산합니다.
/// </summary>
public static class EnemyRangeCalculator
{
    private static readonly GridPosition[] AllDirections =
    {
        new(1, 0),
        new(1, 1),
        new(0, 1),
        new(-1, 1),
        new(-1, 0),
        new(-1, -1),
        new(0, -1),
        new(1, -1)
    };

    /// <summary>
    /// 대상이 몬스터 자신의 칸을 제외한 대각선 포함 일반 공격 범위 안에 있는지 확인합니다.
    /// </summary>
    public static bool IsInActionRange(
        GridPosition origin,
        GridPosition target,
        int actionRange)
    {
        return origin != target
            && origin.ChebyshevDistanceTo(target)
                <= Math.Max(1, actionRange);
    }

    /// <summary>
    /// 보드 안에 있는 일반 공격 범위 좌표를 중복 없는 집합으로 생성합니다.
    /// </summary>
    public static HashSet<GridPosition> CreateActionRange(
        IBoardQuery board,
        GridPosition origin,
        int actionRange)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        int safeRange = Math.Max(1, actionRange);
        var positions = new HashSet<GridPosition>();

        for (int offsetX = -safeRange;
             offsetX <= safeRange;
             offsetX++)
        {
            for (int offsetY = -safeRange;
                 offsetY <= safeRange;
                 offsetY++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                GridPosition position = new(
                    origin.X + offsetX,
                    origin.Y + offsetY);

                if (board.IsInside(position))
                {
                    positions.Add(position);
                }
            }
        }

        return positions;
    }

    /// <summary>
    /// 대상이 몬스터 기준 캐스팅 시작 조건 범위 안에 있는지 확인하며 Forward 계열은 8방향을 검사합니다.
    /// </summary>
    public static bool IsInCastingTriggerRange(
        GridPosition origin,
        GridPosition target,
        EnemyCastingRangeShape shape,
        int range,
        int width,
        IReadOnlyList<Vector2Int> customOffsets,
        GridPosition facingDirection)
    {
        int safeRange = Math.Max(1, range);

        if (IsDirectionalShape(shape))
        {
            foreach (GridPosition direction in AllDirections)
            {
                if (CreateCastingTriggerPositions(
                        origin,
                        shape,
                        safeRange,
                        width,
                        customOffsets,
                        direction)
                    .Contains(target))
                {
                    return true;
                }
            }

            return false;
        }

        return CreateCastingTriggerPositions(
                origin,
                shape,
                safeRange,
                width,
                customOffsets,
                facingDirection)
            .Contains(target);
    }

    /// <summary>
    /// 고정 대상 타일을 중심으로 실제 캐스팅 피해 범위를 생성하며 Custom은 입력한 Offset만 사용합니다.
    /// </summary>
    public static HashSet<GridPosition>
        CreateCastingImpactRange(
            IBoardQuery board,
            GridPosition targetCenter,
            EnemyCastingImpactShape shape,
            int range,
            IReadOnlyList<Vector2Int> customOffsets)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        HashSet<GridPosition> positions =
            CreateCastingImpactPositions(
                targetCenter,
                shape,
                range,
                customOffsets);
        positions.RemoveWhere(
            position => !board.IsInside(position));
        return positions;
    }

    /// <summary>
    /// 현재 위치가 고정 대상 타일을 중심으로 만든 실제 캐스팅 피해 범위 안에 있는지 확인합니다.
    /// </summary>
    public static bool IsInCastingImpactRange(
        GridPosition targetCenter,
        GridPosition position,
        EnemyCastingImpactShape shape,
        int range,
        IReadOnlyList<Vector2Int> customOffsets)
    {
        return CreateCastingImpactPositions(
                targetCenter,
                shape,
                range,
                customOffsets)
            .Contains(position);
    }

    /// <summary>
    /// 몬스터 기준 Shape로 만든 캐스팅 시작 조건 범위 중 현재 보드 안의 좌표만 반환합니다.
    /// </summary>
    public static HashSet<GridPosition> CreateCastingTriggerRange(
        IBoardQuery board,
        GridPosition origin,
        EnemyCastingRangeShape shape,
        int range,
        int width,
        IReadOnlyList<Vector2Int> customOffsets,
        GridPosition facingDirection)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        HashSet<GridPosition> positions =
            CreateCastingTriggerPositions(
                origin,
                shape,
                range,
                width,
                customOffsets,
                facingDirection);
        positions.RemoveWhere(
            position => !board.IsInside(position));
        return positions;
    }

    /// <summary>
    /// 선택한 Shape에 맞는 몬스터 기준 캐스팅 시작 조건 좌표를 생성합니다.
    /// </summary>
    private static HashSet<GridPosition>
        CreateCastingTriggerPositions(
            GridPosition origin,
            EnemyCastingRangeShape shape,
            int range,
            int width,
            IReadOnlyList<Vector2Int> customOffsets,
            GridPosition facingDirection)
    {
        int safeRange = Math.Max(1, range);
        int safeWidth = Math.Max(1, width);
        var positions = new HashSet<GridPosition>();

        switch (shape)
        {
            case EnemyCastingRangeShape.Around:
                AddAround(origin, safeRange, positions);
                break;

            case EnemyCastingRangeShape.Cross:
                AddCross(
                    origin,
                    safeRange,
                    diagonal: false,
                    positions);
                break;

            case EnemyCastingRangeShape.DiagonalCross:
                AddCross(
                    origin,
                    safeRange,
                    diagonal: true,
                    positions);
                break;

            case EnemyCastingRangeShape.EightDirections:
                AddCross(
                    origin,
                    safeRange,
                    diagonal: false,
                    positions);
                AddCross(
                    origin,
                    safeRange,
                    diagonal: true,
                    positions);
                break;

            case EnemyCastingRangeShape.ForwardLine:
                AddForwardLine(
                    origin,
                    safeRange,
                    facingDirection,
                    positions);
                break;

            case EnemyCastingRangeShape.ForwardRectangle:
                AddForwardArea(
                    origin,
                    safeRange,
                    safeWidth,
                    facingDirection,
                    expandPerDepth: false,
                    positions);
                break;

            case EnemyCastingRangeShape.ForwardCone:
                AddForwardArea(
                    origin,
                    safeRange,
                    safeWidth,
                    facingDirection,
                    expandPerDepth: true,
                    positions);
                break;

            default:
                AddCustom(
                    origin,
                    customOffsets,
                    positions);
                break;
        }

        positions.Remove(origin);
        return positions;
    }

    /// <summary>
    /// 플레이어의 고정 대상 타일을 중심으로 십자, X자, 사각 범위 또는 Custom 피해 좌표를 생성합니다.
    /// </summary>
    private static HashSet<GridPosition>
        CreateCastingImpactPositions(
            GridPosition targetCenter,
            EnemyCastingImpactShape shape,
            int range,
            IReadOnlyList<Vector2Int> customOffsets)
    {
        int safeRange = Math.Max(1, range);
        var positions = new HashSet<GridPosition>();

        switch (shape)
        {
            case EnemyCastingImpactShape.Around:
                AddAround(
                    targetCenter,
                    safeRange,
                    positions);
                break;

            case EnemyCastingImpactShape.Cross:
                AddCross(
                    targetCenter,
                    safeRange,
                    diagonal: false,
                    positions);
                break;

            case EnemyCastingImpactShape.DiagonalCross:
                AddCross(
                    targetCenter,
                    safeRange,
                    diagonal: true,
                    positions);
                break;

            default:
                AddCustom(
                    targetCenter,
                    customOffsets,
                    positions);
                break;
        }

        if (shape != EnemyCastingImpactShape.Custom)
        {
            positions.Add(targetCenter);
        }

        return positions;
    }

    /// <summary>
    /// 몬스터 주변의 사각형 범위를 생성합니다.
    /// </summary>
    private static void AddAround(
        GridPosition origin,
        int range,
        ISet<GridPosition> positions)
    {
        for (int offsetX = -range;
             offsetX <= range;
             offsetX++)
        {
            for (int offsetY = -range;
                 offsetY <= range;
                 offsetY++)
            {
                positions.Add(
                    new GridPosition(
                        origin.X + offsetX,
                        origin.Y + offsetY));
            }
        }
    }

    /// <summary>
    /// 상하좌우 십자 또는 대각선 X자 범위를 생성합니다.
    /// </summary>
    private static void AddCross(
        GridPosition origin,
        int range,
        bool diagonal,
        ISet<GridPosition> positions)
    {
        for (int distance = 1;
             distance <= range;
             distance++)
        {
            if (diagonal)
            {
                positions.Add(new GridPosition(
                    origin.X + distance,
                    origin.Y + distance));
                positions.Add(new GridPosition(
                    origin.X - distance,
                    origin.Y + distance));
                positions.Add(new GridPosition(
                    origin.X + distance,
                    origin.Y - distance));
                positions.Add(new GridPosition(
                    origin.X - distance,
                    origin.Y - distance));
            }
            else
            {
                positions.Add(new GridPosition(
                    origin.X + distance,
                    origin.Y));
                positions.Add(new GridPosition(
                    origin.X - distance,
                    origin.Y));
                positions.Add(new GridPosition(
                    origin.X,
                    origin.Y + distance));
                positions.Add(new GridPosition(
                    origin.X,
                    origin.Y - distance));
            }
        }
    }

    /// <summary>
    /// 몬스터가 향하는 대각선 포함 방향으로 지정 거리만큼 직선 범위를 생성합니다.
    /// </summary>
    private static void AddForwardLine(
        GridPosition origin,
        int range,
        GridPosition facingDirection,
        ISet<GridPosition> positions)
    {
        GridPosition forward =
            NormalizeFacing(facingDirection);

        for (int distance = 1;
             distance <= range;
             distance++)
        {
            positions.Add(
                origin.Offset(forward, distance));
        }
    }

    /// <summary>
    /// 현재 방향을 진행축으로 사용하여 직사각형 또는 거리마다 넓어지는 부채꼴 범위를 생성합니다.
    /// </summary>
    private static void AddForwardArea(
        GridPosition origin,
        int range,
        int width,
        GridPosition facingDirection,
        bool expandPerDepth,
        ISet<GridPosition> positions)
    {
        GridPosition forward =
            NormalizeFacing(facingDirection);
        GridPosition side = new(
            -forward.Y,
            forward.X);
        int baseHalfWidth = width / 2;

        for (int depth = 1;
             depth <= range;
             depth++)
        {
            int halfWidth =
                baseHalfWidth
                + (expandPerDepth
                    ? depth - 1
                    : 0);

            for (int sideDistance = -halfWidth;
                 sideDistance <= halfWidth;
                 sideDistance++)
            {
                positions.Add(
                    new GridPosition(
                        origin.X
                        + forward.X * depth
                        + side.X * sideDistance,
                        origin.Y
                        + forward.Y * depth
                        + side.Y * sideDistance));
            }
        }
    }

    /// <summary>
    /// Inspector에서 직접 입력한 보드축 기준 상대 좌표를 범위에 추가합니다.
    /// </summary>
    private static void AddCustom(
        GridPosition origin,
        IReadOnlyList<Vector2Int> offsets,
        ISet<GridPosition> positions)
    {
        if (offsets == null)
        {
            return;
        }

        for (int index = 0;
             index < offsets.Count;
             index++)
        {
            Vector2Int offset = offsets[index];
            positions.Add(
                new GridPosition(
                    origin.X + offset.x,
                    origin.Y + offset.y));
        }
    }

    /// <summary>
    /// 방향의 X와 Y를 -1, 0, 1로 정규화하고 유효하지 않으면 기본 오른쪽을 반환합니다.
    /// </summary>
    private static GridPosition NormalizeFacing(
        GridPosition facingDirection)
    {
        int x = Math.Sign(facingDirection.X);
        int y = Math.Sign(facingDirection.Y);

        return x == 0 && y == 0
            ? new GridPosition(1, 0)
            : new GridPosition(x, y);
    }

    /// <summary>
    /// ForwardLine, ForwardRectangle, ForwardCone처럼 8방향 회전 결과를 합쳐야 하는 Shape인지 확인합니다.
    /// </summary>
    private static bool IsDirectionalShape(
        EnemyCastingRangeShape shape)
    {
        return shape
                == EnemyCastingRangeShape.ForwardLine
            || shape
                == EnemyCastingRangeShape.ForwardRectangle
            || shape
                == EnemyCastingRangeShape.ForwardCone;
    }
}
