using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private UIDocument uiDocument;
    private GameManager gameManager;

    private Label avatarNameLabel;
    private Label levelLabel;


    private Label healthValue;
    private VisualElement healthBarFill;

    private Label scoreValue;
    private VisualElement scoreBarFill;

    private Button pauseButton;
    private bool isPaused;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        gameManager = FindAnyObjectByType<GameManager>();

        var root = uiDocument.rootVisualElement;
        avatarNameLabel = root.Q<Label>("AvatarName");
        levelLabel = root.Q<Label>("CurrentPlayerLevel");


        scoreValue = root.Q<Label>("ScoreValue");
        scoreBarFill = root.Q<VisualElement>("ScoreBarFill");

        healthValue = root.Q<Label>("HealthValue");
        healthBarFill = root.Q<VisualElement>("HealthBarFill");

        pauseButton = root.Q<Button>("ControlButtonPause");
        pauseButton.clicked += TogglePause;

        gameManager.OnLevelChanged += HandleLevelChanged;
        gameManager.OnScoreChanged += HandleScoreChanged;

    }

    void OnDisable()
    {
        gameManager.OnLevelChanged -= HandleLevelChanged;
        gameManager.OnScoreChanged -= HandleScoreChanged;
    }

    void Update()
    {
        UpdateHealthBar();
        healthValue.text = $"{gameManager.CurrentPlayerHealth} / {gameManager.MaxPlayerHealth}";
    }

    private void HandleLevelChanged()
    {
        avatarNameLabel.text = gameManager.CurrentLevelData.avatarName;
        levelLabel.text = (gameManager.GetCurrentGameplayLevel() + 1).ToString();

    }

    private void HandleScoreChanged()
    {
        UpdateScoreBar();
        scoreValue.text = $"{gameManager.CurrentPlayerScore} / {gameManager.CurrentLevelData.maxLevelScore}";
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        pauseButton.EnableInClassList("control-btn--active", isPaused);
    }

    private void UpdateScoreBar()
    {
        float pct = Mathf.Clamp01(( float )gameManager.CurrentPlayerScore / gameManager.CurrentLevelData.maxLevelScore) * 100f;
        scoreBarFill.style.width = new Length(pct, LengthUnit.Percent);
    }

    private void UpdateHealthBar()
    {
        float pct = Mathf.Clamp01(gameManager.CurrentPlayerHealth / gameManager.MaxPlayerHealth) * 100f;
        healthBarFill.style.width = new Length(pct, LengthUnit.Percent);
    }
}