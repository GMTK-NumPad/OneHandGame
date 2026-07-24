using NUnit.Framework;

/// <summary>
/// 스테이지 라운드 번호에 따른 상점, 스킬 선택과 게임 클리어 전환을 검사합니다.
/// </summary>
public sealed class StageProgressionCalculatorTests
{
    /// <summary>
    /// 튜토리얼 세 번째 라운드는 상점만 요청하고 스킬 선택은 요청하지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void TutorialRoundThree_RequestsOnlyShop()
    {
        StageTransitionPlan plan =
            StageProgressionCalculator.CreatePlan(
                GameStageId.Tutorial,
                clearedRoundInStage: 3,
                totalRoundsInStage: 3);

        Assert.That(plan.RequiresShop, Is.True);
        Assert.That(
            plan.RequiresSkillSelection,
            Is.False);
        Assert.That(plan.CompletesStage, Is.True);
        Assert.That(plan.CompletesRun, Is.False);
    }

    /// <summary>
    /// 정규 스테이지 세 번째 라운드는 상점만 요청하고 아직 스테이지를 완료하지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void RegularRoundThree_RequestsShop()
    {
        StageTransitionPlan plan =
            StageProgressionCalculator.CreatePlan(
                GameStageId.A,
                clearedRoundInStage: 3,
                totalRoundsInStage: 6);

        Assert.That(plan.RequiresShop, Is.True);
        Assert.That(plan.CompletesStage, Is.False);
        Assert.That(
            plan.RequiresSkillSelection,
            Is.False);
    }

    /// <summary>
    /// 스테이지 A의 여섯 번째 라운드는 상점 뒤 스킬 선택을 요청하는지 검사합니다.
    /// </summary>
    [Test]
    public void StageASix_RequestsShopThenSkill()
    {
        StageTransitionPlan plan =
            StageProgressionCalculator.CreatePlan(
                GameStageId.A,
                clearedRoundInStage: 6,
                totalRoundsInStage: 6);

        Assert.That(plan.RequiresShop, Is.True);
        Assert.That(
            plan.RequiresSkillSelection,
            Is.True);
        Assert.That(plan.CompletesStage, Is.True);
        Assert.That(plan.CompletesRun, Is.False);
    }

    /// <summary>
    /// 스테이지 C의 여섯 번째 라운드는 상점 뒤 스킬 선택 없이 게임을 완료하는지 검사합니다.
    /// </summary>
    [Test]
    public void StageCSix_CompletesRunWithoutShop()
    {
        StageTransitionPlan plan =
            StageProgressionCalculator.CreatePlan(
                GameStageId.C,
                clearedRoundInStage: 6,
                totalRoundsInStage: 6);

        Assert.That(plan.RequiresShop, Is.False);
        Assert.That(
            plan.RequiresSkillSelection,
            Is.False);
        Assert.That(plan.CompletesRun, Is.True);
    }
}
