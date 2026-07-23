using NUnit.Framework;

/// <summary>
/// 몬스터 기본 이동 경로가 장애물과 점유 타일을 피해 계산되는지 검사합니다.
/// </summary>
public sealed class EnemyPathfinderTests
{
    /// <summary>
    /// 직선 경로가 막혔을 때 대각선으로 우회하는 첫 번째 빈 타일을 반환하는지 검사합니다.
    /// </summary>
    [Test]
    public void BlockedDirectPath_ReturnsWalkableDetourStep()
    {
        var board =
            new BoardState(7, 7, defaultWalkable: true);
        var start = new GridPosition(1, 1);
        var blocked = new GridPosition(2, 1);
        var target = new GridPosition(3, 1);

        board.SetWalkable(blocked, false);
        board.TryPlaceActor(10, start);
        board.TryPlaceActor(20, target);

        bool found = EnemyPathfinder.TryGetNextStep(
            board,
            movingActorId: 10,
            start,
            target,
            out GridPosition nextStep);

        Assert.That(found, Is.True);
        Assert.That(nextStep, Is.Not.EqualTo(blocked));
        Assert.That(nextStep, Is.Not.EqualTo(target));
        Assert.That(board.CanEnter(nextStep), Is.True);
        Assert.That(
            start.ChebyshevDistanceTo(nextStep),
            Is.EqualTo(1));
    }
}
