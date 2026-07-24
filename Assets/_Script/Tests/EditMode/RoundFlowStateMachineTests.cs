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
    /// 환경 효과의 즉시 카운트 감소가 상태를 알리고 0에서 카운트 만료 패배로 이어지는지 검사합니다.
    /// </summary>
    [Test]
    public void EnvironmentCountReduction_UpdatesAndCanEndRun()
    {
        var flow = new RoundFlowStateMachine(3);
        flow.StartFirstRound(1);

        Assert.That(flow.ReduceRemainingCount(2), Is.True);
        Assert.That(flow.RemainingCount, Is.EqualTo(1));
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.PlayerTurn));

        Assert.That(flow.ReduceRemainingCount(1), Is.True);
        Assert.That(flow.RemainingCount, Is.Zero);
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.RunEnded));
        Assert.That(
            flow.Resolution,
            Is.EqualTo(RoundResolution.CountExpired));
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
    /// 다음 라운드에서 번호와 몬스터 수가 갱신되며 정산된 카운트다운이 유지되는지 검사합니다.
    /// </summary>
    [Test]
    public void NextRound_IncrementsRoundAndKeepsCountdown()
    {
        var flow = new RoundFlowStateMachine(10);
        flow.StartFirstRound(1);
        flow.CompletePlayerTurn();
        flow.CompleteEnemyTurn();
        flow.ReportEnemyDefeated();

        Assert.That(flow.StartNextRound(2), Is.True);

        Assert.That(flow.RoundNumber, Is.EqualTo(2));
        Assert.That(flow.RemainingCount, Is.EqualTo(10));
        Assert.That(flow.AliveEnemyCount, Is.EqualTo(2));
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.PlayerTurn));
    }

    /// <summary>
    /// 라운드 클리어 시 기본, 무피해와 소모품 미사용 보너스를 더하고 초과 카운트를 골드로 바꾸는지 검사합니다.
    /// </summary>
    [Test]
    public void ClearRound_AwardsCountdownAndConvertsOverflowToGold()
    {
        var flow = new RoundFlowStateMachine(
            startingCount: 10,
            maximumCount: 10,
            clearCountdownReward: 3,
            noDamageCountdownReward: 2,
            noConsumableCountdownReward: 1,
            overflowGoldPerCountdown: 25);
        flow.StartFirstRound(1);
        flow.CompletePlayerTurn();
        flow.CompleteEnemyTurn();

        flow.ReportEnemyDefeated();

        Assert.That(
            flow.LastClearReward.TotalCountdownReward,
            Is.EqualTo(6));
        Assert.That(
            flow.LastClearReward.AddedCountdown,
            Is.EqualTo(1));
        Assert.That(
            flow.LastClearReward.OverflowCountdown,
            Is.EqualTo(5));
        Assert.That(
            flow.LastClearReward.OverflowGold,
            Is.EqualTo(125));
        Assert.That(flow.RemainingCount, Is.EqualTo(10));
        Assert.That(
            flow.TotalBonusCountdownEarned,
            Is.EqualTo(6));
    }

    /// <summary>
    /// 실제 피해와 소모품 사용 기록이 해당 보너스를 제외하고 다음 라운드에서 초기화되는지 검사합니다.
    /// </summary>
    [Test]
    public void ClearRound_ExcludesFailedConditionBonuses()
    {
        var flow = new RoundFlowStateMachine(
            startingCount: 10,
            maximumCount: 10,
            clearCountdownReward: 3,
            noDamageCountdownReward: 2,
            noConsumableCountdownReward: 1,
            overflowGoldPerCountdown: 1);
        flow.StartFirstRound(1);
        flow.ReportPlayerDamageTaken(2);
        flow.ReportConsumableUsed();

        flow.ReportEnemyDefeated();

        Assert.That(
            flow.LastClearReward.TotalCountdownReward,
            Is.EqualTo(3));
        Assert.That(flow.TotalDamageTaken, Is.EqualTo(2));

        flow.StartNextRound(1);

        Assert.That(flow.DamageTakenThisRound, Is.Zero);
        Assert.That(flow.ConsumableUsedThisRound, Is.False);
    }

    /// <summary>
    /// 일반 라운드는 정산 카운트를 유지하지만 새 스테이지 첫 라운드는 시작값으로 설정되는지 검사합니다.
    /// </summary>
    [Test]
    public void StageFirstRound_ResetsCountdownOnlyAtStageBoundary()
    {
        var flow = new RoundFlowStateMachine(
            startingCount: 10,
            maximumCount: 10,
            clearCountdownReward: 0,
            noDamageCountdownReward: 0,
            noConsumableCountdownReward: 0,
            overflowGoldPerCountdown: 0);
        flow.StartFirstRound(1);
        flow.CompletePlayerTurn();
        flow.CompleteEnemyTurn();
        flow.ReportEnemyDefeated();

        flow.StartNextRound(
            enemyCount: 1,
            isFirstRoundOfStage: false);

        Assert.That(flow.RemainingCount, Is.EqualTo(9));

        flow.ReportEnemyDefeated();
        flow.StartNextRound(
            enemyCount: 1,
            isFirstRoundOfStage: true);

        Assert.That(flow.RemainingCount, Is.EqualTo(10));
    }

    /// <summary>
    /// 마지막 적을 처치한 행동도 몬스터 턴 전환 여부와 관계없이 진행 턴에 포함되는지 검사합니다.
    /// </summary>
    [Test]
    public void ClearAction_IsIncludedInTotalTurns()
    {
        var flow = new RoundFlowStateMachine(10);
        flow.StartFirstRound(1);

        Assert.That(flow.RecordPlayerAction(), Is.True);
        flow.ReportEnemyDefeated();

        Assert.That(flow.TotalTurnsPlayed, Is.EqualTo(1));
    }

    /// <summary>
    /// 마지막 스테이지는 클리어 보상 정산 없이 즉시 게임 클리어로 종료되는지 검사합니다.
    /// </summary>
    [Test]
    public void FinalRoundClear_EndsImmediatelyWithoutClearRewards()
    {
        var flow = new RoundFlowStateMachine(
            startingCount: 10,
            maximumCount: 10,
            clearCountdownReward: 3,
            noDamageCountdownReward: 2,
            noConsumableCountdownReward: 1,
            overflowGoldPerCountdown: 10);
        bool roundClearedRaised = false;
        flow.RoundCleared +=
            _ => roundClearedRaised = true;
        flow.StartFirstRound(
            enemyCount: 1,
            completesRunWhenCleared: true);

        Assert.That(flow.ReportEnemyDefeated(), Is.True);
        Assert.That(flow.Phase, Is.EqualTo(RoundPhase.RunEnded));
        Assert.That(
            flow.Resolution,
            Is.EqualTo(RoundResolution.GameCleared));
        Assert.That(
            flow.TotalBonusCountdownEarned,
            Is.Zero);
        Assert.That(
            flow.LastClearReward.OverflowGold,
            Is.Zero);
        Assert.That(roundClearedRaised, Is.False);
    }
}
