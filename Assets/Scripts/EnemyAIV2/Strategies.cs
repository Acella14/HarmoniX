using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using BlackboardSystem;
using UnityServiceLocator;

public interface IStrategy {
    Node.Status Process();
    void Reset() {
        // Noop
    }
}

public class ActionStrategy : IStrategy {
    readonly Action doSomething;

    public ActionStrategy(Action doSomething) {
        this.doSomething = doSomething;
    }

    public Node.Status Process() {
        doSomething();
        return Node.Status.Success;
    }
}

public class Condition : IStrategy {
    readonly Func<bool> predicate;

    public Condition(Func<bool> predicate) {
        this.predicate = predicate;
    }

    public Node.Status Process() {
        bool result = predicate();
        Debug.Log($"<color=orange>[Condition Node] - Result: {result}</color>");
        return result ? Node.Status.Success : Node.Status.Failure;
    }

    //public Node.Status Process() => predicate() ? Node.Status.Success : Node.Status.Failure;
}

public class PatrolStrategy : IStrategy {
    private readonly Transform entity;
    private readonly NavMeshAgent agent;
    private readonly List<Transform> patrolPoints;
    private readonly float patrolSpeed;
    private readonly bool loopPatrol;
    private int currentIndex;
    private bool isPathCalculated;

    public PatrolStrategy(Transform entity, NavMeshAgent agent, List<Transform> patrolPoints, float patrolSpeed, bool loopPatrol = true) {
        this.entity = entity;
        this.agent = agent;
        this.patrolPoints = patrolPoints;
        this.patrolSpeed = patrolSpeed;
        this.loopPatrol = loopPatrol;
    }

    public Node.Status Process() {
        if (entity.TryGetComponent<Scout>(out Scout scout)) {
            scout.currentState = Scout.AIState.Patrol;
        }
        
        if (patrolPoints.Count == 0) return Node.Status.Failure;

        if (currentIndex >= patrolPoints.Count) {
            if (loopPatrol) {
                currentIndex = 0;
            } else {
                return Node.Status.Success;
            }
        }

        Transform target = patrolPoints[currentIndex];
        agent.speed = patrolSpeed;
        agent.SetDestination(target.position);

        if (isPathCalculated && agent.remainingDistance < 0.1f) {
            currentIndex++;
            isPathCalculated = false;
        }

        if (agent.pathPending) {
            isPathCalculated = true;
        }

        return Node.Status.Running;
    }

    public void Reset() {
        currentIndex = 0;
        isPathCalculated = false;
    }
}

public class ChasePlayerStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly NavMeshAgent agent;
    private readonly float chaseSpeed;
    private readonly Blackboard blackboard;
    private readonly BlackboardKey playerLastPositionKey;
    private Vector3 lastKnownPosition;
    private EnemyVision vision; // Reference to vision system

    public ChasePlayerStrategy(MonoBehaviour enemy, NavMeshAgent agent, float chaseSpeed, Blackboard blackboard) {
        this.enemy = enemy;
        this.agent = agent;
        this.chaseSpeed = chaseSpeed;
        this.blackboard = blackboard;
        this.vision = enemy.GetComponent<EnemyVision>();
        this.playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
    }

    public Node.Status Process() {
        if (enemy is Scout scout) {
            scout.currentState = Scout.AIState.Chase;
        }

        if (!vision.canSeePlayer && !vision.overrideDetection) {
            Debug.Log($"<color=red>[CHASE FAILED]</color> Lost track of player.");
            return Node.Status.Failure;
        }

        if (!blackboard.TryGetValue(playerLastPositionKey, out Vector3 targetPosition)) {
            Debug.Log($"<color=red>[ERROR]</color> No valid target position for chase.");
            return Node.Status.Failure;
        }

        lastKnownPosition = targetPosition;
        float distance = Vector3.Distance(enemy.transform.position, lastKnownPosition);
        float trackingRange = vision.trackingRange;

        // Enable chase mode vision
        vision.SetDetectionOverride(true);

        // LOSS CONDITIONS
        if (distance > trackingRange) {
            Debug.Log($"<color=black>[TRACKING RANGE BREACH]</color> {enemy.name} lost the player.");
            vision.SetDetectionOverride(false);
            return Node.Status.Failure;
        }

        if (!agent.hasPath || Vector3.Distance(agent.destination, lastKnownPosition) > 1f) {
            agent.SetDestination(lastKnownPosition);
            //Debug.Log($"<color=yellow>[CHASING]</color> {enemy.name} moving to {lastKnownPosition}");
        }


        return Node.Status.Running;
    }



    public void Reset() {
        vision.SetDetectionOverride(false); // Ensure normal detection resumes after chasing ends
    }
}

public class EngagedHuntStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly NavMeshAgent agent;
    private readonly Blackboard blackboard;
    private readonly BlackboardKey playerLastPositionKey;

    // Store the target position once when the node is activated.
    private Vector3 lastKnownPosition;
    private bool targetSet;

    public EngagedHuntStrategy(MonoBehaviour enemy, NavMeshAgent agent, Blackboard blackboard) {
        this.enemy = enemy;
        this.agent = agent;
        this.blackboard = blackboard;
        this.playerLastPositionKey = this.blackboard.GetOrRegisterKey("PlayerLastPosition");
    }

    public Node.Status Process() {

        // Gate: Only run if the enemy has ever seen the player.
        EnemyVision vision = enemy.GetComponent<EnemyVision>();
        if (!vision.hasEverSeenPlayer) {
            // If the player was never seen, simply fail to trigger hunt/observe.
            return Node.Status.Failure;
        }

        if (enemy is Scout scout) {
            scout.currentState = Scout.AIState.Hunt;
        }

        // On the first tick, read the player's last position once.
        if (!targetSet) {
            if (blackboard.TryGetValue(playerLastPositionKey, out Vector3 target)) {
                lastKnownPosition = target;
                targetSet = true;
                Debug.Log($"[EngagedHunt] {enemy.name} acquired last known position: {lastKnownPosition}");
                agent.SetDestination(lastKnownPosition);
            } else {
                return Node.Status.Failure;
            }
        }

        // Check if we've reached the destination.
        if (agent.remainingDistance <= agent.stoppingDistance) {
            Debug.Log($"[EngagedHunt] {enemy.name} reached the destination: {lastKnownPosition}");
            // Return Failure so that the tree moves on to the Observation branch.
            return Node.Status.Failure;
        }

        return Node.Status.Running;
    }

    public void Reset() {
        lastKnownPosition = default;
        targetSet = false;
    }
}

public class ObservationStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly float observationTime = 2f;         // Wait time between rotations
    private float startTime = 0f;                          // When the current wait period started
    private int rotationCount = 0;
    private const int maxRotations = 3;
    private const float rotationAngle = 120f;              // Total angle to rotate each time
    private readonly float rotationSpeed = 120f;           // Degrees per second

    // For smooth rotation tracking:
    private Quaternion targetRotation;
    private bool isRotating = false;

    public ObservationStrategy(MonoBehaviour enemy) {
        this.enemy = enemy;
    }

    public Node.Status Process() {
        // Let the scout know we're in observation mode.
        if (enemy is Scout scout) {
            scout.currentState = Scout.AIState.Observe;
        }

        // If we’re not currently rotating, wait until observationTime has elapsed.
        if (!isRotating) {
            if (startTime == 0f)
                startTime = Time.time;

            float elapsed = Time.time - startTime;
            if (elapsed >= observationTime) {
                if (rotationCount >= maxRotations) {
                    Debug.Log($"<color=green>[GIVING UP]</color> {enemy.name} finished observing and is returning to patrol.");
                    return Node.Status.Failure;
                }
                // Set the target rotation by adding the desired angle.
                targetRotation = enemy.transform.rotation * Quaternion.Euler(0, rotationAngle, 0);
                isRotating = true;
            }
        }
        // Smoothly rotate towards the target.
        if (isRotating) {
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            // Check if the rotation is almost complete.
            if (Quaternion.Angle(enemy.transform.rotation, targetRotation) < 0.1f) {
                enemy.transform.rotation = targetRotation;
                isRotating = false;
                rotationCount++;
                Debug.Log($"<color=yellow>[ROTATING]</color> {enemy.name} rotated {rotationCount} times.");
                startTime = Time.time; // Restart waiting period after rotation.
            }
        }
        return Node.Status.Running;
    }

    public void Reset() {
        startTime = 0f;
        rotationCount = 0;
        isRotating = false;
    }
}









