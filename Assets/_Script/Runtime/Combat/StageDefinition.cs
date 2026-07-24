using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 진행에서 사용하는 튜토리얼 및 세 개의 정규 스테이지를 구분합니다.
/// </summary>
public enum GameStageId
{
    Tutorial,
    A,
    B,
    C
}

/// <summary>
/// 한 스테이지의 식별자와 순서대로 진행할 라운드 정의 목록을 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "StageDefinition",
    menuName = "One Hand Game/Progression/Stage Definition")]
public sealed class StageDefinition : ScriptableObject
{
    [SerializeField] private GameStageId stageId =
        GameStageId.Tutorial;
    [SerializeField] private List<RoundDefinition> rounds =
        new();

    public GameStageId StageId => stageId;
    public bool IsTutorial =>
        stageId == GameStageId.Tutorial;
    public int ExpectedRoundCount =>
        IsTutorial ? 3 : 6;
    public IReadOnlyList<RoundDefinition> Rounds =>
        rounds;
    public int RoundCount =>
        rounds != null ? rounds.Count : 0;

    /// <summary>
    /// 스테이지 종류에 맞는 라운드 수와 모든 라운드 SO가 지정되었는지 검사합니다.
    /// </summary>
    public bool IsValid()
    {
        if (rounds == null
            || rounds.Count != ExpectedRoundCount)
        {
            return false;
        }

        foreach (RoundDefinition round in rounds)
        {
            if (round == null)
            {
                return false;
            }
        }

        return true;
    }
}
