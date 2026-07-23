using System;
using System.Collections.Generic;

/// <summary>
/// 몬스터 턴 실행 순서를 Speed 내림차순과 Instance ID 오름차순으로 정렬합니다.
/// </summary>
public static class EnemyTurnOrder
{
    /// <summary>
    /// 원본 목록을 변경하지 않고 이번 몬스터 턴에 사용할 실행 순서를 반환합니다.
    /// </summary>
    public static List<EnemyRuntimeState> Create(
        IEnumerable<EnemyRuntimeState> enemies)
    {
        if (enemies == null)
        {
            throw new ArgumentNullException(nameof(enemies));
        }

        var ordered = new List<EnemyRuntimeState>();

        foreach (EnemyRuntimeState enemy in enemies)
        {
            if (enemy != null && !enemy.IsDefeated)
            {
                ordered.Add(enemy);
            }
        }

        ordered.Sort(Compare);
        return ordered;
    }

    /// <summary>
    /// 두 몬스터의 Speed와 Instance ID를 기준으로 정렬 우선순위를 비교합니다.
    /// </summary>
    public static int Compare(
        int leftSpeed,
        int leftInstanceId,
        int rightSpeed,
        int rightInstanceId)
    {
        int speedComparison = rightSpeed.CompareTo(leftSpeed);
        return speedComparison != 0
            ? speedComparison
            : leftInstanceId.CompareTo(rightInstanceId);
    }

    /// <summary>
    /// 런타임 몬스터 두 개의 턴 순서를 비교합니다.
    /// </summary>
    private static int Compare(
        EnemyRuntimeState left,
        EnemyRuntimeState right)
    {
        return Compare(
            left.Definition.Speed,
            left.InstanceId,
            right.Definition.Speed,
            right.InstanceId);
    }
}
