using NUnit.Framework;

/// <summary>
/// 플레이어 턴부터 라운드 및 게임 종료까지의 핵심 전투 흐름을 검사합니다.
/// </summary>
public sealed class RoundFlowStateMachineTests
{
    /// <summary>
    /// 일반 행동 뒤 몬스터 턴이 실행되고 카운트가 감소하는지 검사합니다.
    /// </summary>
    [Test]
    public void CountedAction_RunsEnemyTurnAndDecreasesCount()
    {
        var flow = new RoundFlowStateMachine(10);
        flow.StartFirstRound(3);

        Assert.That(
            flow.CompletePlayerTurn(),
            Is.True);
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.EnemyTurn));

        Assert.That(flow.CompleteEnemyTurn(), Is.True);
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.PlayerTurn));
        Assert.That(flow.RemainingCount, Is.EqualTo(9));
    }

    /// <summary>
    /// 마지막 몬스터를 처치하면 몬스터 턴 전에 라운드가 완료되는지 검사합니다.
    /// </summary>
    [Test]
    public void DefeatingLastEnemy_ClearsRoundBeforeEnemyTurn()
    {
        var flow = new RoundFlowStateMachine(10);
        flow.StartFirstRound(1);

        Assert.That(flow.ReportEnemyDefeated(), Is.True);

        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.BetweenRounds));
        Assert.That(flow.Resolution, Is.EqualTo(RoundResolution.Cleared));
        Assert.That(flow.RemainingCount, Is.EqualTo(10));
    }

    /// <summary>
    /// 남은 카운트가 0이 되면 게임이 패배로 종료되는지 검사합니다.
    /// </summary>
    [Test]
    public void ReachingZeroCount_EndsRun()
    {
        var flow = new RoundFlowStateMachine(1);
        flow.StartFirstRound(1);

        flow.CompletePlayerTurn();
        flow.CompleteEnemyTurn();

        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.RunEnded));
        Assert.That(flow.Resolution, Is.EqualTo(RoundResolution.CountExpired));
        Assert.That(flow.RemainingCount, Is.Zero);
    }

    /// <summary>
    /// 플레이어 패배 보고가 즉시 게임 종료 상태로 이어지는지 검사합니다.
    /// </summary>
    [Test]
    public void PlayerDefeat_EndsRunImmediately()
    {
        var flow = new RoundFlowStateMachine(10);
        flow.StartFirstRound(2);

        Assert.That(flow.ReportPlayerDefeated(), Is.True);

        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.RunEnded));
        Assert.That(flow.Resolution, Is.EqualTo(RoundResolution.PlayerDefeated));
    }

    /// <summary>
    /// 다음 라운드에서 번호와 몬스터 수가 갱신되고 카운트가 초기화되는지 검사합니다.
    /// </summary>
    [Test]
    public void NextRound_IncrementsRoundAndResetsCount()
    {
        var flow = new RoundFlowStateMachine(10);
        flow.StartFirstRound(1);
        flow.ReportEnemyDefeated();

        Assert.That(flow.StartNextRound(2), Is.True);

        Assert.That(flow.RoundNumber, Is.EqualTo(2));
        Assert.That(flow.RemainingCount, Is.EqualTo(10));
        Assert.That(flow.AliveEnemyCount, Is.EqualTo(2));
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.PlayerTurn));
    }
}
