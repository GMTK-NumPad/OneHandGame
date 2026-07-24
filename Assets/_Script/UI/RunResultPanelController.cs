using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 종료 결과를 화면에 표시하고 버튼 입력으로 타이틀 씬을 불러옵니다.
/// </summary>
public sealed class RunResultPanelController : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField]
    private StageProgressionController stageProgressionController =
        null;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel = null;
    [SerializeField] private TMP_Text resultTitleText = null;
    [SerializeField] private TMP_Text turnsPlayedText = null;
    [SerializeField]
    private TMP_Text bonusCountdownEarnedText = null;
    [SerializeField] private TMP_Text damageTakenText = null;
    [SerializeField] private TMP_Text goldEarnedText = null;

    [Header("Title")]
    [SerializeField] private Button returnToTitleButton = null;
    [SerializeField] private string titleSceneName = "Title";

    private bool isResultVisible;

    /// <summary>
    /// 결과 패널을 숨긴 상태로 시작합니다.
    /// </summary>
    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 게임 종료 이벤트와 타이틀 이동 버튼 입력을 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        if (stageProgressionController != null)
        {
            stageProgressionController.RunEnded +=
                HandleRunEnded;
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(
                ReturnToTitle);
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화되면 등록했던 이벤트와 버튼 입력을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (stageProgressionController != null)
        {
            stageProgressionController.RunEnded -=
                HandleRunEnded;
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.RemoveListener(
                ReturnToTitle);
        }
    }

    /// <summary>
    /// 필수 진행 컨트롤러, 결과 패널, 결과 텍스트와 버튼이 연결되었는지 확인합니다.
    /// </summary>
    private void Start()
    {
        if (stageProgressionController == null
            || resultPanel == null
            || resultTitleText == null
            || turnsPlayedText == null
            || bonusCountdownEarnedText == null
            || damageTakenText == null
            || goldEarnedText == null
            || returnToTitleButton == null)
        {
            Debug.LogError(
                "RunResultPanelController requires StageProgressionController, result panel texts, and Return To Title Button.",
                this);
        }
    }

    /// <summary>
    /// 결과 화면이 열린 동안 넘패드 Enter 입력으로도 타이틀 이동 버튼을 실행합니다.
    /// </summary>
    private void Update()
    {
        if (isResultVisible
            && Keyboard.current != null
            && Keyboard.current.numpadEnterKey
                .wasPressedThisFrame)
        {
            ReturnToTitle();
        }
    }

    /// <summary>
    /// 전달받은 누적 결과를 텍스트에 반영하고 결과 패널을 표시합니다.
    /// </summary>
    private void HandleRunEnded(
        RunResultSnapshot result,
        RoundResolution resolution)
    {
        resultTitleText.text =
            resolution == RoundResolution.GameCleared
                ? "GAME CLEAR"
                : "DEFEAT";
        turnsPlayedText.text =
            result.TurnsPlayed.ToString();
        bonusCountdownEarnedText.text =
            result.BonusCountdownEarned.ToString();
        damageTakenText.text =
            result.DamageTaken.ToString();
        goldEarnedText.text =
            result.GoldEarned.ToString();
        isResultVisible = true;
        resultPanel.SetActive(true);
        returnToTitleButton.Select();
    }

    /// <summary>
    /// Build Profile에 등록된 타이틀 씬을 현재 씬 대신 불러옵니다.
    /// </summary>
    public void ReturnToTitle()
    {
        if (string.IsNullOrWhiteSpace(titleSceneName)
            || !Application.CanStreamedLevelBeLoaded(
                titleSceneName))
        {
            Debug.LogError(
                $"Title scene '{titleSceneName}' is not available. Add it to the active Build Profile and check the scene name.",
                this);
            return;
        }

        SceneManager.LoadScene(titleSceneName);
    }
}
