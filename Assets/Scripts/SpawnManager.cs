using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    private Transform playerTransform;


    [SerializeField] private LevelData[] levels;

    [SerializeField] private int maxSpawnedObjects = 20;
    [SerializeField] private float spawnInterval = 0.1f;

    private int currentSpawnedObjects = 0;

    [SerializeField] private int currentLevel = 1;

    void Start()
    {
        playerTransform = GameObject.Find("Player").transform;
        InvokeRepeating(nameof(SpawnObject), 0f, spawnInterval);
    }

    void SpawnObject()
    {
        if (currentSpawnedObjects >= maxSpawnedObjects)
            return;

        int randomIndex = Random.Range(0, levels[currentLevel - 1].spawnables.Length);
        GameObject prefab = levels[currentLevel - 1].spawnables[randomIndex];

        float spawnRadiusX = 10f;
        float spawnRadiusZ = 5f;

        Vector3 randomPosition = new Vector3(
            playerTransform.position.x + Random.Range(-spawnRadiusX, spawnRadiusX),
            0f,
            playerTransform.position.z + Random.Range(-spawnRadiusZ, spawnRadiusZ)
        );

        GameObject obj = Instantiate(prefab, randomPosition, Quaternion.identity);
        obj.transform.localScale = new Vector3(1, 1, 1);

        currentSpawnedObjects++;
    }

    public void ObjectDestroyed()
    {
        currentSpawnedObjects--;
    }

}