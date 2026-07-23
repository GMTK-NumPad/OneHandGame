using System;

/// <summary>
/// 한 번의 게임 진행에서 플레이어가 획득한 총 골드를 보관합니다.
/// </summary>
public sealed class GoldWallet
{
    public int TotalGold { get; private set; }

    /// <summary>
    /// 양수 골드를 최대 정수 범위 안에서 누적하고 실제 추가된 양을 반환합니다.
    /// </summary>
    public int AddGold(int amount)
    {
        int safeAmount = Math.Max(0, amount);
        int addedAmount = Math.Min(
            safeAmount,
            int.MaxValue - TotalGold);
        TotalGold += addedAmount;
        return addedAmount;
    }
}
