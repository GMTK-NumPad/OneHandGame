using NUnit.Framework;

/// <summary>
/// 몬스터의 Speed와 Instance ID 턴 우선순위를 검사합니다.
/// </summary>
public sealed class EnemyTurnOrderTests
{
    /// <summary>
    /// Speed가 높은 몬스터가 먼저 정렬되는지 검사합니다.
    /// </summary>
    [Test]
    public void HigherSpeed_ActsFirst()
    {
        int comparison = EnemyTurnOrder.Compare(
            leftSpeed: 5,
            leftInstanceId: 20,
            rightSpeed: 3,
            rightInstanceId: 10);

        Assert.That(comparison, Is.LessThan(0));
    }

    /// <summary>
    /// Speed가 같으면 Instance ID가 작은 몬스터가 먼저 정렬되는지 검사합니다.
    /// </summary>
    [Test]
    public void SameSpeed_LowerInstanceIdActsFirst()
    {
        int comparison = EnemyTurnOrder.Compare(
            leftSpeed: 5,
            leftInstanceId: 10,
            rightSpeed: 5,
            rightInstanceId: 20);

        Assert.That(comparison, Is.LessThan(0));
    }
}
