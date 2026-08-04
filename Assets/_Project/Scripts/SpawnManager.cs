using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    private GameManager gameManager;

    [SerializeField] private int maxSpawnedObjects = 20;
    [SerializeField] private float spawnInterval = 0.1f;

    private int currentSpawnedObjects = 0;


    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        InvokeRepeating(nameof(SpawnObject), 0f, spawnInterval);
    }

    private GameObject GetRandomFoodObjForCurrentLevel()
    {
        LevelData currentLevelData = gameManager.GetCurrentLevelData();
        int foodObjCount = currentLevelData.food.Length;
        int randomIdx = Random.Range(0, foodObjCount);

        return currentLevelData.food[randomIdx];
    }

    void SpawnObject()
    {
        if (currentSpawnedObjects >= maxSpawnedObjects || gameManager.GetCurrentLevelFoodCount() == 0)
            return;

        GameObject randomFoodObjForCurrentLevel = GetRandomFoodObjForCurrentLevel();

        float spawnRadiusX = 10f;
        float spawnRadiusZ = 5f;

        Transform playerTransform = gameManager.GetPlayerTransform();

        Vector3 randomPosition = new Vector3(
            playerTransform.position.x + Random.Range(-spawnRadiusX, spawnRadiusX),
            0f,
            playerTransform.position.z + Random.Range(-spawnRadiusZ, spawnRadiusZ)
        );

        Instantiate(randomFoodObjForCurrentLevel, randomPosition, Quaternion.identity);

        currentSpawnedObjects++;
    }

    public void ObjectDestroyed()
    {
        currentSpawnedObjects = Mathf.Max(0, currentSpawnedObjects - 1);
    }

}