using System;

/// <summary>
/// 7×7 보드 안에서 사용하는 정수 기반 타일 좌표입니다.
/// </summary>
[Serializable]
public readonly struct GridPosition : IEquatable<GridPosition>
{
    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    /// <summary>
    /// 지정한 방향으로 거리만큼 이동한 새로운 보드 좌표를 반환합니다.
    /// </summary>
    public GridPosition Offset(GridPosition direction, int distance = 1)
    {
        return new GridPosition(
            X + direction.X * distance,
            Y + direction.Y * distance);
    }

    /// <summary>
    /// 대각선 이동을 한 칸으로 계산하여 다른 좌표까지의 거리를 반환합니다.
    /// </summary>
    /// <param name="other">거리를 계산할 대상 좌표입니다.</param>
    /// <returns>가로와 세로 거리 중 더 큰 값입니다.</returns>
    public int ChebyshevDistanceTo(GridPosition other)
    {
        return Math.Max(
            Math.Abs(X - other.X),
            Math.Abs(Y - other.Y));
    }

    /// <summary>
    /// 두 보드 좌표 사이의 상하좌우 타일 거리를 계산합니다.
    /// </summary>
    public int ManhattanDistanceTo(GridPosition other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    /// <summary>
    /// 다른 보드 좌표와 X, Y 값이 같은지 확인합니다.
    /// </summary>
    public bool Equals(GridPosition other)
    {
        return X == other.X && Y == other.Y;
    }

    /// <summary>
    /// 전달된 객체가 같은 보드 좌표인지 확인합니다.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is GridPosition other && Equals(other);
    }

    /// <summary>
    /// 사전과 집합에서 좌표를 식별하기 위한 해시 값을 반환합니다.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    /// 디버깅에 사용할 수 있도록 좌표를 문자열로 반환합니다.
    /// </summary>
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public static bool operator ==(GridPosition left, GridPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GridPosition left, GridPosition right)
    {
        return !left.Equals(right);
    }
}
