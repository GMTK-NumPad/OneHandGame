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
    private bool decreaseCountAfterEnemyTurn;

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
    /// 플레이어 행동을 소모하고 설정에 따라 몬스터 행동과 카운트 감소를 진행합니다.
    /// </summary>
    public bool CommitPlayerAction(bool executesEnemyTurn, bool decreasesCount)
    {
        if (Phase != RoundPhase.PlayerTurn)
        {
            return false;
        }

        if (!executesEnemyTurn)
        {
            CompletePlayerAction(decreasesCount);
            return true;
        }

        decreaseCountAfterEnemyTurn = decreasesCount;
        Phase = RoundPhase.EnemyTurn;
        PublishState();
        return true;
    }

    /// <summary>
    /// 몬스터 행동이 모두 끝난 뒤 예약된 카운트 감소를 적용하고 다음 상태를 결정합니다.
    /// </summary>
    public bool CompleteEnemyTurn()
    {
        if (Phase != RoundPhase.EnemyTurn)
        {
            return false;
        }

        bool decreasesCount = decreaseCountAfterEnemyTurn;
        decreaseCountAfterEnemyTurn = false;
        CompletePlayerAction(decreasesCount);

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
        decreaseCountAfterEnemyTurn = false;
        Resolution = RoundResolution.None;
        Phase = RoundPhase.PlayerTurn;
        PublishState();
    }

    /// <summary>
    /// 몬스터 행동 여부와 관계없이 플레이어 행동 이후의 카운트 및 패배 판정을 처리합니다.
    /// </summary>
    private void CompletePlayerAction(bool decreasesCount)
    {
        if (decreasesCount)
        {
            RemainingCount = Math.Max(0, RemainingCount - 1);
        }

        if (RemainingCount <= 0)
        {
            EndRun(RoundResolution.CountExpired);
            return;
        }

        Phase = RoundPhase.PlayerTurn;
        PublishState();
    }

    /// <summary>
    /// 현재 라운드를 성공 상태로 마치고 라운드 사이 상태로 전환합니다.
    /// </summary>
    private void ClearRound()
    {
        AliveEnemyCount = 0;
        decreaseCountAfterEnemyTurn = false;
        Resolution = RoundResolution.Cleared;
        Phase = RoundPhase.BetweenRounds;
        PublishState();
    }

    /// <summary>
    /// 지정한 패배 원인을 기록하고 게임 종료 상태로 전환합니다.
    /// </summary>
    private void EndRun(RoundResolution resolution)
    {
        decreaseCountAfterEnemyTurn = false;
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
