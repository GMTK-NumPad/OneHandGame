using NUnit.Framework;

/// <summary>
/// 몬스터 생성 시 일반 골드와 희귀 고액 골드 범위를 검사합니다.
/// </summary>
public sealed class GoldRewardCalculatorTests
{
    private static readonly GoldRewardRules Rules =
        new GoldRewardRules(0.3f, 0.025f, 2f, 0.1f);

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
    /// 희귀 보상이 기준 골드 두 배를 중심으로 10퍼센트 오차 범위인지 검사합니다.
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

        Assert.That(minimum.Amount, Is.EqualTo(180));
        Assert.That(maximum.Amount, Is.EqualTo(220));
        Assert.That(minimum.IsJackpot, Is.True);
        Assert.That(maximum.IsJackpot, Is.True);
    }
}
