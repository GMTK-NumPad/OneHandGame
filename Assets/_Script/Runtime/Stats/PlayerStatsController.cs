using UnityEngine;

/// <summary>
/// 플레이어 능력치 SO로 런타임 능력치를 생성하고 씬 컴포넌트에 제공합니다.
/// </summary>
public sealed class PlayerStatsController : MonoBehaviour
{
    [SerializeField] private PlayerStatsDefinition definition = null;

    public PlayerStatsDefinition Definition => definition;
    public PlayerRuntimeStats RuntimeStats { get; private set; }

    /// <summary>
    /// 씬이 시작될 때 지정된 SO를 사용해 런타임 능력치를 생성합니다.
    /// </summary>
    private void Awake()
    {
        Initialize(definition);
    }

    /// <summary>
    /// 지정한 플레이어 능력치 SO로 런타임 상태를 새로 생성합니다.
    /// </summary>
    public void Initialize(PlayerStatsDefinition statsDefinition)
    {
        if (statsDefinition == null)
        {
            Debug.LogError(
                "PlayerStatsController requires a PlayerStatsDefinition.",
                this);
            RuntimeStats = null;
            return;
        }

        definition = statsDefinition;
        RuntimeStats = definition.CreateRuntimeStats();
    }
}
