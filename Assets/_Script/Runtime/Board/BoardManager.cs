using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 씬의 Tilemap과 논리 보드 좌표를 연결하고 액터 점유 상태를 관리합니다.
/// </summary>
public sealed class BoardManager : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private Vector2Int boardSize = new(7, 7);
    [SerializeField] private Vector3Int originCell = Vector3Int.zero;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap walkableTilemap = null;
    [SerializeField] private Tilemap obstacleTilemap = null;

    private readonly Dictionary<int, BoardActor> actors = new();
    private BoardState boardState;

    public Vector2Int BoardSize => boardSize;
    public GridPosition CenterPosition => EnsureBoardState().Center;

    /// <summary>
    /// 씬이 시작될 때 Tilemap을 기준으로 논리 보드를 구성합니다.
    /// </summary>
    private void Awake()
    {
        RebuildBoard();
    }

    /// <summary>
    /// Inspector에서 보드 크기가 1보다 작아지지 않도록 제한합니다.
    /// </summary>
    private void OnValidate()
    {
        boardSize.x = Mathf.Max(1, boardSize.x);
        boardSize.y = Mathf.Max(1, boardSize.y);
    }

    /// <summary>
    /// 현재 Tilemap을 읽어 이동 가능 타일과 장애물 정보를 다시 구성합니다.
    /// </summary>
    [ContextMenu("Rebuild Board")]
    public void RebuildBoard()
    {
        foreach (BoardActor actor in actors.Values)
        {
            if (actor != null)
            {
                actor.ClearPlacement(this);
            }
        }

        actors.Clear();
        boardState = new BoardState(boardSize.x, boardSize.y);

        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                GridPosition position = new(x, y);
                Vector3Int cell = BoardToCell(position);
                bool hasGround = walkableTilemap == null || walkableTilemap.HasTile(cell);
                bool hasObstacle = obstacleTilemap != null && obstacleTilemap.HasTile(cell);
                boardState.SetWalkable(position, hasGround && !hasObstacle);
            }
        }
    }

    /// <summary>
    /// 보드 좌표가 이동 가능하고 다른 액터가 점유하지 않았는지 확인합니다.
    /// </summary>
    public bool CanEnter(GridPosition position)
    {
        return EnsureBoardState().CanEnter(position);
    }

    /// <summary>
    /// 액터를 지정한 보드 좌표에 등록하고 월드 위치를 갱신합니다.
    /// </summary>
    public bool TryPlaceActor(BoardActor actor, GridPosition position)
    {
        if (actor == null || !CanEnterForActor(actor, position))
        {
            return false;
        }

        if (actor.BoardManager != null && actor.BoardManager != this)
        {
            actor.BoardManager.RemoveActor(actor);
        }

        int actorId = actor.GetInstanceID();
        if (!EnsureBoardState().TryPlaceActor(actorId, position))
        {
            return false;
        }

        actors[actorId] = actor;
        actor.ApplyPlacement(this, position);
        return true;
    }

    /// <summary>
    /// 등록된 액터를 지정한 보드 좌표로 이동하고 월드 위치를 갱신합니다.
    /// </summary>
    public bool TryMoveActor(BoardActor actor, GridPosition targetPosition)
    {
        if (actor == null || actor.BoardManager != this)
        {
            return false;
        }

        if (!EnsureBoardState().TryMoveActor(actor.GetInstanceID(), targetPosition))
        {
            return false;
        }

        actor.ApplyPlacement(this, targetPosition);
        return true;
    }

    /// <summary>
    /// 액터를 보드 점유 정보에서 제거합니다.
    /// </summary>
    public bool RemoveActor(BoardActor actor)
    {
        if (actor == null || actor.BoardManager != this)
        {
            return false;
        }

        int actorId = actor.GetInstanceID();
        bool removed = EnsureBoardState().RemoveActor(actorId);
        actors.Remove(actorId);
        actor.ClearPlacement(this);
        return removed;
    }

    /// <summary>
    /// 지정한 보드 좌표에 있는 액터를 가져옵니다.
    /// </summary>
    public bool TryGetActor(GridPosition position, out BoardActor actor)
    {
        actor = null;

        if (!EnsureBoardState().TryGetOccupant(position, out int actorId))
        {
            return false;
        }

        return actors.TryGetValue(actorId, out actor) && actor != null;
    }

    /// <summary>
    /// 보드 좌표를 Tilemap 셀 좌표로 변환합니다.
    /// </summary>
    public Vector3Int BoardToCell(GridPosition position)
    {
        return originCell + new Vector3Int(position.X, position.Y, 0);
    }

    /// <summary>
    /// 보드 좌표를 액터가 배치될 월드 중심 위치로 변환합니다.
    /// </summary>
    public Vector3 BoardToWorld(GridPosition position)
    {
        Vector3Int cell = BoardToCell(position);

        if (walkableTilemap != null)
        {
            return walkableTilemap.GetCellCenterWorld(cell);
        }

        return transform.TransformPoint(
            new Vector3(cell.x + 0.5f, cell.y + 0.5f, cell.z));
    }

    /// <summary>
    /// 월드 위치를 유효한 보드 좌표로 변환합니다.
    /// </summary>
    public bool TryWorldToBoard(Vector3 worldPosition, out GridPosition position)
    {
        Vector3Int cell;

        if (walkableTilemap != null)
        {
            cell = walkableTilemap.WorldToCell(worldPosition);
        }
        else
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            cell = Vector3Int.FloorToInt(localPosition);
        }

        position = new GridPosition(
            cell.x - originCell.x,
            cell.y - originCell.y);

        return EnsureBoardState().IsInside(position);
    }

    /// <summary>
    /// 아직 생성되지 않은 논리 보드를 필요할 때 즉시 구성해 반환합니다.
    /// </summary>
    private BoardState EnsureBoardState()
    {
        if (boardState == null)
        {
            RebuildBoard();
        }

        return boardState;
    }

    /// <summary>
    /// 액터 자신의 현재 타일을 포함해 지정한 좌표에 배치 가능한지 확인합니다.
    /// </summary>
    private bool CanEnterForActor(BoardActor actor, GridPosition position)
    {
        if (actor.BoardManager == this && actor.Position == position)
        {
            return true;
        }

        return CanEnter(position);
    }
}
