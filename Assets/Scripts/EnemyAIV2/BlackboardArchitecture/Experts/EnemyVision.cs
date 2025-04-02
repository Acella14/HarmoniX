using System.Collections;
using UnityEngine;
using BlackboardSystem;
using UnityServiceLocator;

public abstract class EnemyVision : MonoBehaviour {

    [Header("General Behavior")]
    [SerializeField] protected float patrolSpeed = 2f;
    [SerializeField] protected float chaseSpeed = 5f;
    [SerializeField] protected float detectionRadius = 10f;
    [SerializeField] public float trackingRange = 15f;
    [SerializeField] protected float chaseRange = 50f;
    public float GetChaseRange() => chaseRange;

    [SerializeField] protected float fovAngle = 90f;
    [SerializeField] protected float detectionDelay = 1f;
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] public LayerMask obstacleLayer;

    public bool canSeePlayer = false;  // Controlled by detection & chase logic
    public bool overrideDetection = false;
    public bool hasEverSeenPlayer = false;

    protected Blackboard blackboard;
    protected BlackboardKey playerLastPositionKey;

    public float GetPatrolSpeed() => patrolSpeed;
    public float GetChaseSpeed() => chaseSpeed;

    private Coroutine detectionRoutine;
    private WaitForSeconds detectionWait;

    protected virtual void Start() {
        blackboard = ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
        playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
        detectionWait = new WaitForSeconds(detectionDelay);
        
        if (detectionRoutine != null) {
            StopCoroutine(detectionRoutine);
        }
        detectionRoutine = StartCoroutine(DetectionRoutine());
    }

    private IEnumerator DetectionRoutine() {
        while (true) {
            DetectPlayer();
            yield return detectionWait;
        }
    }

    protected virtual void DetectPlayer() {
        canSeePlayer = false; // Reset unless detection occurs

        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        foreach (Collider col in colliders) {
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, col.transform.position);
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (overrideDetection) {
                // Allow continued tracking even if vision cone is temporarily broken
                if (distanceToTarget > trackingRange) {
                    Debug.Log($"<color=black>[LOST: Tracking Range Exceeded]</color> {gameObject.name} cannot track player anymore.");
                    overrideDetection = false; 
                    OnPlayerLost();
                    return;
                }
                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleLayer)) {
                    canSeePlayer = true;
                    Debug.Log($"<color=green>[CHASE MODE: Can See Player]</color> {gameObject.name} still sees the player.");
                    return;
                }
            } else {
                // Normal Vision Check
                if (angle < fovAngle * 0.5f && !Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleLayer)) {
                    Debug.Log($"<color=blue>[PLAYER DETECTED]</color> {gameObject.name} spotted the player!");
                    canSeePlayer = true;
                    OnPlayerDetected(col.transform.position);
                    return;
                }
            }
        }

        if (!overrideDetection) {
            Debug.Log($"<color=red>[PLAYER LOST]</color> {gameObject.name} lost sight of the player.");
            canSeePlayer = false;
            OnPlayerLost();
        }
    }



    protected virtual void OnPlayerDetected(Vector3 lastKnownPosition) {
        canSeePlayer = true;
        hasEverSeenPlayer = true;
        Debug.Log($"<color=blue>[DETECTED]</color> {gameObject.name} has spotted the player!");
    }

    protected virtual void OnPlayerLost() {
        // Do NOT reset `canSeePlayer` here anymore
        Debug.Log($"<color=black>[LOST SIGHT]</color> {gameObject.name} lost sight of the player.");
    }

    private void OnDrawGizmosSelected() {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw FOV cone
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.Euler(0, -fovAngle / 2, 0);
        Quaternion rightRayRotation = Quaternion.Euler(0, fovAngle / 2, 0);

        Vector3 leftRayDirection = leftRayRotation * forward * detectionRadius;
        Vector3 rightRayDirection = rightRayRotation * forward * detectionRadius;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, leftRayDirection);
        Gizmos.DrawRay(transform.position, rightRayDirection);

        // Draw tracking range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, trackingRange);
    }

    public void SetDetectionOverride(bool state) {
        overrideDetection = state;
    }
}
