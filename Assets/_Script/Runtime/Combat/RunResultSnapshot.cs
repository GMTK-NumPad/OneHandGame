/// <summary>
/// 게임 결과 화면에 표시할 누적 진행 수치를 보관합니다.
/// </summary>
public readonly struct RunResultSnapshot
{
    /// <summary>
    /// 진행 턴, 추가 카운트다운, 실제 피해와 획득 골드 누적값을 생성합니다.
    /// </summary>
    public RunResultSnapshot(
        int turnsPlayed,
        int bonusCountdownEarned,
        int damageTaken,
        int goldEarned)
    {
        TurnsPlayed = turnsPlayed;
        BonusCountdownEarned =
            bonusCountdownEarned;
        DamageTaken = damageTaken;
        GoldEarned = goldEarned;
    }

    public int TurnsPlayed { get; }
    public int BonusCountdownEarned { get; }
    public int DamageTaken { get; }
    public int GoldEarned { get; }
}
