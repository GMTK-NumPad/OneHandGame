/// <summary>
/// 라운드 클리어 뒤 상점, 스킬 선택과 게임 클리어 순서를 나타냅니다.
/// </summary>
public readonly struct StageTransitionPlan
{
    /// <summary>
    /// 현재 라운드 완료 결과에 필요한 팝업과 다음 진행 여부를 생성합니다.
    /// </summary>
    public StageTransitionPlan(
        bool requiresShop,
        bool requiresSkillSelection,
        bool completesStage,
        bool completesRun)
    {
        RequiresShop = requiresShop;
        RequiresSkillSelection =
            requiresSkillSelection;
        CompletesStage = completesStage;
        CompletesRun = completesRun;
    }

    public bool RequiresShop { get; }
    public bool RequiresSkillSelection { get; }
    public bool CompletesStage { get; }
    public bool CompletesRun { get; }
}

/// <summary>
/// 스테이지와 라운드 번호만으로 클리어 후 필요한 화면 순서를 계산합니다.
/// </summary>
public static class StageProgressionCalculator
{
    /// <summary>
    /// 매 세 번째 라운드의 상점과 정규 스테이지 완료 시 스킬 선택 및 C 완료를 판정합니다.
    /// </summary>
    public static StageTransitionPlan CreatePlan(
        GameStageId stageId,
        int clearedRoundInStage,
        int totalRoundsInStage)
    {
        bool completesStage =
            clearedRoundInStage >= totalRoundsInStage;
        bool completesRun =
            completesStage
            && stageId == GameStageId.C;
        bool requiresShop =
            !completesRun
            && clearedRoundInStage > 0
            && clearedRoundInStage % 3 == 0;
        bool requiresSkillSelection =
            completesStage
            && stageId != GameStageId.Tutorial
            && stageId != GameStageId.C;

        return new StageTransitionPlan(
            requiresShop,
            requiresSkillSelection,
            completesStage,
            completesRun);
    }
}
