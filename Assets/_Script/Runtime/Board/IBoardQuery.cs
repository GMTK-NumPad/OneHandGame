/// <summary>
/// 이동 계획 계산에 필요한 보드 크기, 타일과 점유 상태를 읽기 전용으로 제공합니다.
/// </summary>
public interface IBoardQuery
{
    int Width { get; }
    int Height { get; }

    /// <summary>
    /// 지정한 좌표가 보드 범위 안에 있는지 확인합니다.
    /// </summary>
    bool IsInside(GridPosition position);

    /// <summary>
    /// 지정한 좌표의 타일이 이동 가능한 지형인지 확인합니다.
    /// </summary>
    bool IsWalkable(GridPosition position);

    /// <summary>
    /// 지정한 좌표가 이동 가능한 지형이며 다른 액터가 점유하지 않았는지 확인합니다.
    /// </summary>
    bool CanEnter(GridPosition position);

    /// <summary>
    /// 지정한 좌표를 점유 중인 액터의 Instance ID를 가져옵니다.
    /// </summary>
    bool TryGetOccupant(GridPosition position, out int actorId);
}
