using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FoodController : MonoBehaviour
{
    private GameManager gameManager;
    private SpawnManager spawnManager;

    [SerializeField] private float maxSpawnDistance = 30;

    [SerializeField] private float rotationSpeed = 5f;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        spawnManager = FindAnyObjectByType<SpawnManager>();
    }

    // Update is called once per frame
    void Update()
    {
        Transform playerTransform = gameManager.GetPlayerTransform();

        if (Vector3.Distance(playerTransform.position, transform.position) > maxSpawnDistance)
        {
            spawnManager.ObjectDestroyed();
            Destroy(gameObject);
        }

        float rotation = rotationSpeed * Time.deltaTime;
        transform.Rotate(rotation, rotation, rotation);

    }

    public void Collect()
    {
        LevelData levelData = gameManager.GetCurrentLevelData();

        if (levelData.foodCollectEffect != null)
        {
            Instantiate(
                levelData.foodCollectEffect,
                transform.position,
                Quaternion.identity
            );
        }

        gameManager.IncreasePlayerScoreBy(1);

        spawnManager.ObjectDestroyed();
        Destroy(gameObject);
    }
}
