using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    private GameManager gameManager;

    [SerializeField] private int maxSpawnedObjects = 20;
    [SerializeField] private float spawnInterval = 0.1f;

    [Header("Haters")]
    [SerializeField] private GameObject[] haterPrefabs;
    [SerializeField] private float haterCooldown = 10f;
    [SerializeField] private float haterFollowSpeed = 0.3f;
    [SerializeField] private int haterDamage = 5;
    [SerializeField] private float haterSpawnRadiusX = 12f;
    [SerializeField] private float haterSpawnRadiusZ = 6f;
    [SerializeField] private float haterMinDistance = 2f;
    [SerializeField] private float haterDespawnDistance = 10f;
    [SerializeField] private float haterScale = 0.6f;
    [SerializeField] private float haterRotationSpeed = 15f;
    [SerializeField] private ParticleSystem haterHitEffect;

    private int currentSpawnedObjects = 0;
    private int currentSpanedCollectableCompanions = 0;
    private int currentSpawnedHaters = 0;

    private readonly HashSet<int> spawnedCompanionIds = new HashSet<int>();

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        InvokeRepeating(nameof(SpawnObject), 0f, spawnInterval);
        InvokeRepeating(nameof(SpawnCompanion), 10, 15);

        StartCoroutine(HaterRoutine());
    }

    private IEnumerator HaterRoutine()
    {
        yield return new WaitForSeconds(haterCooldown);

        while (true)
        {
            if (gameManager.CurrentPlayerScore >= gameManager.CurrentLevelData.maxLevelScore)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            SpawnHater();

            while (currentSpawnedHaters > 0)
                yield return null;

            yield return new WaitForSeconds(haterCooldown);
        }
    }

    private GameObject GetRandomFoodObjForCurrentLevel()
    {
        LevelData currentLevelData = gameManager.CurrentLevelData;
        int foodObjCount = currentLevelData.food.Length;
        int randomIdx = Random.Range(0, foodObjCount);

        return currentLevelData.food[randomIdx];
    }

    void SpawnCompanion()
    {
        if (currentSpanedCollectableCompanions >= 1)
            return;

        LevelData levelData = gameManager.CurrentLevelData;
        Companion[] companions = levelData.playerCompanions;

        GameObject nextCompanion = null;
        int id = -1;

        for (int i = 0; i < companions.Length; ++i)
        {
            Companion companion = companions[i];

            if (!companion.isOwned && !spawnedCompanionIds.Contains(i))
            {
                nextCompanion = companion.companionAvatar;
                id = i;
                break;
            }
        }

        if (nextCompanion == null)
            return;

        float spawnRadiusX = 10f;
        float spawnRadiusZ = 5f;

        Transform playerTransform = gameManager.PlayerTransform;

        Vector3 randomPosition = new Vector3(
            playerTransform.position.x + Random.Range(-spawnRadiusX, spawnRadiusX),
            0f,
            playerTransform.position.z + Random.Range(-spawnRadiusZ, spawnRadiusZ)
        );

        GameObject instance = Instantiate(
            nextCompanion,
            randomPosition,
            Quaternion.Euler(-90, 0, 180),
            transform
        );

        instance.tag = "CollectableCompanion";
        instance.layer = 0;

        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();
        if (boxCollider == null)
            instance.AddComponent<BoxCollider>();

        boxCollider.isTrigger = true;

        CollectableCompanionController ccc = instance.AddComponent<CollectableCompanionController>();
        ccc.Init(id);

        spawnedCompanionIds.Add(id);
        currentSpanedCollectableCompanions++;
    }

    void SpawnObject()
    {
        if (currentSpawnedObjects >= maxSpawnedObjects || gameManager.CurrentLevelFoodCount == 0)
            return;

        GameObject randomFoodObjForCurrentLevel = GetRandomFoodObjForCurrentLevel();

        float spawnRadiusX = 10f;
        float spawnRadiusZ = 5f;

        Transform playerTransform = gameManager.PlayerTransform;

        Vector3 randomPosition = new Vector3(
            playerTransform.position.x + Random.Range(-spawnRadiusX, spawnRadiusX),
            0f,
            playerTransform.position.z + Random.Range(-spawnRadiusZ, spawnRadiusZ)
        );

        GameObject instance = Instantiate(
            randomFoodObjForCurrentLevel,
            randomPosition,
            Quaternion.identity,
            transform
        );

        instance.tag = "Food";

        if (instance.GetComponent<FoodController>() == null)
        {
            instance.AddComponent<FoodController>();
        }

        currentSpawnedObjects++;
    }

    void SpawnHater()
    {
        if (haterPrefabs == null || haterPrefabs.Length == 0)
            return;

        Transform playerTransform = gameManager.PlayerTransform;

        Vector3 randomPosition = Vector3.zero;
        int attempts = 0;
        do
        {
            randomPosition = new Vector3(
                playerTransform.position.x + Random.Range(-haterSpawnRadiusX, haterSpawnRadiusX),
                0f,
                playerTransform.position.z + Random.Range(-haterSpawnRadiusZ, haterSpawnRadiusZ)
            );
            attempts++;
        }
        while (Vector3.Distance(playerTransform.position, randomPosition) < haterMinDistance && attempts < 10);

        GameObject haterPrefab = haterPrefabs[Random.Range(0, haterPrefabs.Length)];

        GameObject instance = Instantiate(
            haterPrefab,
            randomPosition,
            Quaternion.Euler(-90, 0, 180),
            transform
        );

        instance.tag = "Hater";
        instance.layer = 0;
        instance.transform.localScale = Vector3.one * haterScale;

        HaterController hater = instance.AddComponent<HaterController>();
        hater.Init(haterFollowSpeed, haterDamage, haterDespawnDistance, haterHitEffect, haterRotationSpeed);

        currentSpawnedHaters++;
    }

    public void HaterDestroyed()
    {
        currentSpawnedHaters = Mathf.Max(0, currentSpawnedHaters - 1);
    }

    public void FoodObjectDestroyed()
    {
        currentSpawnedObjects = Mathf.Max(0, currentSpawnedObjects - 1);
    }

    public void CollectableCompanionObjectDestroyed()
    {
        currentSpanedCollectableCompanions = 0;
    }

    public void CollectableCompanionNotCollected(int id)
    {
        currentSpanedCollectableCompanions = 0;
        spawnedCompanionIds.Remove(id);
    }

    public void DestroyAllObjects()
    {
        foreach (Transform child in transform)
        {
            FoodController fc = child.GetComponent<FoodController>();
            if (fc != null)
            {
                fc.StartCollectEffect();
            }

            Destroy(child.gameObject);
        }

        currentSpawnedObjects = 0;
        spawnedCompanionIds.Clear();
        currentSpanedCollectableCompanions = 0;
        currentSpawnedHaters = 0;
    }

}