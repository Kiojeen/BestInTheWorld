using Solo.MOST_IN_ONE;
using UnityEngine;

public class CollectableCompanionController : MonoBehaviour
{
    private GameManager gameManager;
    private SpawnManager spawnManager;

    [SerializeField] private float maxSpawnDistance = 10f;

    [SerializeField] private int id;

    public int Id => id;

    public void Init(int id)
    {
        this.id = id;
    }

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (spawnManager == null)
            spawnManager = FindAnyObjectByType<SpawnManager>();
    }

    void Start()
    {

    }

    void Update()
    {
        Transform playerTransform = gameManager.PlayerTransform;

        if (Vector3.Distance(playerTransform.position, transform.position) > maxSpawnDistance)
        {
            spawnManager.CollectableCompanionNotCollected(id);
            Destroy(gameObject);
        }

    }

    private void FixedUpdate()
    {

    }

    public void Collect()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.LightImpact);

        StartCollectEffect();

        spawnManager.CollectableCompanionObjectDestroyed();
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
