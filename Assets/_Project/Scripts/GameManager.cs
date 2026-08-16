using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private SpawnManager spawnManager;

    [SerializeField] private GameObject player;
    private PlayerController playerController;

    [SerializeField] private int currentGameplayLevel = 0;
    [SerializeField] private int currentPlayerScore = 0;
    [SerializeField] private int maxPlayerHealth = 100;
    [SerializeField] private int currentPlayerHealth = 100;

    [SerializeField] private LevelData[] levels;

    public event Action OnLevelChanged;
    public event Action OnScoreChanged;

    public int CurrentPlayerScore => currentPlayerScore;

    public Transform PlayerTransform => player.transform;

    public GameObject CurrentPlayerAvatar => levels[currentGameplayLevel].playerAvatar;

    public int CurrentLevelFoodCount => levels[currentGameplayLevel].food.Length;

    public LevelData CurrentLevelData => levels[currentGameplayLevel];

    public int CurrentPlayerHealth => currentPlayerHealth;
    public int MaxPlayerHealth => maxPlayerHealth;


    public int GetCurrentGameplayLevel() => currentGameplayLevel;

    public void IncreasePlayerScoreBy(int score)
    {
        currentPlayerScore += score;
        OnScoreChanged?.Invoke();
    }


    public void ModifyHealth(int amount)
    {
        currentPlayerHealth = Mathf.Clamp(currentPlayerHealth + amount, 0, maxPlayerHealth);
    }


    void Start()
    {
        spawnManager = FindAnyObjectByType<SpawnManager>();

        playerController = player.GetComponent<PlayerController>();
        LevelData levelData = levels[currentGameplayLevel];
        playerController.ReplacePlayerAvatar(levelData.playerAvatar, levelData.avatarChangeEffect);
        OnLevelChanged?.Invoke();
        OnScoreChanged?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentPlayerScore >= CurrentLevelData.maxLevelScore && currentGameplayLevel < levels.Length - 1)
        {
            spawnManager.DestroyAllObjects();

            currentGameplayLevel++;
            currentPlayerScore = 0;
            OnScoreChanged?.Invoke();
            LevelData levelData = levels[currentGameplayLevel];
            playerController.ReplacePlayerAvatar(levelData.playerAvatar, levelData.avatarChangeEffect);
            OnLevelChanged?.Invoke();
        }
    }
}