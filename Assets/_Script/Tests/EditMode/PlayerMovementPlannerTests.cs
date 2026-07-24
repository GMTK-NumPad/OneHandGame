using NUnit.Framework;

/// <summary>
/// 플레이어의 8방향 기본 이동과 Rampage 이동 및 공격 규칙을 검사합니다.
/// </summary>
public sealed class PlayerMovementPlannerTests
{
    private const int PlayerId = 1;
    private const int MonsterId = 2;

    /// <summary>
    /// 직선상에 몬스터가 없으면 대각선을 포함해 기본 한 칸만 이동하는지 검사합니다.
    /// </summary>
    [Test]
    public void NoMonsterInLine_MovesOneTileDiagonally()
    {
        BoardState board = CreateBoardWithPlayer(new GridPosition(1, 1));

        PlayerMovementPlan plan = PlayerMovementPlanner.CreatePlan(
            board,
            PlayerId,
            new GridPosition(1, 1),
            new GridPosition(1, 1),
            baseMoveRange: 1,
            rampageDistance: 2);

        Assert.That(plan.Destination, Is.EqualTo(new GridPosition(2, 2)));
        Assert.That(plan.ShouldMove, Is.True);
        Assert.That(plan.ShouldAttack, Is.False);
    }

    /// <summary>
    /// Rampage를 모두 이동에 사용하면 몬스터 앞에 도달해도 공격하지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void RampageSpentOnMovement_ReachesFrontWithoutAttack()
    {
        var start = new GridPosition(0, 0);
        BoardState board = CreateBoardWithPlayer(start);
        board.TryPlaceActor(MonsterId, new GridPosition(4, 0));

        PlayerMovementPlan plan = PlayerMovementPlanner.CreatePlan(
            board,
            PlayerId,
            start,
            new GridPosition(1, 0),
            baseMoveRange: 1,
            rampageDistance: 2);

        Assert.That(plan.Destination, Is.EqualTo(new GridPosition(3, 0)));
        Assert.That(plan.RampageRemaining, Is.Zero);
        Assert.That(plan.ShouldAttack, Is.False);
    }

    /// <summary>
    /// 몬스터 앞에 도달한 뒤 Rampage가 남아 있으면 공격하는지 검사합니다.
    /// </summary>
    [Test]
    public void RampageRemainingAfterMovement_AttacksTarget()
    {
        var start = new GridPosition(0, 0);
        BoardState board = CreateBoardWithPlayer(start);
        board.TryPlaceActor(MonsterId, new GridPosition(3, 0));

        PlayerMovementPlan plan = PlayerMovementPlanner.CreatePlan(
            board,
            PlayerId,
            start,
            new GridPosition(1, 0),
            baseMoveRange: 1,
            rampageDistance: 2);

        Assert.That(plan.Destination, Is.EqualTo(new GridPosition(2, 0)));
        Assert.That(plan.RampageRemaining, Is.EqualTo(1));
        Assert.That(plan.ShouldAttack, Is.True);
        Assert.That(plan.AttackTargetActorId, Is.EqualTo(MonsterId));
    }

    /// <summary>
    /// 인접한 몬스터 방향을 입력하면 이동하지 않고 즉시 공격하는지 검사합니다.
    /// </summary>
    [Test]
    public void AdjacentMonster_IsAttackedWithoutMovement()
    {
        var start = new GridPosition(3, 3);
        BoardState board = CreateBoardWithPlayer(start);
        board.TryPlaceActor(MonsterId, new GridPosition(4, 4));

        PlayerMovementPlan plan = PlayerMovementPlanner.CreatePlan(
            board,
            PlayerId,
            start,
            new GridPosition(1, 1),
            baseMoveRange: 1,
            rampageDistance: 0);

        Assert.That(plan.Destination, Is.EqualTo(start));
        Assert.That(plan.ShouldMove, Is.False);
        Assert.That(plan.ShouldAttack, Is.True);
    }

    /// <summary>
    /// 속박 중 인접한 몬스터 방향 입력은 이동 없이 공격 행동으로 처리되는지 검사합니다.
    /// </summary>
    [Test]
    public void BoundWithAdjacentMonster_AttacksWithoutMoving()
    {
        var start = new GridPosition(3, 3);
        BoardState board = CreateBoardWithPlayer(start);
        board.TryPlaceActor(MonsterId, new GridPosition(4, 3));

        PlayerMovementPlan plan =
            PlayerMovementPlanner.CreateBoundPlan(
                board,
                PlayerId,
                start,
                new GridPosition(1, 0));

        Assert.That(plan.Destination, Is.EqualTo(start));
        Assert.That(plan.ShouldMove, Is.False);
        Assert.That(plan.ShouldAttack, Is.True);
        Assert.That(plan.CanAct, Is.True);
    }

    /// <summary>
    /// 속박 중 빈 방향 입력은 이동과 공격 없이도 플레이어 행동 한 번을 소비하는지 검사합니다.
    /// </summary>
    [Test]
    public void BoundWithEmptyDirection_ConsumesActionWithoutMoving()
    {
        var start = new GridPosition(3, 3);
        BoardState board = CreateBoardWithPlayer(start);

        PlayerMovementPlan plan =
            PlayerMovementPlanner.CreateBoundPlan(
                board,
                PlayerId,
                start,
                new GridPosition(1, 0));

        Assert.That(plan.Destination, Is.EqualTo(start));
        Assert.That(plan.ShouldMove, Is.False);
        Assert.That(plan.ShouldAttack, Is.False);
        Assert.That(plan.CanAct, Is.True);
    }

    /// <summary>
    /// 몬스터와 플레이어 사이에 이동 불가 타일이 있으면 Rampage가 발동하지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void ObstacleBeforeMonster_DisablesRampage()
    {
        var start = new GridPosition(0, 0);
        BoardState board = CreateBoardWithPlayer(start);
        board.SetWalkable(new GridPosition(2, 0), false);
        board.TryPlaceActor(MonsterId, new GridPosition(4, 0));

        PlayerMovementPlan plan = PlayerMovementPlanner.CreatePlan(
            board,
            PlayerId,
            start,
            new GridPosition(1, 0),
            baseMoveRange: 1,
            rampageDistance: 2);

        Assert.That(plan.Destination, Is.EqualTo(new GridPosition(1, 0)));
        Assert.That(plan.ShouldAttack, Is.False);
        Assert.That(plan.RampageRemaining, Is.Zero);
    }

    /// <summary>
    /// 플레이어가 지정한 위치에 배치된 이동 가능한 테스트 보드를 생성합니다.
    /// </summary>
    private static BoardState CreateBoardWithPlayer(GridPosition position)
    {
        var board = new BoardState(7, 7, defaultWalkable: true);
        board.TryPlaceActor(PlayerId, position);
        return board;
    }
}
