using UnityEngine;

public class HaterController : MonoBehaviour
{
    private GameManager gameManager;
    private SpawnManager spawnManager;

    private float followSpeed;
    private int damage;
    private float despawnDistance;
    private float rotationSpeed;
    private ParticleSystem hitEffect;

    [SerializeField] private float attackRange = 0.5f;

    private bool hasHit = false;

    public void Init(float followSpeed, int damage, float despawnDistance, ParticleSystem hitEffect, float rotationSpeed)
    {
        this.followSpeed = followSpeed;
        this.damage = damage;
        this.despawnDistance = despawnDistance;
        this.hitEffect = hitEffect;
        this.rotationSpeed = rotationSpeed;
    }

    void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (spawnManager == null)
            spawnManager = FindAnyObjectByType<SpawnManager>();

    }

    void Update()
    {
        Transform playerTransform = gameManager.PlayerTransform;
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (Vector3.Distance(playerTransform.position, transform.position) > despawnDistance)
        {
            spawnManager.HaterDestroyed();
            Destroy(gameObject);
        }

        transform.position += direction.normalized * followSpeed * Time.deltaTime;
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

    }

    private void Despawn()
    {
        spawnManager.HaterDestroyed();
        Destroy(gameObject);
    }

    public void Hit()
    {
        hasHit = true;

        gameManager.ModifyHealth(-damage);
        spawnManager.HaterDestroyed();

        PlayHitEffect();

        Destroy(gameObject);
    }

    private void PlayHitEffect()
    {
        if (hitEffect == null)
            return;

        ParticleSystem ps = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}