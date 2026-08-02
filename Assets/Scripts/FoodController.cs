using UnityEngine;

public class FoodController : MonoBehaviour
{
    [SerializeField] private float maxSpawnDistance = 15;

    private Transform playerTransform;
    private SpawnManager spawnManager;

    void Start()
    {
        playerTransform = GameObject.Find("Player").transform;
        spawnManager = GameObject.Find("SpawnManager").GetComponent<SpawnManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(playerTransform.position, transform.position) > maxSpawnDistance)
        {
            spawnManager.ObjectDestroyed();
            Destroy(gameObject);
            Debug.Log("Destroyed");
        }
        
    }
}
