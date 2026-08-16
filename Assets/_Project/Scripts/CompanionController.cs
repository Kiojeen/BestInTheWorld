using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CompanionController : MonoBehaviour
{
    private Transform target;
    private float orbitRadius;
    private float orbitSpeed;
    private float currentAngle;

    private Animator animator;
    public void Init(Transform followTarget, float radius, float speedDegPerSec, float startAngleDeg)
    {
        target = followTarget;
        orbitRadius = radius;
        orbitSpeed = speedDegPerSec;
        currentAngle = startAngleDeg;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (target == null) return;

        currentAngle += orbitSpeed * Time.deltaTime;    
        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * orbitRadius;
        transform.position = target.position + offset;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            animator.SetTrigger("Collect");
            FoodController food = other.GetComponent<FoodController>();
            if (food != null)
                food.Collect();
        }
    }
}