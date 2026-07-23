using UnityEngine;

/// <summary>
/// 플레이어와 몬스터가 보드 위에서 공유하는 위치 및 이동 기능을 제공합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GridYSorting))]
public sealed class BoardActor : MonoBehaviour
{
    private GridYSorting gridYSorting;

    public BoardManager BoardManager { get; private set; }
    public GridPosition Position { get; private set; }
    public bool IsPlaced => BoardManager != null;

    /// <summary>
    /// 보드 배치 시 사용할 Y 정렬 컴포넌트를 미리 가져옵니다.
    /// </summary>
    private void Awake()
    {
        gridYSorting = GetGridYSorting();
    }

    /// <summary>
    /// 현재 보드에서 지정한 타일로 이동을 요청합니다.
    /// </summary>
    public bool TryMove(GridPosition targetPosition)
    {
        return BoardManager != null
            && BoardManager.TryMoveActor(this, targetPosition);
    }

    /// <summary>
    /// 현재 액터를 보드의 점유 정보에서 제거합니다.
    /// </summary>
    public void RemoveFromBoard()
    {
        BoardManager?.RemoveActor(this);
    }

    /// <summary>
    /// BoardManager가 액터의 보드와 좌표 정보를 갱신할 때 사용합니다.
    /// </summary>
    internal void ApplyPlacement(BoardManager boardManager, GridPosition position)
    {
        BoardManager = boardManager;
        Position = position;
        transform.position = boardManager.BoardToWorld(position);
        GetGridYSorting().ApplyGridPosition(position);
    }

    /// <summary>
    /// BoardManager가 재구성되거나 액터가 제거될 때 보드 참조를 해제합니다.
    /// </summary>
    internal void ClearPlacement(BoardManager boardManager)
    {
        if (BoardManager == boardManager)
        {
            BoardManager = null;
        }
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 남아 있는 보드 점유 정보를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        RemoveFromBoard();
    }

    /// <summary>
    /// 기존 참조가 없다면 GridYSorting을 찾아보고, 없는 기존 프리팹에는 자동으로 추가합니다.
    /// </summary>
    private GridYSorting GetGridYSorting()
    {
        if (gridYSorting == null)
        {
            gridYSorting = GetComponent<GridYSorting>();

            if (gridYSorting == null)
            {
                gridYSorting = gameObject.AddComponent<GridYSorting>();
            }
        }

        return gridYSorting;
    }
}
