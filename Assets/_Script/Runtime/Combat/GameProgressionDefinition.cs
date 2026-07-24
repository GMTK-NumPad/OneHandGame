using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼부터 스테이지 C까지 진행할 스테이지 SO의 순서를 정의합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "GameProgression",
    menuName = "One Hand Game/Progression/Game Progression")]
public sealed class GameProgressionDefinition
    : ScriptableObject
{
    [SerializeField] private List<StageDefinition> stages =
        new();

    /// <summary>
    /// 등록된 스테이지 참조가 튜토리얼, A, B, C 순서의 앞부분으로 구성됐는지 검사합니다.
    /// 개별 스테이지의 라운드 데이터는 해당 스테이지에 실제 진입할 때 검사합니다.
    /// </summary>
    public bool IsValidSequence()
    {
        if (stages == null
            || stages.Count == 0
            || stages.Count > 4)
        {
            return false;
        }

        for (int index = 0;
             index < stages.Count;
             index++)
        {
            StageDefinition stage = stages[index];

            if (stage == null
                || stage.StageId != (GameStageId)index)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 지정한 순번의 스테이지가 존재하고 유효하면 반환합니다.
    /// </summary>
    public bool TryGetStage(
        int index,
        out StageDefinition stage)
    {
        stage = null;

        if (stages == null
            || index < 0
            || index >= stages.Count)
        {
            return false;
        }

        stage = stages[index];
        return stage != null && stage.IsValid();
    }
}
