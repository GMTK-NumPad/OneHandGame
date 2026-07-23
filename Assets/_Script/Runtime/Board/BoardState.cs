using System;
using System.Collections.Generic;

/// <summary>
/// 보드의 이동 가능 타일과 액터 점유 상태를 Unity 씬과 독립적으로 관리합니다.
/// </summary>
public sealed class BoardState
{
    private readonly bool[,] walkableTiles;
    private readonly Dictionary<GridPosition, int> occupants = new();
    private readonly Dictionary<int, GridPosition> actorPositions = new();

    /// <summary>
    /// 지정한 크기와 기본 이동 가능 여부로 보드 상태를 생성합니다.
    /// </summary>
    public BoardState(int width, int height, bool defaultWalkable = false)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        walkableTiles = new bool[width, height];

        if (!defaultWalkable)
        {
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                walkableTiles[x, y] = true;
            }
        }
    }

    public int Width { get; }
    public int Height { get; }
    public GridPosition Center => new((Width - 1) / 2, (Height - 1) / 2);

    /// <summary>
    /// 좌표가 보드 범위 안에 있는지 확인합니다.
    /// </summary>
    public bool IsInside(GridPosition position)
    {
        return position.X >= 0
            && position.X < Width
            && position.Y >= 0
            && position.Y < Height;
    }

    /// <summary>
    /// 좌표가 보드 안에 있고 이동 가능한 타일인지 확인합니다.
    /// </summary>
    public bool IsWalkable(GridPosition position)
    {
        return IsInside(position) && walkableTiles[position.X, position.Y];
    }

    /// <summary>
    /// 좌표에 다른 액터가 위치하고 있는지 확인합니다.
    /// </summary>
    public bool IsOccupied(GridPosition position)
    {
        return occupants.ContainsKey(position);
    }

    /// <summary>
    /// 좌표가 이동 가능하고 다른 액터가 점유하지 않았는지 확인합니다.
    /// </summary>
    public bool CanEnter(GridPosition position)
    {
        return IsWalkable(position) && !IsOccupied(position);
    }

    /// <summary>
    /// 지정한 좌표의 이동 가능 여부를 변경합니다.
    /// </summary>
    public bool SetWalkable(GridPosition position, bool isWalkable)
    {
        if (!IsInside(position))
        {
            return false;
        }

        if (!isWalkable && IsOccupied(position))
        {
            return false;
        }

        walkableTiles[position.X, position.Y] = isWalkable;
        return true;
    }

    /// <summary>
    /// 액터를 비어 있는 이동 가능 타일에 등록합니다.
    /// </summary>
    public bool TryPlaceActor(int actorId, GridPosition position)
    {
        if (actorPositions.TryGetValue(actorId, out GridPosition currentPosition))
        {
            return currentPosition == position || TryMoveActor(actorId, position);
        }

        if (!CanEnter(position))
        {
            return false;
        }

        occupants.Add(position, actorId);
        actorPositions.Add(actorId, position);
        return true;
    }

    /// <summary>
    /// 등록된 액터를 비어 있는 이동 가능 타일로 옮깁니다.
    /// </summary>
    public bool TryMoveActor(int actorId, GridPosition targetPosition)
    {
        if (!actorPositions.TryGetValue(actorId, out GridPosition currentPosition))
        {
            return false;
        }

        if (currentPosition == targetPosition)
        {
            return true;
        }

        if (!CanEnter(targetPosition))
        {
            return false;
        }

        occupants.Remove(currentPosition);
        occupants.Add(targetPosition, actorId);
        actorPositions[actorId] = targetPosition;
        return true;
    }

    /// <summary>
    /// 액터와 액터가 점유한 타일 정보를 보드에서 제거합니다.
    /// </summary>
    public bool RemoveActor(int actorId)
    {
        if (!actorPositions.Remove(actorId, out GridPosition position))
        {
            return false;
        }

        occupants.Remove(position);
        return true;
    }

    /// <summary>
    /// 등록된 액터의 현재 보드 좌표를 가져옵니다.
    /// </summary>
    public bool TryGetActorPosition(int actorId, out GridPosition position)
    {
        return actorPositions.TryGetValue(actorId, out position);
    }

    /// <summary>
    /// 지정한 타일을 점유한 액터 식별자를 가져옵니다.
    /// </summary>
    public bool TryGetOccupant(GridPosition position, out int actorId)
    {
        return occupants.TryGetValue(position, out actorId);
    }

    /// <summary>
    /// 이동 가능 타일 정보는 유지하고 모든 액터 점유 정보만 제거합니다.
    /// </summary>
    public void ClearOccupants()
    {
        occupants.Clear();
        actorPositions.Clear();
    }
}
