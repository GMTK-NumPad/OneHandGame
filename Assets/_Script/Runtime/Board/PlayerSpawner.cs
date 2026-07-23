using System;
using UnityEngine;

/// <summary>
/// 플레이어 프리팹을 최초 생성하고 각 라운드 시작 시 보드 중앙에 재배치합니다.
/// </summary>
public sealed class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager = null;
    [SerializeField] private BoardActor playerPrefab = null;
    [SerializeField] private Transform actorRoot = null;

    private BoardActor spawnedPlayer;

    public event Action<BoardActor> PlayerSpawned;
    public BoardActor SpawnedPlayer => spawnedPlayer;

    /// <summary>
    /// 플레이어가 없다면 생성하고 현재 보드의 중앙 타일에 배치합니다.
    /// </summary>
    public BoardActor SpawnOrResetPlayer()
    {
        if (boardManager == null)
        {
            Debug.LogError("PlayerSpawner requires a BoardManager.", this);
            return null;
        }

        bool createdPlayer = spawnedPlayer == null;

        if (createdPlayer)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("PlayerSpawner requires a Player prefab.", this);
                return null;
            }

            spawnedPlayer = Instantiate(playerPrefab, actorRoot);
        }

        if (!boardManager.TryPlaceActor(spawnedPlayer, boardManager.CenterPosition))
        {
            Debug.LogError(
                $"Could not place the player at {boardManager.CenterPosition}.",
                this);

            if (createdPlayer)
            {
                Destroy(spawnedPlayer.gameObject);
                spawnedPlayer = null;
            }

            return null;
        }

        if (createdPlayer)
        {
            PlayerSpawned?.Invoke(spawnedPlayer);
        }

        return spawnedPlayer;
    }
}
