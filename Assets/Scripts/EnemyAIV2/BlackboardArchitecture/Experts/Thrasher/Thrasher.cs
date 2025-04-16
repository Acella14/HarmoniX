using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BlackboardSystem;
using UnityServiceLocator;

public class Thrasher : EnemyVision, IExpert {
    private BehaviourTree tree;
    private UnityEngine.AI.NavMeshAgent agent;
    private Animator animator;

    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float turningSpeed = 180f;
    [SerializeField] private ShockwaveEffect shockwaveEffect;

    protected override void Start() {
        base.Start();

        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.angularSpeed = turningSpeed;
        agent.updateRotation = true;

        playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");

        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);

        tree = BuildBehaviorTree();
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

    private BehaviourTree BuildBehaviorTree() {
        Selector root = new Selector("Thrasher_AI");

        // === Line Slam Attack ===
        Sequence slamBranch = new Sequence("LineSlamAttack");
        slamBranch.AddChild(new Leaf("IsTracking", new Condition(() => overrideDetection)));
        slamBranch.AddChild(new Leaf("LineSlam", new LineSlamStrategy(this, animator, blackboard, shockwaveEffect, 1f, 4f)));

        root.AddChild(slamBranch);

        var tree = new BehaviourTree("ThrasherTree");
        tree.AddChild(root);
        return tree;
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

    private void Update() {
        tree.Process();
    }

    public int GetInsistence(Blackboard blackboard) => 100;

    public void Execute(Blackboard blackboard) {
        //blackboard.SetValue(playerLastPositionKey, transform.position);
    }

    public void ResetTree() => tree?.Reset();
}

