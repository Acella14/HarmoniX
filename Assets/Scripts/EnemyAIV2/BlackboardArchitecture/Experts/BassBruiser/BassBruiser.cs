using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BlackboardSystem;
using UnityServiceLocator;

public class BassBruiser : EnemyVision, IExpert {
    private BehaviourTree tree;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Animator animator;
    private BlackboardKey isSafeKey;

    [Header("AI Settings")]
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float turningSpeed = 180f;
    [SerializeField] private AnimationCurve jumpArcCurve;
    [SerializeField] private float backhandRange = 3f;
    [SerializeField] private int backhandDamage = 10;
    [SerializeField] private float knockbackForce = 20f;

    [Header("Audio")]
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip chargeClip;
    [SerializeField] public AudioClip jumpLaunchClip;

    private bool lastCanSeePlayer;

    protected override void Start() {
        base.Start();

        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        agent.angularSpeed = turningSpeed;
        agent.updateRotation = true;

        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);

        tree = BuildBehaviorTree();
    }

    private BehaviourTree BuildBehaviorTree() {
        Selector rootSelector = new Selector("BassBruiser_MainSelector");

        // 1. Jump if too far to chase
        Sequence jumpBranch = new Sequence("JumpAttack");
        jumpBranch.AddChild(new Leaf("IsTracking", new Condition(() => overrideDetection)));
        jumpBranch.AddChild(new Leaf("TooFarToChase", new Condition(() => !IsPlayerWithinRange(GetChaseRange() - 2f))));
        jumpBranch.AddChild(new Leaf("JumpToPlayer", new JumpAndSlamStrategy(this, animator, blackboard, jumpArcCurve, 3.0f, 5.0f, 0.99f)));
        rootSelector.AddChild(jumpBranch);

        
        // 2. Backhand comes BEFORE chase
        Sequence backhandBranch = new Sequence("BackhandAttack");
        backhandBranch.AddChild(new Leaf("IsTracking", new Condition(() => {
            bool val = overrideDetection;
            Debug.Log($"[BT] IsTracking: {val}");
            return val;
        })));

        backhandBranch.AddChild(new Leaf("CloseEnoughForBackhand", new Condition(() => {
            bool closeEnough = IsPlayerWithinRange(backhandRange);
            Debug.Log($"[BT] CloseEnoughForBackhand: {closeEnough}");
            return closeEnough;
        })));

        backhandBranch.AddChild(new Leaf("KnockbackStrike",
            new KnockbackStrikeStrategy(
                this,
                backhandRange,
                backhandDamage,
                knockbackForce
            )
        ));
        rootSelector.AddChild(backhandBranch);

        // 3. Chase
        Sequence chaseWithInterrupt = new Sequence("ChasePlayer");
        chaseWithInterrupt.AddChild(new Leaf("IsTracking", new Condition(() => overrideDetection)));
        chaseWithInterrupt.AddChild(new Leaf("BackhandInRange", new Condition(() => !IsPlayerWithinRange(backhandRange))));
        chaseWithInterrupt.AddChild(new Leaf("WithinChaseRange", new Condition(() => IsPlayerWithinRange(GetChaseRange() - 2f))));
        chaseWithInterrupt.AddChild(new Leaf("Chase", new ChasePlayerStrategy(this, agent, GetChaseSpeed(), GetChaseRange(), blackboard)));
        rootSelector.AddChild(chaseWithInterrupt);
        

        // 4. Patrol
        Sequence patrolBranch = new Sequence("Patrol");
        patrolBranch.AddChild(new Leaf("Patrol", new PatrolStrategy(transform, agent, patrolPoints, GetPatrolSpeed(), true)));
        rootSelector.AddChild(patrolBranch);

        var tree = new BehaviourTree("BassBruiser_AI");
        tree.AddChild(rootSelector);
        return tree;
    }

    private void Update() {
        if (!lastCanSeePlayer && canSeePlayer) {
            tree.Reset(); // reset tree if visibility changed
        }
        lastCanSeePlayer = canSeePlayer;

        tree.Process();
    }

    private void LateUpdate() {
        if (overrideDetection && blackboard.TryGetValue(playerLastPositionKey, out Vector3 playerPos)) {
            Vector3 toPlayer = playerPos - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f) {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }


    protected override void DetectPlayer() {
        if (overrideDetection) {
            if (blackboard.TryGetValue(playerLastPositionKey, out Vector3 playerPos)) {
                float dist = Vector3.Distance(transform.position, playerPos);
                if (dist > trackingRange) {
                    overrideDetection = false;
                    canSeePlayer = false;
                    OnPlayerLost();
                } else {
                    canSeePlayer = true; // Even if occluded
                }
            }
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        foreach (Collider col in colliders) {
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, col.transform.position);
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (!overrideDetection && angle < fovAngle * 0.5f &&
                !Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleLayer)) {
                canSeePlayer = true;
                OnPlayerDetected(col.transform.position);
                overrideDetection = true;
                return;
            }
        }

        canSeePlayer = false;
        OnPlayerLost();
    }


    public int GetInsistence(Blackboard blackboard) => 100;

    public void Execute(Blackboard blackboard) {
    }

    private bool IsPlayerWithinRange(float range, string debugLabel = null) {
        if (!blackboard.TryGetValue(playerLastPositionKey, out Vector3 playerPos)) {
            if (debugLabel != null)
                Debug.LogWarning($"[BT:{debugLabel}] PlayerLastPosition missing!");
            return false;
        }

        float actualDist = Vector3.Distance(transform.position, playerPos);

        if (debugLabel != null)
            Debug.Log($"[BT:{debugLabel}] Distance to player: {actualDist:F2} vs. needed {range:F2}");

        return actualDist <= range;
    }


    public void ResetTree() => tree?.Reset();

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

        // Chase Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Backhand Range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, backhandRange);
    }

}
