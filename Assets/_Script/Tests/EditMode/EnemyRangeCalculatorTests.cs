using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 일반 공격 및 캐스팅 범위의 경계, 중복 제거와 실제 판정 일치를 검사합니다.
/// </summary>
public sealed class EnemyRangeCalculatorTests
{
    /// <summary>
    /// 중앙에서 일반 공격 범위 1이 자신의 칸을 제외한 주변 8칸인지 검사합니다.
    /// </summary>
    [Test]
    public void ActionRangeOne_AtCenterContainsEightNeighbors()
    {
        var board = new BoardState(
            width: 7,
            height: 7,
            defaultWalkable: true);
        GridPosition origin = new(3, 3);

        HashSet<GridPosition> positions =
            EnemyRangeCalculator.CreateActionRange(
                board,
                origin,
                actionRange: 1);

        Assert.That(positions.Count, Is.EqualTo(8));
        Assert.That(positions.Contains(origin), Is.False);
        Assert.That(
            positions,
            Does.Contain(new GridPosition(4, 4)));
    }

    /// <summary>
    /// 보드 모서리 밖의 일반 공격 범위가 결과에 포함되지 않는지 검사합니다.
    /// </summary>
    [Test]
    public void ActionRangeOne_AtCornerStaysInsideBoard()
    {
        var board = new BoardState(
            width: 7,
            height: 7,
            defaultWalkable: true);

        HashSet<GridPosition> positions =
            EnemyRangeCalculator.CreateActionRange(
                board,
                new GridPosition(0, 0),
                actionRange: 1);

        Assert.That(positions.Count, Is.EqualTo(3));
        Assert.That(
            positions,
            Does.Contain(new GridPosition(1, 1)));
    }

    /// <summary>
    /// 캐스팅 상대 좌표가 실제 보드 좌표로 변환되고 중복과 자신의 칸은 제외되는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingOffsets_ConvertToUniqueBoardPositions()
    {
        var board = new BoardState(
            width: 7,
            height: 7,
            defaultWalkable: true);
        var offsets = new List<Vector2Int>
        {
            new(0, 0),
            new(0, 1),
            new(0, 1),
            new(2, 0)
        };

        HashSet<GridPosition> positions =
            EnemyRangeCalculator.CreateCastingTriggerRange(
                board,
                new GridPosition(3, 3),
                EnemyCastingRangeShape.Custom,
                range: 1,
                width: 1,
                customOffsets: offsets,
                facingDirection: new GridPosition(1, 0));

        Assert.That(positions.Count, Is.EqualTo(2));
        Assert.That(
            positions,
            Does.Contain(new GridPosition(3, 4)));
        Assert.That(
            positions,
            Does.Contain(new GridPosition(5, 3)));
    }

    /// <summary>
    /// 화면 표시와 실제 일반 공격 판정이 같은 대각선 포함 거리 규칙을 사용하는지 검사합니다.
    /// </summary>
    [Test]
    public void ActionRangeCheck_UsesChebyshevDistance()
    {
        GridPosition origin = new(3, 3);

        Assert.That(
            EnemyRangeCalculator.IsInActionRange(
                origin,
                new GridPosition(4, 4),
                actionRange: 1),
            Is.True);
        Assert.That(
            EnemyRangeCalculator.IsInActionRange(
                origin,
                new GridPosition(5, 3),
                actionRange: 1),
            Is.False);
    }

    /// <summary>
    /// Around 거리 2가 자신의 칸을 제외한 5×5의 24칸을 생성하는지 검사합니다.
    /// </summary>
    [Test]
    public void AroundRangeTwo_ContainsTwentyFourTiles()
    {
        var board = new BoardState(
            width: 7,
            height: 7,
            defaultWalkable: true);

        HashSet<GridPosition> positions =
            EnemyRangeCalculator.CreateCastingTriggerRange(
                board,
                new GridPosition(3, 3),
                EnemyCastingRangeShape.Around,
                range: 2,
                width: 1,
                customOffsets: null,
                facingDirection: new GridPosition(1, 0));

        Assert.That(positions.Count, Is.EqualTo(24));
    }

    /// <summary>
    /// Cross는 +자, DiagonalCross는 X자 좌표만 생성하는지 검사합니다.
    /// </summary>
    [Test]
    public void CrossShapes_GenerateSeparatePlusAndDiagonalTiles()
    {
        var board = new BoardState(
            width: 7,
            height: 7,
            defaultWalkable: true);
        GridPosition origin = new(3, 3);

        HashSet<GridPosition> cross =
            EnemyRangeCalculator.CreateCastingTriggerRange(
                board,
                origin,
                EnemyCastingRangeShape.Cross,
                range: 2,
                width: 1,
                customOffsets: null,
                facingDirection: new GridPosition(1, 0));
        HashSet<GridPosition> diagonalCross =
            EnemyRangeCalculator.CreateCastingTriggerRange(
                board,
                origin,
                EnemyCastingRangeShape.DiagonalCross,
                range: 2,
                width: 1,
                customOffsets: null,
                facingDirection: new GridPosition(1, 0));

        Assert.That(cross.Count, Is.EqualTo(8));
        Assert.That(diagonalCross.Count, Is.EqualTo(8));
        Assert.That(
            cross.Contains(new GridPosition(5, 3)),
            Is.True);
        Assert.That(
            cross.Contains(new GridPosition(5, 5)),
            Is.False);
        Assert.That(
            diagonalCross.Contains(
                new GridPosition(5, 5)),
            Is.True);
        Assert.That(
            diagonalCross.Contains(
                new GridPosition(5, 3)),
            Is.False);
    }

    /// <summary>
    /// 실제 십자 캐스팅 피해 범위가 고정된 플레이어 대상 타일을 중심으로 생성되는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingImpactCross_IsCenteredOnFixedPlayerTarget()
    {
        var board = new BoardState(
            width: 7,
            height: 7,
            defaultWalkable: true);
        GridPosition fixedPlayerTarget = new(3, 3);

        HashSet<GridPosition> positions =
            EnemyRangeCalculator.CreateCastingImpactRange(
                board,
                fixedPlayerTarget,
                EnemyCastingImpactShape.Cross,
                range: 1,
                customOffsets: null);

        Assert.That(positions.Count, Is.EqualTo(5));
        Assert.That(
            positions.Contains(fixedPlayerTarget),
            Is.True);
        Assert.That(
            positions.Contains(new GridPosition(4, 3)),
            Is.True);
        Assert.That(
            positions.Contains(new GridPosition(4, 4)),
            Is.False);
    }

    /// <summary>
    /// Custom 피해 범위가 자동으로 중심을 추가하지 않고 입력한 Offset만 사용하는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingImpactCustom_UsesOnlyConfiguredOffsets()
    {
        GridPosition fixedPlayerTarget = new(3, 3);
        var offsets = new List<Vector2Int>
        {
            new(1, 0),
            new(0, 1)
        };

        Assert.That(
            EnemyRangeCalculator.IsInCastingImpactRange(
                fixedPlayerTarget,
                fixedPlayerTarget,
                EnemyCastingImpactShape.Custom,
                range: 1,
                customOffsets: offsets),
            Is.False);
        Assert.That(
            EnemyRangeCalculator.IsInCastingImpactRange(
                fixedPlayerTarget,
                new GridPosition(4, 3),
                EnemyCastingImpactShape.Custom,
                range: 1,
                customOffsets: offsets),
            Is.True);

        offsets.Add(new Vector2Int(0, 0));

        Assert.That(
            EnemyRangeCalculator.IsInCastingImpactRange(
                fixedPlayerTarget,
                fixedPlayerTarget,
                EnemyCastingImpactShape.Custom,
                range: 1,
                customOffsets: offsets),
            Is.True);
    }

    /// <summary>
    /// 캐스팅 좌표 판정이 일반 인식 범위와 독립적으로 계산되는지 검사합니다.
    /// </summary>
    [Test]
    public void CastingCondition_UsesCastingRangeIndependently()
    {
        GridPosition origin = new(3, 3);
        GridPosition target = new(5, 5);

        bool castingRangeMatched =
            EnemyRangeCalculator.IsInCastingTriggerRange(
                origin,
                target,
                EnemyCastingRangeShape.ForwardLine,
                range: 2,
                width: 1,
                customOffsets: null,
                facingDirection: new GridPosition(1, 1));

        Assert.That(castingRangeMatched, Is.True);
        Assert.That(
            EnemyRangeCalculator.IsInActionRange(
                origin,
                target,
                actionRange: 1),
            Is.False);
    }
}
