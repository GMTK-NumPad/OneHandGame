using System;
using UnityEngine;

/// <summary>
/// 처치된 몬스터의 스폰 시점 골드 보상을 지갑에 누적하고 변경 이벤트를 전달합니다.
/// </summary>
public sealed class GoldWalletController : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner = null;

    private GoldWallet wallet;

    public event Action<int> GoldChanged;
    public int TotalGold => EnsureWallet().TotalGold;
    public int TotalGoldEarned =>
        EnsureWallet().TotalGoldEarned;

    /// <summary>
    /// 컴포넌트를 처음 추가할 때 같은 GameObject의 EnemySpawner를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        enemySpawner = GetComponent<EnemySpawner>();
    }

    /// <summary>
    /// 새로운 게임 진행에서 사용할 빈 골드 지갑을 생성합니다.
    /// </summary>
    private void Awake()
    {
        wallet = new GoldWallet();
    }

    /// <summary>
    /// 몬스터 처치 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.EnemyDefeated +=
                HandleEnemyDefeated;
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 몬스터 처치 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.EnemyDefeated -=
                HandleEnemyDefeated;
        }
    }

    /// <summary>
    /// 처치된 몬스터에게 저장된 골드를 한 번 지급하고 변경된 총 골드를 알립니다.
    /// </summary>
    private void HandleEnemyDefeated(EnemyActor enemy)
    {
        if (enemy == null
            || !enemy.IsInitialized)
        {
            return;
        }

        AddGold(enemy.RuntimeState.GoldReward);
    }

    /// <summary>
    /// 몬스터 보상 외의 정산 골드를 지갑에 더하고 변경된 총 골드를 알립니다.
    /// </summary>
    public int AddGold(int amount)
    {
        int addedGold =
            EnsureWallet().AddGold(amount);

        if (addedGold > 0)
        {
            GoldChanged?.Invoke(wallet.TotalGold);
        }

        return addedGold;
    }

    /// <summary>
    /// 아직 생성되지 않은 경우 빈 골드 지갑을 생성해 반환합니다.
    /// </summary>
    private GoldWallet EnsureWallet()
    {
        wallet ??= new GoldWallet();
        return wallet;
    }
}
