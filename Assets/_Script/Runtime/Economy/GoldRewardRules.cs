using System;

/// <summary>
/// 몬스터 골드 보상의 일반 오차 범위와 희귀 고액 보상 확률을 정의합니다.
/// </summary>
public readonly struct GoldRewardRules
{
    /// <summary>
    /// 골드 보상 계산에 사용할 비율들을 유효한 범위로 제한해 생성합니다.
    /// </summary>
    public GoldRewardRules(
        float normalVariance,
        float jackpotChance,
        float jackpotMultiplier,
        float jackpotVariance)
    {
        NormalVariance = Math.Clamp(normalVariance, 0f, 1f);
        JackpotChance = Math.Clamp(jackpotChance, 0f, 1f);
        JackpotMultiplier = Math.Max(
            1f,
            jackpotMultiplier);
        JackpotVariance = Math.Clamp(jackpotVariance, 0f, 1f);
    }

    public float NormalVariance { get; }
    public float JackpotChance { get; }
    public float JackpotMultiplier { get; }
    public float JackpotVariance { get; }
}

/// <summary>
/// 한 몬스터가 생성될 때 확정된 골드 보상과 희귀 보상 여부를 보관합니다.
/// </summary>
public readonly struct GoldRewardResult
{
    /// <summary>
    /// 확정된 골드 금액과 희귀 고액 보상 여부를 생성합니다.
    /// </summary>
    public GoldRewardResult(int amount, bool isJackpot)
    {
        Amount = Math.Max(0, amount);
        IsJackpot = isJackpot;
    }

    public int Amount { get; }
    public bool IsJackpot { get; }
}
