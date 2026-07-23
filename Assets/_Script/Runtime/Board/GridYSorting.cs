using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 같은 그리드 위의 액터와 필드 아이템을 Y 좌표에 따라 앞뒤로 정렬합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SortingGroup))]
public sealed class GridYSorting : MonoBehaviour
{
    [SerializeField] private int baseSortingOrder = 100;
    [SerializeField, Min(1)] private int orderPerRow = 10;
    [SerializeField] private int localOrderOffset;

    private SortingGroup sortingGroup;

    public int SortingOrder =>
        GetSortingGroup().sortingOrder;

    /// <summary>
    /// 정렬에 사용할 SortingGroup을 가져옵니다.
    /// </summary>
    private void Awake()
    {
        sortingGroup = GetSortingGroup();
    }

    /// <summary>
    /// 아래쪽 행이 위쪽 행보다 앞에 그려지도록 현재 그리드 Y 좌표를 적용합니다.
    /// </summary>
    public void ApplyGridPosition(GridPosition position)
    {
        GetSortingGroup().sortingOrder =
            baseSortingOrder
            - position.Y * Mathf.Max(1, orderPerRow)
            + localOrderOffset;
    }

    /// <summary>
    /// 기존 참조가 없다면 같은 GameObject의 SortingGroup을 찾아 반환합니다.
    /// </summary>
    private SortingGroup GetSortingGroup()
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
        }

        return sortingGroup;
    }
}
