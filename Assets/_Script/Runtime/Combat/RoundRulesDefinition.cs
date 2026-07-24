using UnityEngine;

/// <summary>
/// 카운트다운 최대치와 라운드 클리어 보너스처럼 공통으로 사용하는 전투 규칙을 정의합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "RoundRules",
    menuName = "One Hand Game/Definitions/Round Rules")]
public sealed class RoundRulesDefinition : ScriptableObject
{
    [SerializeField, Min(1)] private int startingCount = 10;
    [SerializeField, Min(1)] private int maximumCount = 10;
    [SerializeField, Min(0)] private int clearCountdownReward = 3;
    [SerializeField, Min(0)] private int noDamageCountdownReward = 2;
    [SerializeField, Min(0)] private int noConsumableCountdownReward = 1;
    [SerializeField, Min(0)] private int overflowGoldPerCountdown = 10;

    public int StartingCount => Mathf.Max(1, startingCount);
    public int MaximumCount =>
        Mathf.Max(StartingCount, maximumCount);
    public int ClearCountdownReward =>
        Mathf.Max(0, clearCountdownReward);
    public int NoDamageCountdownReward =>
        Mathf.Max(0, noDamageCountdownReward);
    public int NoConsumableCountdownReward =>
        Mathf.Max(0, noConsumableCountdownReward);
    public int OverflowGoldPerCountdown =>
        Mathf.Max(0, overflowGoldPerCountdown);

    /// <summary>
    /// 현재 SO에 설정된 카운트다운 및 클리어 보상 규칙으로 새로운 라운드 상태 머신을 생성합니다.
    /// </summary>
    public RoundFlowStateMachine CreateStateMachine()
    {
        return new RoundFlowStateMachine(
            StartingCount,
            MaximumCount,
            ClearCountdownReward,
            NoDamageCountdownReward,
            NoConsumableCountdownReward,
            OverflowGoldPerCountdown);
    }

    /// <summary>
    /// Inspector에서 최대 카운트다운이 시작 카운트다운보다 작아지지 않도록 보정합니다.
    /// </summary>
    private void OnValidate()
    {
        startingCount = Mathf.Max(1, startingCount);
        maximumCount = Mathf.Max(
            startingCount,
            maximumCount);
    }
}
