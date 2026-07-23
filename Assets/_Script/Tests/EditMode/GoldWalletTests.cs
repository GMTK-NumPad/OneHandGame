using NUnit.Framework;

/// <summary>
/// 총 골드 누적과 잘못된 음수 입력 방지 규칙을 검사합니다.
/// </summary>
public sealed class GoldWalletTests
{
    /// <summary>
    /// 여러 번 획득한 골드가 총 골드에 누적되는지 검사합니다.
    /// </summary>
    [Test]
    public void AddGold_AccumulatesRewards()
    {
        var wallet = new GoldWallet();

        wallet.AddGold(70);
        wallet.AddGold(130);

        Assert.That(wallet.TotalGold, Is.EqualTo(200));
    }

    /// <summary>
    /// 음수 골드가 총 골드를 감소시키지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void AddGold_NegativeAmountIsIgnored()
    {
        var wallet = new GoldWallet();

        int addedAmount = wallet.AddGold(-100);

        Assert.That(addedAmount, Is.Zero);
        Assert.That(wallet.TotalGold, Is.Zero);
    }
}
