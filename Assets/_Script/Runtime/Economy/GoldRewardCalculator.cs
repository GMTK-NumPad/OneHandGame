using System;

/// <summary>
/// 기준 골드에서 일반 보상 또는 낮은 확률의 고액 보상을 계산합니다.
/// </summary>
public static class GoldRewardCalculator
{
    /// <summary>
    /// 전달된 두 난수를 사용해 재현 가능한 몬스터 골드 보상을 계산합니다.
    /// </summary>
    public static GoldRewardResult Roll(
        int baseGold,
        GoldRewardRules rules,
        double jackpotRoll,
        double amountRoll)
    {
        int safeBaseGold = Math.Max(0, baseGold);
        int normalMinimum = RoundToInt(
            safeBaseGold * (1d - rules.NormalVariance));
        int normalMaximum = RoundToInt(
            safeBaseGold * (1d + rules.NormalVariance));
        int jackpotMaximum = Math.Max(
            normalMaximum,
            RoundToInt(
                safeBaseGold * rules.JackpotMaximumMultiplier));

        bool isJackpot =
            jackpotRoll < rules.JackpotChance
            && jackpotMaximum > normalMaximum;

        int minimum = isJackpot
            ? normalMaximum + 1
            : normalMinimum;
        int maximum = isJackpot
            ? jackpotMaximum
            : normalMaximum;

        return new GoldRewardResult(
            SampleInclusive(minimum, maximum, amountRoll),
            isJackpot);
    }

    /// <summary>
    /// 0부터 1까지의 난수를 양 끝을 포함하는 정수 범위의 값으로 변환합니다.
    /// </summary>
    private static int SampleInclusive(
        int minimum,
        int maximum,
        double sample)
    {
        if (maximum <= minimum || sample <= 0d)
        {
            return minimum;
        }

        if (sample >= 1d)
        {
            return maximum;
        }

        int valueCount = maximum - minimum + 1;
        return minimum + (int)Math.Floor(sample * valueCount);
    }

    /// <summary>
    /// 중간값을 0에서 멀어지는 방향으로 반올림해 정수 골드로 변환합니다.
    /// </summary>
    private static int RoundToInt(double value)
    {
        return Math.Max(
            0,
            (int)Math.Round(
                value,
                MidpointRounding.AwayFromZero));
    }
}
