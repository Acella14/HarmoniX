using System.Collections.Generic;
using BlackboardSystem;
using UnityEngine;
using UnityEngine.AI;
using UnityServiceLocator;

public class Scout : EnemyVision, IExpert {

    BlackboardKey isSafeKey;
    bool dangerSensor;
    private BehaviourTree tree;
    private NavMeshAgent agent;
    [SerializeField] private List<Transform> waypoints = new();

    [SerializeField] private float turningSpeed = 720f;

    [SerializeField] private GameObject stateIndicator;
    private Renderer indicatorRenderer;
    private bool lastCanSeePlayer = false;

    public enum AIState {
        Patrol,    // Patrolling and no player nearby.
        Detected,  // Player within detection radius but not yet chased.
        Chase,     // Actively chasing the player.
        Hunt,      // Engaged hunt (player lost after chase).
        Observe    // Observing after hunt fails.
    }
    public AIState currentState = AIState.Patrol;

    
    protected override void Start() {
        base.Start();

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.angularSpeed = turningSpeed;

        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);

        if (stateIndicator != null) {
            indicatorRenderer = stateIndicator.GetComponent<Renderer>();
        } else {
            Debug.LogWarning($"State Indicator not assigned for {gameObject.name}!");
        }

        // === MAIN SELECTOR (Priority Order) ===
        Selector mainSelector = new Selector("MainSelector");

        // 1. CHASE SEQUENCE (Highest Priority: If player is visible, chase)
        Sequence engageBranch = new Sequence("Engage");
        engageBranch.AddChild(new Leaf("Check Visibility", new Condition(() => canSeePlayer))); 
        engageBranch.AddChild(new Leaf("Chase Player", new ChasePlayerStrategy(this, agent, GetChaseSpeed(), GetChaseRange(), blackboard)));
        mainSelector.AddChild(engageBranch);

        // 2. HUNT SEQUENCE (If chase fails, engage hunt)
        Sequence huntBranch = new Sequence("Hunt");
        huntBranch.AddChild(new Leaf("Hunt Last Position", new EngagedHuntStrategy(this, agent, blackboard)));
        Guard huntGuard = new Guard("HuntGuard", () => GetComponent<EnemyVision>().hasEverSeenPlayer, huntBranch);
        mainSelector.AddChild(huntGuard);

        // 3. OBSERVATION SEQUENCE (If hunting fails, observe)
        Sequence observeBranch = new Sequence("Observe");
        observeBranch.AddChild(new Leaf("Observe", new ObservationStrategy(this)));
        Guard observeGuard = new Guard("ObserveGuard", () => GetComponent<EnemyVision>().hasEverSeenPlayer, observeBranch);
        mainSelector.AddChild(observeGuard);

        // 4. PATROL SEQUENCE (Fallback: patrol if no player detected)
        Sequence patrolBranch = new Sequence("Patrol");
        patrolBranch.AddChild(new Leaf("Patrolling", new PatrolStrategy(transform, agent, waypoints, GetPatrolSpeed(), true)));
        mainSelector.AddChild(patrolBranch);

        tree = new BehaviourTree("Scout AI");
        tree.AddChild(mainSelector);
    }



    void Update() {
        if (!lastCanSeePlayer && canSeePlayer) {
            tree.Reset();
        }
        lastCanSeePlayer = canSeePlayer;

        //Debug.Log($"<color=purple>[DEBUG] canSeePlayer = {canSeePlayer}</color>");
        tree.Process();
        UpdateIndicatorColor();
    }



    private void UpdateIndicatorColor() {
        if (indicatorRenderer == null) return;

        switch (currentState) {
            case AIState.Chase:
                indicatorRenderer.material.color = Color.red;
                break;
            case AIState.Hunt:
                // Orange isn’t built in; create one.
                indicatorRenderer.material.color = Color.blue;
                break;
            case AIState.Observe:
                indicatorRenderer.material.color = Color.white;
                break;
            case AIState.Patrol:
                // In Patrol, further differentiate based on player's presence.
                if (IsPlayerWithinDetectionRadius())
                    indicatorRenderer.material.color = Color.yellow;
                else
                    indicatorRenderer.material.color = Color.green;
                break;
        }
    }

    private bool IsPlayerWithinDetectionRadius() {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        return colliders.Length > 0;
    }

    public int GetInsistence(Blackboard blackboard) {
        return dangerSensor ? 100 : 0;
    }

    public void Execute(Blackboard blackboard) {
        blackboard.AddAction(() => {
            if (blackboard.TryGetValue(isSafeKey, out bool isSafe)) {
                blackboard.SetValue(isSafeKey, !isSafe);
            }
        });
    }
}
