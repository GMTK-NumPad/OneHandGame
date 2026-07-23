using UnityEngine;

/// <summary>
/// 씬에 생성된 몬스터 오브젝트와 보드 위치, 개별 런타임 상태를 연결합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoardActor))]
public sealed class EnemyActor : MonoBehaviour
{
    private BoardActor boardActor;

    public BoardActor BoardActor => GetBoardActor();
    public EnemyDefinition Definition { get; private set; }
    public EnemyRuntimeState RuntimeState { get; private set; }
    public bool IsInitialized => RuntimeState != null;

    /// <summary>
    /// 같은 GameObject에 있는 보드 액터 참조를 미리 가져옵니다.
    /// </summary>
    private void Awake()
    {
        boardActor = GetComponent<BoardActor>();
    }

    /// <summary>
    /// 몬스터 정의와 스폰 시 결정된 골드 보상으로 개별 런타임 상태를 생성합니다.
    /// </summary>
    public bool Initialize(
        EnemyDefinition definition,
        GoldRewardResult goldReward)
    {
        if (definition == null)
        {
            Debug.LogError(
                "EnemyActor requires an EnemyDefinition.",
                this);
            return false;
        }

        if (IsInitialized)
        {
            Debug.LogWarning(
                "EnemyActor is already initialized.",
                this);
            return false;
        }

        Definition = definition;
        RuntimeState = definition.CreateRuntimeState(
            gameObject.GetInstanceID(),
            goldReward);
        return true;
    }

    /// <summary>
    /// 캐시가 비어 있다면 같은 GameObject에서 BoardActor를 다시 찾아 반환합니다.
    /// </summary>
    private BoardActor GetBoardActor()
    {
        if (boardActor == null)
        {
            boardActor = GetComponent<BoardActor>();
        }

        return boardActor;
    }
}
