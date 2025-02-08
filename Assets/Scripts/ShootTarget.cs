using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using static UnityEngine.GraphicsBuffer;
// mlagents-learn config/shootTarget1.yaml --force --time-scale=10
public class ShootTarget : Agent {
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform targetTransform; // Transform of the target we're shooting
    //private Rigidbody targetRigidbody; // Rigidbody of the target (to get its velocity)
    [SerializeField] private Material winMaterial; // To change floor color
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;

    public float moveSpeed = 3f;
    public float rotationSpeed = 100f;
    public float shootCooldown = 1f;

    private Rigidbody rb;
    private float lastFireTime;

    void Start() {
        rb = GetComponent<Rigidbody>(); // To get this velocity
        //targetRigidbody = targetTransform.GetComponent<Rigidbody>(); // Get Target rigidbody
    }
    public override void OnEpisodeBegin(){
        transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f)); // Spawn in a random 12x12 area
        transform.rotation = Quaternion.identity; // Set to same rotation every time
        targetTransform.localPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f)); // Change this later for other agent

        lastFireTime = Time.time; // Reset shoot cooldown
    }
    public override void CollectObservations(VectorSensor sensor){ // Need sensors for this position, target position, this velocity, target velocity, detection radius
        // Direction to the target
        Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;
        // Calculate angle between the agent's forward vector and the target direction
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        // sensor.AddObservation(transform.localPosition);
        // sensor.AddObservation(targetTransform.localPosition);

        sensor.AddObservation(directionToTarget); // Direction to Target
        sensor.AddObservation((Time.time - lastFireTime) / shootCooldown); // Time since last shot

        // Add the angle as an observation
        sensor.AddObservation(angleToTarget / 180f);  // Normalize the angle to range [0, 1]

        // sensor.AddObservation(Vector3.Distance(transform.position, targetTransform.position) / 10f); // Distance to target
        // sensor.AddObservation(rb.velocity); // Agent velocity
        // sensor.AddObservation(targetTransform); // Agent angular velocity
        // sensor.AddObservation(targetRigidbody.velocity); // Target velocity
    }
    public override void OnActionReceived(ActionBuffers actions){
        // WASD Movement
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;

        // Q/E Rotation
        float rotateInput = actions.ContinuousActions[2];
        transform.Rotate(Vector3.up, rotateInput * rotationSpeed * Time.deltaTime);

        // Shooting action
        if(actions.DiscreteActions[0] == 1 && Time.time - lastFireTime > shootCooldown){Shoot();}

        AddReward(-0.001f); // Adds a small negative reward for taking too long, really only applies when target is hit

        //Debug.Log("moveX: " + actions.ContinuousActions[0]);
        //Debug.Log("moveZ: " + actions.ContinuousActions[1]);
        //Debug.Log("rotate: " + actions.DiscreteActions[1]);
        //Debug.Log("shoot: " + actions.DiscreteActions[0]);
    }
    void Shoot(){
        GameObject bulletObj = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

        // Assign the agent reference to the bullet
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        if(bulletScript != null){
            bulletScript.shooterAgent = this; // Pass this agent to the bullet
        }

        lastFireTime = Time.time;
    }
    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // Keyboard movement (WASD)
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
        // Rotation (Q/E)
        if(Input.GetKey(KeyCode.Q))
            continuousActions[2] = -1f; // Rotate left
        else if(Input.GetKey(KeyCode.E))
            continuousActions[2] = 1f; // Rotate right
        else
            continuousActions[2] = 0f;

        if(Input.GetKey(KeyCode.Space)){ // Space to fire
            discreteActions[0] = 1;
        }
        else{
            discreteActions[0] = 0;
        }
    }
    private void OnCollisionEnter(Collision collision){
        //if(collision.gameObject.CompareTag("Bullet")) {
        //    SetReward(-1f); // Apply a larger penalty for getting shot
        //    EndEpisode(); // Restart the episode
        //}
        if(collision.gameObject.CompareTag("Wall")){
            floorMeshRenderer.material = loseMaterial; // Change to lose material
            SetReward(-0.5f); // Apply a penalty for hitting the wall
            EndEpisode(); // Restart the episode
        }
        if(collision.gameObject.CompareTag("Target")){
            floorMeshRenderer.material = loseMaterial; // Change to lose material
            SetReward(-1f); // Apply a larger penalty for running into the target
            EndEpisode(); // Restart the episode
        }
    }
    public void ChangeFloor(bool won){
        floorMeshRenderer.material = won ? winMaterial : loseMaterial;
    }
}
