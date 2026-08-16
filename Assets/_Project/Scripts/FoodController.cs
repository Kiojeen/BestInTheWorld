using Solo.MOST_IN_ONE;
using UnityEngine;

public class FoodController : MonoBehaviour
{
    private GameManager gameManager;
    private SpawnManager spawnManager;

    [SerializeField] private float maxSpawnDistance = 10f;

    [SerializeField] private float rotationSpeed = 5f;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        spawnManager = FindAnyObjectByType<SpawnManager>();

        GetComponent<Collider>().isTrigger = true;
    }

    // Update is called once per frame
    void Update()
    {
        Transform playerTransform = gameManager.PlayerTransform;

        if (Vector3.Distance(playerTransform.position, transform.position) > maxSpawnDistance)
        {
            spawnManager.FoodObjectDestroyed();
            Destroy(gameObject);
        }

        float rotation = rotationSpeed * Time.deltaTime;
        transform.Rotate(rotation, rotation, rotation);

    }

    public void Collect()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.LightImpact);

        StartCollectEffect();
        gameManager.IncreasePlayerScoreBy(1);
        gameManager.ModifyHealth(1);

        spawnManager.FoodObjectDestroyed();
        Destroy(gameObject);
    }

    public void StartCollectEffect()
    {
        LevelData levelData = gameManager.CurrentLevelData;
        ParticleSystem effect = levelData.collectEffect;

        if (effect != null)
        {
            ParticleSystem ps = Instantiate(
                 effect,
                 transform.position,
                 Quaternion.identity
             );

            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
    }
}
