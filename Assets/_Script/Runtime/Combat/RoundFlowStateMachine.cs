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
    CountExpired
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
/// 플레이어 턴, 몬스터 턴, 카운트 감소와 라운드 종료를 관리합니다.
/// </summary>
public sealed class RoundFlowStateMachine
{
    private readonly int startingCount;

    /// <summary>
    /// 라운드마다 사용할 초기 카운트로 상태 머신을 생성합니다.
    /// </summary>
    public RoundFlowStateMachine(int startingCount)
    {
        if (startingCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startingCount),
                "Starting count must be greater than zero.");
        }

        this.startingCount = startingCount;
    }

    public event Action<RoundFlowSnapshot> StateChanged;

    public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;
    public RoundResolution Resolution { get; private set; } = RoundResolution.None;
    public int RoundNumber { get; private set; }
    public int RemainingCount { get; private set; }
    public int AliveEnemyCount { get; private set; }

    public RoundFlowSnapshot Snapshot => new(
        Phase,
        Resolution,
        RoundNumber,
        RemainingCount,
        AliveEnemyCount);

    /// <summary>
    /// 지정한 수의 몬스터와 함께 첫 번째 라운드를 시작합니다.
    /// </summary>
    public void StartFirstRound(int enemyCount)
    {
        if (Phase != RoundPhase.NotStarted)
        {
            throw new InvalidOperationException("The run has already started.");
        }

        ValidateEnemyCount(enemyCount);
        RoundNumber = 1;
        StartRound(enemyCount);
    }

    /// <summary>
    /// 라운드 사이 상태에서 다음 라운드를 시작하고 라운드 번호를 증가시킵니다.
    /// </summary>
    public bool StartNextRound(int enemyCount)
    {
        if (Phase != RoundPhase.BetweenRounds)
        {
            return false;
        }

        ValidateEnemyCount(enemyCount);
        RoundNumber++;
        StartRound(enemyCount);
        return true;
    }

    /// <summary>
    /// 턴을 소비한 플레이어 행동을 완료하고 몬스터 턴으로 전환합니다.
    /// 턴을 소비하지 않는 행동에는 이 메서드를 호출하지 않습니다.
    /// </summary>
    public bool CompletePlayerTurn()
    {
        if (Phase != RoundPhase.PlayerTurn)
        {
            return false;
        }

        Phase = RoundPhase.EnemyTurn;
        PublishState();
        return true;
    }

    /// <summary>
    /// 몬스터 턴 처리를 끝내고 카운트를 1 감소시킨 뒤 다음 상태를 결정합니다.
    /// </summary>
    public bool CompleteEnemyTurn()
    {
        if (Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        RemainingCount = Math.Max(0, RemainingCount - 1);

        if (RemainingCount <= 0)
        {
            EndRun(RoundResolution.CountExpired);
            return true;
        }

        Phase = RoundPhase.PlayerTurn;
        PublishState();
        return true;
    }

    /// <summary>
    /// 환경 효과 등으로 남은 카운트를 즉시 감소시키고 0이 되면 게임을 패배로 종료합니다.
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

        if (Phase != RoundPhase.PlayerTurn && Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        AliveEnemyCount = Math.Max(0, AliveEnemyCount - amount);

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
        if (Phase != RoundPhase.PlayerTurn && Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        EndRun(RoundResolution.PlayerDefeated);
        return true;
    }

    /// <summary>
    /// 라운드 상태와 카운트, 생존 몬스터 수를 초기화합니다.
    /// </summary>
    private void StartRound(int enemyCount)
    {
        AliveEnemyCount = enemyCount;
        RemainingCount = startingCount;
        Resolution = RoundResolution.None;
        Phase = RoundPhase.PlayerTurn;
        PublishState();
    }

    /// <summary>
    /// 현재 라운드를 성공 상태로 마치고 라운드 사이 상태로 전환합니다.
    /// </summary>
    private void ClearRound()
    {
        AliveEnemyCount = 0;
        Resolution = RoundResolution.Cleared;
        Phase = RoundPhase.BetweenRounds;
        PublishState();
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
