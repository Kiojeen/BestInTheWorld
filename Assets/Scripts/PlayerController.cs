using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
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

    private void Start()
    {
        playerRb = GetComponent<Rigidbody>();

        currentAvatar = transform.GetChild(0).gameObject;

        movementAction.Enable();
    }


    public void ReplacePlayerAvatar(GameObject newAvatar)
    {
        Vector3 localPos = currentAvatar.transform.localPosition;

        Destroy(currentAvatar);

        currentAvatar = Instantiate(newAvatar, transform);

        currentAvatar.transform.localPosition = localPos;
    }


    void Update()
    {
        Vector2 movement = movementAction.ReadValue<Vector2>();

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
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput);

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
            FoodController food = other.GetComponent<FoodController>();

            if (food != null)
            {
                food.Collect();
            }
        }
    }
}