using NUnit.Framework;

/// <summary>
/// 보드 범위, 이동 가능 타일과 액터 점유 규칙을 검사합니다.
/// </summary>
public sealed class BoardStateTests
{
    /// <summary>
    /// 7×7 보드의 유효 범위와 중앙 좌표가 올바른지 검사합니다.
    /// </summary>
    [Test]
    public void SevenBySevenBoard_HasExpectedBoundsAndCenter()
    {
        var board = new BoardState(7, 7, defaultWalkable: true);

        Assert.That(board.Center, Is.EqualTo(new GridPosition(3, 3)));
        Assert.That(board.IsInside(new GridPosition(0, 0)), Is.True);
        Assert.That(board.IsInside(new GridPosition(6, 6)), Is.True);
        Assert.That(board.IsInside(new GridPosition(7, 6)), Is.False);
        Assert.That(board.IsInside(new GridPosition(-1, 0)), Is.False);
    }

    /// <summary>
    /// 이동 불가 타일과 이미 점유된 타일에는 액터를 배치할 수 없는지 검사합니다.
    /// </summary>
    [Test]
    public void Placement_RejectsBlockedAndOccupiedTiles()
    {
        var board = new BoardState(7, 7, defaultWalkable: true);
        var blockedPosition = new GridPosition(0, 0);
        var occupiedPosition = new GridPosition(3, 3);

        board.SetWalkable(blockedPosition, false);

        Assert.That(board.TryPlaceActor(1, blockedPosition), Is.False);
        Assert.That(board.TryPlaceActor(1, occupiedPosition), Is.True);
        Assert.That(board.TryPlaceActor(2, occupiedPosition), Is.False);
    }

    /// <summary>
    /// 액터 이동 후 이전 타일이 다시 비고 새 타일이 점유되는지 검사합니다.
    /// </summary>
    [Test]
    public void Movement_ReleasesPreviousTileAndOccupiesTargetTile()
    {
        var board = new BoardState(7, 7, defaultWalkable: true);
        var start = new GridPosition(3, 3);
        var target = new GridPosition(4, 3);

        board.TryPlaceActor(1, start);

        Assert.That(board.TryMoveActor(1, target), Is.True);
        Assert.That(board.IsOccupied(start), Is.False);
        Assert.That(board.IsOccupied(target), Is.True);
        Assert.That(board.TryGetActorPosition(1, out GridPosition position), Is.True);
        Assert.That(position, Is.EqualTo(target));
    }

    /// <summary>
    /// 액터를 제거하면 해당 타일에 다른 액터를 배치할 수 있는지 검사합니다.
    /// </summary>
    [Test]
    public void Removal_ReleasesOccupiedTile()
    {
        var board = new BoardState(7, 7, defaultWalkable: true);
        var position = new GridPosition(3, 3);

        board.TryPlaceActor(1, position);

        Assert.That(board.RemoveActor(1), Is.True);
        Assert.That(board.TryPlaceActor(2, position), Is.True);
    }
}
