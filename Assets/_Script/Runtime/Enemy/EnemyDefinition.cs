using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 몬스터 기준으로 캐스팅을 시작할 수 있는 조건 범위의 모양을 나타냅니다.
/// </summary>
public enum EnemyCastingRangeShape
{
    Custom,
    Around,
    Cross,
    DiagonalCross,
    EightDirections,
    ForwardLine,
    ForwardRectangle,
    ForwardCone
}

/// <summary>
/// 고정된 플레이어 대상 타일을 중심으로 실제 피해를 주는 캐스팅 모양을 나타냅니다.
/// </summary>
public enum EnemyCastingImpactShape
{
    Custom,
    Around,
    Cross,
    DiagonalCross
}

/// <summary>
/// 캐스팅이 발동했을 때 효과를 적용할 대상을 찾는 방식을 나타냅니다.
/// </summary>
public enum EnemyCastingTargetType
{
    Area,
    DirectPlayer
}

/// <summary>
/// 캐스팅 대상에게 적용할 개별 효과의 종류를 나타냅니다.
/// </summary>
public enum EnemyCastingEffectType
{
    Damage,
    Stun
}

/// <summary>
/// 캐스팅 효과 한 개의 종류와 피해량 또는 기절 지속 턴을 보관합니다.
/// </summary>
[System.Serializable]
public sealed class EnemyCastingEffectData
{
    [SerializeField] private EnemyCastingEffectType effectType =
        EnemyCastingEffectType.Damage;
    [SerializeField, Min(0)] private int amount = 1;
    [SerializeField, Min(1)] private int durationTurns = 1;

    public EnemyCastingEffectType EffectType => effectType;
    public int Amount => Mathf.Max(0, amount);
    public int DurationTurns => Mathf.Max(1, durationTurns);
}

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

    [Header("Casting Trigger")]
    [SerializeField] private EnemyCastingRangeShape castingRangeShape =
        EnemyCastingRangeShape.Custom;
    [SerializeField, Min(1)] private int castingRange = 1;
    [SerializeField, Min(1)] private int castingRangeWidth = 1;
    [SerializeField] private List<Vector2Int> castingRangeOffsets = new();

    [Header("Casting Impact")]
    [FormerlySerializedAs("castingAttackType")]
    [SerializeField] private EnemyCastingTargetType castingTargetType =
        EnemyCastingTargetType.Area;
    [SerializeField] private EnemyCastingImpactShape castingImpactShape =
        EnemyCastingImpactShape.Custom;
    [SerializeField, Min(1)] private int castingImpactRange = 1;
    [SerializeField] private List<Vector2Int> castingImpactOffsets = new();
    [SerializeField] private List<EnemyCastingEffectData> castingEffects =
        new()
        {
            new EnemyCastingEffectData()
        };

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
    public EnemyCastingRangeShape CastingRangeShape =>
        castingRangeShape;
    public int CastingRange => Mathf.Max(1, castingRange);
    public int CastingRangeWidth =>
        Mathf.Max(1, castingRangeWidth);
    public IReadOnlyList<Vector2Int> CastingRangeOffsets =>
        castingRangeOffsets;
    public EnemyCastingTargetType CastingTargetType =>
        castingTargetType;
    public EnemyCastingImpactShape CastingImpactShape =>
        castingImpactShape;
    public int CastingImpactRange =>
        Mathf.Max(1, castingImpactRange);
    public IReadOnlyList<Vector2Int> CastingImpactOffsets =>
        castingImpactOffsets;
    public IReadOnlyList<EnemyCastingEffectData> CastingEffects =>
        castingEffects;

    /// <summary>
    /// Forward 범위의 너비가 몬스터 진행축을 기준으로 좌우가 대칭인 홀수가 되도록 보정합니다.
    /// </summary>
    private void OnValidate()
    {
        castingRange = Mathf.Max(1, castingRange);
        castingImpactRange = Mathf.Max(
            1,
            castingImpactRange);
        castingRangeWidth = Mathf.Max(
            1,
            castingRangeWidth);

        if (castingRangeWidth % 2 == 0)
        {
            castingRangeWidth++;
        }
    }

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
