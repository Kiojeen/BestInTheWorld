using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private int currentGameplayLevel = 0;
    [SerializeField] private int currentPlayerScore = 0;

    [SerializeField] private LevelData[] levels;

    public Transform GetPlayerTransform() => player.transform;
    public GameObject GetCurrentPlayerAvatar() => levels[currentGameplayLevel].playerAvatar;
    public LevelData GetCurrentLevelData() => levels[currentGameplayLevel];
    public int GetCurrentLevelFoodCount() => levels[currentGameplayLevel].food.Length;
    public int GetCurrentGameplayLevel() => currentGameplayLevel;

    public void IncreasePlayerScoreBy(int score)
    {
        currentPlayerScore += score;
    }



    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (currentPlayerScore >= GetCurrentLevelData().maxLevelScore && currentGameplayLevel < levels.Length - 1)
        {
            currentGameplayLevel++;
            currentPlayerScore = 0;
            player.GetComponent<PlayerController>().ReplacePlayerAvatar(levels[currentGameplayLevel].playerAvatar);
        }
    }
}
