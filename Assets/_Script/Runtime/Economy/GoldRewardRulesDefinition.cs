using UnityEngine;

/// <summary>
/// 모든 몬스터 골드 보상에 공통으로 적용할 확률과 범위를 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "GoldRewardRules",
    menuName = "One Hand Game/Definitions/Gold Reward Rules")]
public sealed class GoldRewardRulesDefinition : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] private float normalVariance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float jackpotChance = 0.025f;
    [SerializeField, Min(1f)] private float jackpotMaximumMultiplier = 2f;

    public float NormalVariance => Mathf.Clamp01(normalVariance);
    public float JackpotChance => Mathf.Clamp01(jackpotChance);
    public float JackpotMaximumMultiplier =>
        Mathf.Max(1f, jackpotMaximumMultiplier);

    /// <summary>
    /// Inspector 값을 이용해 순수 골드 계산 규칙을 생성합니다.
    /// </summary>
    public GoldRewardRules CreateRules()
    {
        return new GoldRewardRules(
            NormalVariance,
            JackpotChance,
            JackpotMaximumMultiplier);
    }

    /// <summary>
    /// 몬스터가 생성될 때 기준 골드로 이번 인스턴스의 보상을 한 번 결정합니다.
    /// </summary>
    public GoldRewardResult Roll(int baseGold)
    {
        return GoldRewardCalculator.Roll(
            baseGold,
            CreateRules(),
            Random.value,
            Random.value);
    }
}
