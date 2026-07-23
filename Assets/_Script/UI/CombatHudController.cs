using UnityEngine;

/// <summary>
/// 라운드 카운트와 총 골드 변경을 TMP 숫자 및 크기 애니메이션에 연결합니다.
/// </summary>
public sealed class CombatHudController : MonoBehaviour
{
    [SerializeField] private CombatSceneBootstrap combatBootstrap = null;
    [SerializeField] private GoldWalletController goldWalletController = null;
    [SerializeField] private TMP_ScaleAnimator countAnimator = null;
    [SerializeField] private TMP_ScaleAnimator goldAnimator = null;

    private RoundFlowStateMachine subscribedRoundFlow;
    private int displayedCount = int.MinValue;
    private int displayedGold = int.MinValue;

    /// <summary>
    /// 컴포넌트를 처음 추가할 때 같은 GameObject의 전투 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    private void Reset()
    {
        combatBootstrap =
            GetComponent<CombatSceneBootstrap>();
        goldWalletController =
            GetComponent<GoldWalletController>();
    }

    /// <summary>
    /// 전투 초기화와 골드 변경 이벤트를 구독하고 이미 준비된 값을 즉시 표시합니다.
    /// </summary>
    private void OnEnable()
    {
        if (combatBootstrap != null)
        {
            combatBootstrap.CombatInitialized +=
                HandleCombatInitialized;

            if (combatBootstrap.IsInitialized)
            {
                BindRoundFlow();
            }
        }

        if (goldWalletController != null)
        {
            goldWalletController.GoldChanged +=
                HandleGoldChanged;
            UpdateGold(
                goldWalletController.TotalGold,
                animate: false);
        }
    }

    /// <summary>
    /// 시작 시 누락된 UI 참조를 알리고 현재 전투 및 골드 값을 다시 동기화합니다.
    /// </summary>
    private void Start()
    {
        ValidateReferences();

        if (combatBootstrap != null
            && combatBootstrap.IsInitialized)
        {
            BindRoundFlow();
        }

        if (goldWalletController != null)
        {
            UpdateGold(
                goldWalletController.TotalGold,
                animate: false);
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 모든 상태 변경 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (combatBootstrap != null)
        {
            combatBootstrap.CombatInitialized -=
                HandleCombatInitialized;
        }

        if (goldWalletController != null)
        {
            goldWalletController.GoldChanged -=
                HandleGoldChanged;
        }

        UnsubscribeRoundFlow();
    }

    /// <summary>
    /// 전투 초기화가 끝나면 새 RoundFlow를 구독하고 초기 카운트를 표시합니다.
    /// </summary>
    private void HandleCombatInitialized()
    {
        displayedCount = int.MinValue;
        BindRoundFlow();
    }

    /// <summary>
    /// RoundFlow 상태가 바뀌었을 때 실제 카운트 값이 변경된 경우에만 애니메이션을 재생합니다.
    /// </summary>
    private void HandleRoundStateChanged(
        RoundFlowSnapshot snapshot)
    {
        UpdateCount(
            snapshot.RemainingCount,
            animate: true);
    }

    /// <summary>
    /// 총 골드가 변경되면 새로운 값을 표시하고 크기 애니메이션을 재생합니다.
    /// </summary>
    private void HandleGoldChanged(int totalGold)
    {
        UpdateGold(totalGold, animate: true);
    }

    /// <summary>
    /// 카운트 숫자가 달라졌을 때 TMP에 적용하고 초기 표시가 아니라면 애니메이션을 재생합니다.
    /// </summary>
    private void UpdateCount(
        int count,
        bool animate)
    {
        if (count == displayedCount)
        {
            return;
        }

        bool shouldAnimate =
            animate && displayedCount != int.MinValue;
        displayedCount = count;
        countAnimator?.SetNumber(
            count,
            shouldAnimate);
    }

    /// <summary>
    /// 총 골드 숫자가 달라졌을 때 TMP에 적용하고 초기 표시가 아니라면 애니메이션을 재생합니다.
    /// </summary>
    private void UpdateGold(
        int totalGold,
        bool animate)
    {
        if (totalGold == displayedGold)
        {
            return;
        }

        bool shouldAnimate =
            animate && displayedGold != int.MinValue;
        displayedGold = totalGold;
        goldAnimator?.SetNumber(
            totalGold,
            shouldAnimate);
    }

    /// <summary>
    /// 현재 CombatSceneBootstrap이 소유한 RoundFlow의 상태 이벤트를 구독합니다.
    /// </summary>
    private void BindRoundFlow()
    {
        UnsubscribeRoundFlow();
        subscribedRoundFlow =
            combatBootstrap != null
                ? combatBootstrap.RoundFlow
                : null;

        if (subscribedRoundFlow == null)
        {
            return;
        }

        subscribedRoundFlow.StateChanged +=
            HandleRoundStateChanged;
        UpdateCount(
            subscribedRoundFlow.RemainingCount,
            animate: false);
    }

    /// <summary>
    /// 이전에 연결된 RoundFlow의 상태 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeRoundFlow()
    {
        if (subscribedRoundFlow != null)
        {
            subscribedRoundFlow.StateChanged -=
                HandleRoundStateChanged;
            subscribedRoundFlow = null;
        }
    }

    /// <summary>
    /// 카운트와 골드 UI 연결에 필요한 참조가 모두 지정되었는지 검사합니다.
    /// </summary>
    private void ValidateReferences()
    {
        if (combatBootstrap == null
            || goldWalletController == null
            || countAnimator == null
            || goldAnimator == null)
        {
            Debug.LogError(
                "CombatHudController requires CombatSceneBootstrap, GoldWalletController, Count Animator, and Gold Animator.",
                this);
        }
    }
}
