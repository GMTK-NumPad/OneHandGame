using UnityEngine;

/// <summary>
/// 라운드 카운트와 특수 상점 주기처럼 공통으로 사용하는 전투 규칙을 정의합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "RoundRules",
    menuName = "One Hand Game/Definitions/Round Rules")]
public sealed class RoundRulesDefinition : ScriptableObject
{
    [SerializeField, Min(1)] private int startingCount = 10;
    [SerializeField, Min(1)] private int specialShopInterval = 5;

    public int StartingCount => Mathf.Max(1, startingCount);
    public int SpecialShopInterval => Mathf.Max(1, specialShopInterval);

    /// <summary>
    /// 완료한 라운드 뒤에 특수 상점이 등장하는지 확인합니다.
    /// </summary>
    public bool HasSpecialShopAfter(int clearedRound)
    {
        return clearedRound > 0 && clearedRound % SpecialShopInterval == 0;
    }

    /// <summary>
    /// 현재 SO에 설정된 카운트로 새로운 라운드 상태 머신을 생성합니다.
    /// </summary>
    public RoundFlowStateMachine CreateStateMachine()
    {
        return new RoundFlowStateMachine(StartingCount);
    }
}
