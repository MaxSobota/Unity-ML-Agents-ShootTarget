using UnityEngine;

public class Bullet : MonoBehaviour{
    public float speed = 7f;
    public float lifetime = 3f; // Bullet destroys itself after this time

    [HideInInspector] public ShootTarget shooterAgent; // The agent that fired this bullet
    private Rigidbody rb;
    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward * speed;

        // Prevent fast bullets from skipping collisions
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Auto-destroy after a few seconds
        Destroy(gameObject, lifetime);
    }
    void FixedUpdate() {
        Vector3 moveStep = transform.forward * speed * Time.fixedDeltaTime;

        // Check for collision BEFORE moving
        if(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveStep.magnitude)) {
            HandleCollision(hit.collider);
            return; // Stop further movement
        }

        // Move normally if no collision detected
        rb.MovePosition(rb.position + moveStep);
    }

    void HandleCollision(Collider other){
        if(other.gameObject.CompareTag("Target")) {
            if(shooterAgent != null) {
                shooterAgent.ChangeFloor(true);  // Change floor to winMaterial
                shooterAgent.AddReward(1f); // Reward for hitting the target
                shooterAgent.EndEpisode();  // End the episode
                Destroy(gameObject); // Destroy the bullet on impact
            }
        }
        else {
            shooterAgent.AddReward(-0.1f); // Slight negative reward for not hitting the target
            Destroy(gameObject); // Destroy the bullet on impact
        }
    }
}
