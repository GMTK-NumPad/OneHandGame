using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 그리드 Y 좌표에 따른 렌더링 순서 계산을 검사합니다.
/// </summary>
public sealed class GridYSortingTests
{
    /// <summary>
    /// 아래쪽 타일의 오브젝트가 위쪽 타일보다 높은 Sorting Order를 갖는지 검사합니다.
    /// </summary>
    [Test]
    public void LowerGridRow_IsRenderedInFront()
    {
        GameObject lowerObject = new("Lower");
        GameObject upperObject = new("Upper");

        try
        {
            GridYSorting lowerSorting =
                lowerObject.AddComponent<GridYSorting>();
            GridYSorting upperSorting =
                upperObject.AddComponent<GridYSorting>();

            lowerSorting.ApplyGridPosition(new GridPosition(3, 1));
            upperSorting.ApplyGridPosition(new GridPosition(3, 5));

            SortingGroup lowerGroup =
                lowerObject.GetComponent<SortingGroup>();
            SortingGroup upperGroup =
                upperObject.GetComponent<SortingGroup>();

            Assert.That(
                lowerGroup.sortingOrder,
                Is.GreaterThan(upperGroup.sortingOrder));
        }
        finally
        {
            Object.DestroyImmediate(lowerObject);
            Object.DestroyImmediate(upperObject);
        }
    }
}
