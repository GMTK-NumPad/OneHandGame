using System;

/// <summary>
/// 현재 라운드가 어느 행동 단계에 있는지 나타냅니다.
/// </summary>
public enum RoundPhase
{
    NotStarted,
    PlayerTurn,
    EnemyTurn,
    BetweenRounds,
    RunEnded
}

/// <summary>
/// 라운드 또는 게임이 종료된 원인을 나타냅니다.
/// </summary>
public enum RoundResolution
{
    None,
    Cleared,
    PlayerDefeated,
    CountExpired,
    GameCleared
}

/// <summary>
/// 라운드 클리어 시 획득한 카운트다운과 초과분 골드 정산 결과를 보관합니다.
/// </summary>
public readonly struct RoundClearReward
{
    /// <summary>
    /// 클리어 보너스의 구성과 실제 카운트다운 및 골드 반영량을 생성합니다.
    /// </summary>
    public RoundClearReward(
        int baseCountdown,
        int noDamageCountdown,
        int noConsumableCountdown,
        int addedCountdown,
        int overflowCountdown,
        int overflowGold)
    {
        BaseCountdown = baseCountdown;
        NoDamageCountdown = noDamageCountdown;
        NoConsumableCountdown = noConsumableCountdown;
        AddedCountdown = addedCountdown;
        OverflowCountdown = overflowCountdown;
        OverflowGold = overflowGold;
    }

    public int BaseCountdown { get; }
    public int NoDamageCountdown { get; }
    public int NoConsumableCountdown { get; }
    public int TotalCountdownReward =>
        BaseCountdown
        + NoDamageCountdown
        + NoConsumableCountdown;
    public int AddedCountdown { get; }
    public int OverflowCountdown { get; }
    public int OverflowGold { get; }
}

/// <summary>
/// UI와 외부 시스템에 전달할 현재 라운드 상태를 보관합니다.
/// </summary>
public readonly struct RoundFlowSnapshot
{
    /// <summary>
    /// 현재 라운드 상태를 하나의 읽기 전용 값으로 생성합니다.
    /// </summary>
    public RoundFlowSnapshot(
        RoundPhase phase,
        RoundResolution resolution,
        int roundNumber,
        int remainingCount,
        int aliveEnemyCount)
    {
        Phase = phase;
        Resolution = resolution;
        RoundNumber = roundNumber;
        RemainingCount = remainingCount;
        AliveEnemyCount = aliveEnemyCount;
    }

    public RoundPhase Phase { get; }
    public RoundResolution Resolution { get; }
    public int RoundNumber { get; }
    public int RemainingCount { get; }
    public int AliveEnemyCount { get; }
}

/// <summary>
/// 플레이어 턴, 몬스터 턴, 카운트다운 정산과 라운드 종료를 관리합니다.
/// </summary>
public sealed class RoundFlowStateMachine
{
    private readonly int startingCount;
    private readonly int maximumCount;
    private readonly int clearCountdownReward;
    private readonly int noDamageCountdownReward;
    private readonly int noConsumableCountdownReward;
    private readonly int overflowGoldPerCountdown;
    private bool playerActionRecorded;
    private bool completesRunOnClear;

    /// <summary>
    /// 기본 규칙인 최대 카운트 10과 클리어 보너스 3, 2, 1로 상태 머신을 생성합니다.
    /// </summary>
    public RoundFlowStateMachine(int startingCount)
        : this(
            startingCount,
            maximumCount: Math.Max(10, startingCount),
            clearCountdownReward: 3,
            noDamageCountdownReward: 2,
            noConsumableCountdownReward: 1,
            overflowGoldPerCountdown: 10)
    {
    }

    /// <summary>
    /// 시작 및 최대 카운트다운과 라운드 클리어 보상 규칙으로 상태 머신을 생성합니다.
    /// </summary>
    public RoundFlowStateMachine(
        int startingCount,
        int maximumCount,
        int clearCountdownReward,
        int noDamageCountdownReward,
        int noConsumableCountdownReward,
        int overflowGoldPerCountdown)
    {
        if (startingCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startingCount),
                "Starting count must be greater than zero.");
        }

        if (maximumCount < startingCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCount),
                "Maximum count must be at least the starting count.");
        }

        this.startingCount = startingCount;
        this.maximumCount = maximumCount;
        this.clearCountdownReward =
            Math.Max(0, clearCountdownReward);
        this.noDamageCountdownReward =
            Math.Max(0, noDamageCountdownReward);
        this.noConsumableCountdownReward =
            Math.Max(0, noConsumableCountdownReward);
        this.overflowGoldPerCountdown =
            Math.Max(0, overflowGoldPerCountdown);
    }

    public event Action<RoundFlowSnapshot> StateChanged;
    public event Action<RoundClearReward> RoundCleared;

    public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;
    public RoundResolution Resolution { get; private set; } = RoundResolution.None;
    public int RoundNumber { get; private set; }
    public int RemainingCount { get; private set; }
    public int AliveEnemyCount { get; private set; }
    public int DamageTakenThisRound { get; private set; }
    public bool ConsumableUsedThisRound { get; private set; }
    public int TotalTurnsPlayed { get; private set; }
    public int TotalBonusCountdownEarned { get; private set; }
    public int TotalDamageTaken { get; private set; }
    public RoundClearReward LastClearReward { get; private set; }

    public RoundFlowSnapshot Snapshot => new(
        Phase,
        Resolution,
        RoundNumber,
        RemainingCount,
        AliveEnemyCount);

    /// <summary>
    /// 지정한 수의 몬스터와 함께 첫 스테이지의 첫 라운드를 시작합니다.
    /// </summary>
    public void StartFirstRound(
        int enemyCount,
        bool completesRunWhenCleared = false)
    {
        if (Phase != RoundPhase.NotStarted)
        {
            throw new InvalidOperationException(
                "The run has already started.");
        }

        ValidateEnemyCount(enemyCount);
        RoundNumber = 1;
        RemainingCount = startingCount;
        StartRound(
            enemyCount,
            completesRunWhenCleared);
    }

    /// <summary>
    /// 라운드 사이 상태에서 다음 라운드를 시작하며 스테이지 첫 라운드일 때만 카운트다운을 10으로 재설정합니다.
    /// </summary>
    public bool StartNextRound(
        int enemyCount,
        bool isFirstRoundOfStage = false,
        bool completesRunWhenCleared = false)
    {
        if (Phase != RoundPhase.BetweenRounds)
        {
            return false;
        }

        ValidateEnemyCount(enemyCount);
        RoundNumber++;

        if (isFirstRoundOfStage)
        {
            RemainingCount = startingCount;
        }

        StartRound(
            enemyCount,
            completesRunWhenCleared);
        return true;
    }

    /// <summary>
    /// 유효한 플레이어 행동 한 번을 결과 통계에 중복 없이 기록합니다.
    /// </summary>
    public bool RecordPlayerAction()
    {
        if (Phase != RoundPhase.PlayerTurn
            || playerActionRecorded)
        {
            return false;
        }

        TotalTurnsPlayed =
            SaturatingAdd(TotalTurnsPlayed, 1);
        playerActionRecorded = true;
        return true;
    }

    /// <summary>
    /// 턴을 소비한 플레이어 행동을 기록하고 몬스터 턴으로 전환합니다.
    /// 턴을 소비하지 않는 행동에는 이 메서드를 호출하지 않습니다.
    /// </summary>
    public bool CompletePlayerTurn()
    {
        if (Phase != RoundPhase.PlayerTurn)
        {
            return false;
        }

        RecordPlayerAction();
        Phase = RoundPhase.EnemyTurn;
        PublishState();
        return true;
    }

    /// <summary>
    /// 몬스터 턴 처리를 끝내고 카운트다운을 1 감소시킨 뒤 다음 상태를 결정합니다.
    /// </summary>
    public bool CompleteEnemyTurn()
    {
        if (Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        RemainingCount = Math.Max(
            0,
            RemainingCount - 1);

        if (RemainingCount <= 0)
        {
            EndRun(RoundResolution.CountExpired);
            return true;
        }

        playerActionRecorded = false;
        Phase = RoundPhase.PlayerTurn;
        PublishState();
        return true;
    }

    /// <summary>
    /// 환경 효과 등으로 남은 카운트다운을 즉시 감소시키고 0이 되면 게임을 패배로 종료합니다.
    /// </summary>
    public bool ReduceRemainingCount(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Count reduction amount must be greater than zero.");
        }

        if (Phase != RoundPhase.PlayerTurn
            && Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        RemainingCount = Math.Max(
            0,
            RemainingCount - amount);

        if (RemainingCount <= 0)
        {
            EndRun(RoundResolution.CountExpired);
        }
        else
        {
            PublishState();
        }

        return true;
    }

    /// <summary>
    /// 방어 이후 실제로 감소한 플레이어 체력을 현재 라운드와 전체 결과 통계에 기록합니다.
    /// </summary>
    public bool ReportPlayerDamageTaken(int amount)
    {
        if (amount <= 0
            || (Phase != RoundPhase.PlayerTurn
                && Phase != RoundPhase.EnemyTurn))
        {
            return false;
        }

        DamageTakenThisRound =
            SaturatingAdd(
                DamageTakenThisRound,
                amount);
        TotalDamageTaken =
            SaturatingAdd(
                TotalDamageTaken,
                amount);
        return true;
    }

    /// <summary>
    /// 이번 라운드에서 소모품 사용이 실제로 성공했음을 기록합니다.
    /// </summary>
    public bool ReportConsumableUsed()
    {
        if (Phase != RoundPhase.PlayerTurn
            && Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        ConsumableUsedThisRound = true;
        return true;
    }

    /// <summary>
    /// 처치된 몬스터 수를 반영하고 남은 몬스터가 없으면 라운드를 완료합니다.
    /// </summary>
    public bool ReportEnemyDefeated(int amount = 1)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Defeated enemy amount must be greater than zero.");
        }

        if (Phase != RoundPhase.PlayerTurn
            && Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        AliveEnemyCount = Math.Max(
            0,
            AliveEnemyCount - amount);

        if (AliveEnemyCount == 0)
        {
            ClearRound();
        }
        else
        {
            PublishState();
        }

        return true;
    }

    /// <summary>
    /// 플레이어 패배를 기록하고 진행 중인 게임을 종료합니다.
    /// </summary>
    public bool ReportPlayerDefeated()
    {
        if (Phase != RoundPhase.PlayerTurn
            && Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        EndRun(RoundResolution.PlayerDefeated);
        return true;
    }

    /// <summary>
    /// 현재 카운트다운은 유지하면서 새 라운드의 몬스터와 보너스 판정 기록을 초기화합니다.
    /// </summary>
    private void StartRound(
        int enemyCount,
        bool completesRunWhenCleared)
    {
        completesRunOnClear =
            completesRunWhenCleared;
        AliveEnemyCount = enemyCount;
        DamageTakenThisRound = 0;
        ConsumableUsedThisRound = false;
        playerActionRecorded = false;
        LastClearReward = default;
        Resolution = RoundResolution.None;
        Phase = RoundPhase.PlayerTurn;
        PublishState();
    }

    /// <summary>
    /// 클리어 보너스를 더하고 최대치를 넘긴 카운트다운을 골드로 환산한 뒤 라운드 사이 상태로 전환합니다.
    /// </summary>
    private void ClearRound()
    {
        AliveEnemyCount = 0;

        if (completesRunOnClear)
        {
            LastClearReward = default;
            EndRun(RoundResolution.GameCleared);
            return;
        }

        int noDamageReward =
            DamageTakenThisRound == 0
                ? noDamageCountdownReward
                : 0;
        int noConsumableReward =
            !ConsumableUsedThisRound
                ? noConsumableCountdownReward
                : 0;
        int totalReward =
            clearCountdownReward
            + noDamageReward
            + noConsumableReward;
        long uncappedCount =
            (long)RemainingCount
            + totalReward;
        int addedCountdown = Math.Max(
            0,
            Math.Min(
                totalReward,
                maximumCount - RemainingCount));
        int overflowCountdown = (int)Math.Min(
            int.MaxValue,
            Math.Max(
                0L,
                uncappedCount - maximumCount));
        int overflowGold = SaturatingMultiply(
            overflowCountdown,
            overflowGoldPerCountdown);

        RemainingCount = Math.Min(
            maximumCount,
            RemainingCount + addedCountdown);
        TotalBonusCountdownEarned =
            SaturatingAdd(
                TotalBonusCountdownEarned,
                totalReward);
        LastClearReward = new RoundClearReward(
            clearCountdownReward,
            noDamageReward,
            noConsumableReward,
            addedCountdown,
            overflowCountdown,
            overflowGold);

        Resolution = RoundResolution.Cleared;
        Phase = RoundPhase.BetweenRounds;
        PublishState();
        RoundCleared?.Invoke(LastClearReward);
    }

    /// <summary>
    /// 지정한 패배 원인을 기록하고 게임 종료 상태로 전환합니다.
    /// </summary>
    private void EndRun(RoundResolution resolution)
    {
        Resolution = resolution;
        Phase = RoundPhase.RunEnded;
        PublishState();
    }

    /// <summary>
    /// 현재 상태의 스냅샷을 구독 중인 외부 시스템에 알립니다.
    /// </summary>
    private void PublishState()
    {
        StateChanged?.Invoke(Snapshot);
    }

    /// <summary>
    /// 현재 값과 추가 값을 정수 최댓값을 넘지 않도록 더합니다.
    /// </summary>
    private static int SaturatingAdd(
        int current,
        int amount)
    {
        return (int)Math.Min(
            int.MaxValue,
            (long)Math.Max(0, current)
            + Math.Max(0, amount));
    }

    /// <summary>
    /// 두 양수 정수의 곱을 정수 최댓값을 넘지 않도록 계산합니다.
    /// </summary>
    private static int SaturatingMultiply(
        int left,
        int right)
    {
        return (int)Math.Min(
            int.MaxValue,
            (long)Math.Max(0, left)
            * Math.Max(0, right));
    }

    /// <summary>
    /// 라운드 시작에 사용할 몬스터 수가 유효한지 검사합니다.
    /// </summary>
    private static void ValidateEnemyCount(int enemyCount)
    {
        if (enemyCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enemyCount),
                "A round must start with at least one enemy.");
        }
    }
}
