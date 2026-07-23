using UnityEngine;

/// <summary>
/// 한 종류의 몬스터가 공유하는 기본 능력치와 행동 설정을 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "EnemyDefinition",
    menuName = "One Hand Game/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Enemy";
    [SerializeField, Min(0)] private int threat;

    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 2;
    [SerializeField, Min(0)] private int attackPower = 1;
    [SerializeField, Min(1)] private int actionRange = 1;
    [SerializeField, Min(0)] private int speed = 1;
    [SerializeField, Min(0)] private int baseGoldReward = 10;

    [Header("Spawn")]
    [SerializeField, Min(0)] private int minimumSpawnDistanceFromPlayer;
    [SerializeField, Min(0)] private int minimumSpawnDistanceFromEnemies;

    [Header("Action")]
    [SerializeField, Min(0)] private int actionCooldownTurns;
    [SerializeField] private bool hasCustomMovementPattern;

    [Header("Casting")]
    [SerializeField] private bool isCaster;
    [SerializeField, Min(1)] private int requiredCastingProgress = 1;

    public string DisplayName => displayName;
    public int Threat => Mathf.Max(0, threat);
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int AttackPower => Mathf.Max(0, attackPower);
    public int ActionRange => Mathf.Max(1, actionRange);
    public int Speed => Mathf.Max(0, speed);
    public int BaseGoldReward => Mathf.Max(0, baseGoldReward);
    public int MinimumSpawnDistanceFromPlayer =>
        Mathf.Max(0, minimumSpawnDistanceFromPlayer);
    public int MinimumSpawnDistanceFromEnemies =>
        Mathf.Max(0, minimumSpawnDistanceFromEnemies);
    public int ActionCooldownTurns => Mathf.Max(0, actionCooldownTurns);
    public bool HasCustomMovementPattern => hasCustomMovementPattern;
    public bool IsCaster => isCaster;
    public int RequiredCastingProgress => Mathf.Max(1, requiredCastingProgress);

    /// <summary>
    /// 생성된 몬스터 오브젝트의 고정 Instance ID로 런타임 상태를 생성합니다.
    /// </summary>
    public EnemyRuntimeState CreateRuntimeState(
        int instanceId,
        GoldRewardResult goldReward)
    {
        return new EnemyRuntimeState(this, instanceId, goldReward);
    }
}
