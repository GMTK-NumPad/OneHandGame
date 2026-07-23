using NUnit.Framework;

/// <summary>
/// 몬스터 생성 시 일반 골드와 희귀 고액 골드 범위를 검사합니다.
/// </summary>
public sealed class GoldRewardCalculatorTests
{
    private static readonly GoldRewardRules Rules =
        new GoldRewardRules(0.3f, 0.025f, 2f);

    /// <summary>
    /// 일반 보상이 기준 골드의 70퍼센트부터 130퍼센트까지 나오는지 검사합니다.
    /// </summary>
    [Test]
    public void NormalReward_UsesThirtyPercentVariance()
    {
        GoldRewardResult minimum = GoldRewardCalculator.Roll(
            100,
            Rules,
            jackpotRoll: 1d,
            amountRoll: 0d);
        GoldRewardResult maximum = GoldRewardCalculator.Roll(
            100,
            Rules,
            jackpotRoll: 1d,
            amountRoll: 1d);

        Assert.That(minimum.Amount, Is.EqualTo(70));
        Assert.That(maximum.Amount, Is.EqualTo(130));
        Assert.That(minimum.IsJackpot, Is.False);
        Assert.That(maximum.IsJackpot, Is.False);
    }

    /// <summary>
    /// 희귀 보상이 일반 최댓값 다음 값부터 기준 골드 두 배까지 나오는지 검사합니다.
    /// </summary>
    [Test]
    public void JackpotReward_UsesExclusiveHighRange()
    {
        GoldRewardResult minimum = GoldRewardCalculator.Roll(
            100,
            Rules,
            jackpotRoll: 0d,
            amountRoll: 0d);
        GoldRewardResult maximum = GoldRewardCalculator.Roll(
            100,
            Rules,
            jackpotRoll: 0d,
            amountRoll: 1d);

        Assert.That(minimum.Amount, Is.EqualTo(131));
        Assert.That(maximum.Amount, Is.EqualTo(200));
        Assert.That(minimum.IsJackpot, Is.True);
        Assert.That(maximum.IsJackpot, Is.True);
    }
}
