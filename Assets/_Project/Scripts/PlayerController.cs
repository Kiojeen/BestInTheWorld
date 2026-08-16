using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private GameManager gameManager;
    private SoundManager soundManager;

    private Rigidbody playerRb;
    private GameObject currentAvatar;

    [SerializeField] private InputAction movementAction;

    [SerializeField] private float speed = 0.2f;
    [SerializeField] private float maxSpeed = 0.5f;
    private float horizontalInput;
    private float verticalInput;

    [SerializeField] private float spinSpeed = 10f;
    [SerializeField] private float speedMultiplier = 2f;
    private float spinDirection = 1f;
    private float currentZRotation = 180f;

    private Vector3 targetPosition;
    private bool hasTarget = false;
    [Tooltip("The distance to the target at which the player will stop applying force.")]
    [SerializeField] private float stopDistance = 0.2f;

    [Header("Companions")]
    [SerializeField] private float companionOrbitRadius = 1.5f;
    [SerializeField] private float companionOrbitSpeed = 40f; // degrees/sec

    private readonly List<GameObject> activeCompanions = new List<GameObject>();

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

        gameManager.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnLevelChanged -= HandleLevelChanged;
    }

    private void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        movementAction.Enable();
    }

    private void HandleLevelChanged()
    {
        LevelData levelData = gameManager.CurrentLevelData;
        ReplacePlayerAvatar(levelData.playerAvatar, levelData.avatarChangeEffect);
        SetupCompanions(levelData);
    }

    public void ReplacePlayerAvatar(GameObject newAvatar, ParticleSystem effect = null)
    {
        soundManager.PlayCharacterChangeSound();

        if (currentAvatar != null)
            Destroy(currentAvatar);

        currentAvatar = Instantiate(newAvatar, transform);
        currentAvatar.transform.localPosition = Vector3.zero;

        Renderer renderer = currentAvatar.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Vector3 offset = renderer.bounds.center - currentAvatar.transform.position;
            currentAvatar.transform.position -= offset;
        }

        if (effect != null)
        {
            ParticleSystem ps = Instantiate(effect, currentAvatar.transform);
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
    }

    private void SetupCompanions(LevelData levelData)
    {
        ClearCompanions();

        if (!levelData.playerCanHaveCompanions || levelData.playerCompanions == null)
            return;

        List<Companion> owned = new List<Companion>();
        foreach (var c in levelData.playerCompanions)
        {
            if (c.isOwned && c.companionAvatar != null)
                owned.Add(c);
        }

        for (int i = 0; i < owned.Count; i++)
        {
            float angle = (360f / owned.Count) * i;
            GameObject instance = Instantiate(owned[i].companionAvatar, transform);

            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb == null)
                rb = instance.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;

            CompanionController controller = instance.GetComponent<CompanionController>();
            if (controller == null)
                controller = instance.AddComponent<CompanionController>();

            controller.Init(transform, companionOrbitRadius, companionOrbitSpeed, angle);
            instance.layer = 6;
            activeCompanions.Add(instance);
        }
    }

    private void ClearCompanions()
    {
        foreach (var companion in activeCompanions)
        {
            if (companion != null)
                Destroy(companion);
        }
        activeCompanions.Clear();
    }

    public void SetCompanionOwned(int companionIndex, bool owned)
    {
        Companion[] companions = gameManager.CurrentLevelData.playerCompanions;

        if (companionIndex < 0 || companionIndex >= companions.Length)
        {
            Debug.LogWarning($"Invalid companion index: {companionIndex}");
            return;
        }

        Companion c = companions[companionIndex];
        c.isOwned = owned;
        companions[companionIndex] = c;

        SetupCompanions(gameManager.CurrentLevelData);
    }

    void Update()
    {
        Vector2 movement = movementAction.ReadValue<Vector2>();

        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            Plane movementPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (movementPlane.Raycast(ray, out float enterDistance))
            {
                targetPosition = ray.GetPoint(enterDistance);
                hasTarget = true;
            }
        }

        horizontalInput = -movement.x;
        verticalInput = movement.y;

        if (horizontalInput > 0.01f)
            spinDirection = 1f;
        else if (horizontalInput < -0.01f)
            spinDirection = -1f;

        float spin = spinSpeed + playerRb.linearVelocity.magnitude * speedMultiplier;
        currentZRotation += spin * spinDirection * Time.deltaTime;

        transform.rotation = Quaternion.Euler(-90, 0, currentZRotation);
    }

    void FixedUpdate()
    {
        Vector3 moveDirection;

        if (hasTarget)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = transform.position.y;

            if (toTarget.magnitude > stopDistance)
            {
                moveDirection = toTarget.normalized;
                horizontalInput = -moveDirection.x;
                verticalInput = -moveDirection.y;
            }
            else
            {
                hasTarget = false;
                moveDirection = Vector3.zero;
                horizontalInput = 0;
                verticalInput = 0;
            }
        }
        else
        {
            moveDirection = new Vector3(horizontalInput, 0f, verticalInput);
        }

        playerRb.AddForce(moveDirection * speed);

        if (playerRb.linearVelocity.magnitude > maxSpeed)
        {
            playerRb.linearVelocity = playerRb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            soundManager.PlayEatSound();
            FoodController food = other.GetComponent<FoodController>();
            if (food != null)
                food.Collect();
        }
        else if (other.CompareTag("CollectableCompanion"))
        {
            CollectableCompanionController ccc = other.GetComponent<CollectableCompanionController>();
            if (ccc != null && !gameManager.CurrentLevelData.playerCompanions[ccc.Id].isOwned)
            {
                int id = ccc.Id;
                ccc.Collect();
                SetCompanionOwned(id, true);
            }
        }
        else if (other.CompareTag("Hater"))
        {
            HaterController hc = other.GetComponent<HaterController>();
            if (hc != null)
            {
                soundManager.PlayEnemyHitSound();
                hc.Hit();
            }
        }
    }
}