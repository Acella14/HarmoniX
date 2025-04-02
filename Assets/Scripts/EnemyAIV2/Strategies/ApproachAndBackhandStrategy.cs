using UnityEngine;
using UnityEngine.AI;
using BlackboardSystem;
using System.Collections;
using UnityServiceLocator;

public class ApproachAndBackhandStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly NavMeshAgent agent;
    private readonly float attackRange;
    private readonly float damage;
    private readonly LayerMask playerMask;
    private readonly Blackboard blackboard;
    private readonly BlackboardKey playerLastPositionKey;

    private bool isAttacking;
    private float attackTimer;
    private float windUpTime = 0.7f;
    private bool hasDealtDamage;

    private Transform enemyTransform;
    private Vector3 playerPosition;

    public ApproachAndBackhandStrategy(MonoBehaviour enemy, NavMeshAgent agent, float range, float damage, LayerMask playerMask) {
        this.enemy = enemy;
        this.agent = agent;
        this.attackRange = range;
        this.damage = damage;
        this.playerMask = playerMask;
        this.blackboard = ServiceLocator.For(enemy).Get<BlackboardController>().GetBlackboard();
        this.playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
        this.enemyTransform = enemy.transform;
    }

    public Node.Status Process() {
        if (!blackboard.TryGetValue(playerLastPositionKey, out playerPosition)) {
            return Node.Status.Failure;
        }

        float dist = Vector3.Distance(enemyTransform.position, playerPosition);

        // === Phase 1: Move Toward Player ===
        if (!isAttacking && dist > attackRange * 0.75f) {
            agent.isStopped = false;
            agent.SetDestination(playerPosition);
            return Node.Status.Running;
        }

        // === Phase 2: Begin Windup (keep creeping forward) ===
        if (!isAttacking) {
            isAttacking = true;
            attackTimer = 0f;
            hasDealtDamage = false;

            agent.isStopped = true;

            if (enemy.TryGetComponent<Animator>(out var anim)) {
                anim.SetTrigger("Backhand");
            }
        }

        attackTimer += Time.deltaTime;

        // Keep "drifting forward" slightly during windup
        Vector3 toPlayer = (playerPosition - enemyTransform.position).normalized;
        enemyTransform.position += toPlayer * 2f * Time.deltaTime; // slight shove

        // Face the player during windup
        Vector3 lookDir = toPlayer;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) {
            enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        // === Phase 3: Mid-Windup = Lunge + Damage ===
        if (!hasDealtDamage && attackTimer >= windUpTime * 0.6f) {
            // Lunge forward right before hit
            enemyTransform.position += toPlayer * 1.5f; // burst forward

            Collider[] hits = Physics.OverlapSphere(enemyTransform.position, attackRange, playerMask);
            foreach (var hit in hits) {
                if (hit.TryGetComponent<Rigidbody>(out var rb)) {
                    Vector3 forceDir = (hit.transform.position - enemyTransform.position).normalized;
                    rb.AddForce(forceDir * 50f, ForceMode.Impulse);
                }

                Debug.Log("Bass Bruiser backhanded the player!");
                // Apply your damage logic here if you have one
            }

            hasDealtDamage = true;
        }

        // === Phase 4: Cooldown ===
        if (attackTimer >= windUpTime) {
            Reset();
            return Node.Status.Success;
        }

        return Node.Status.Running;
    }

    public void Reset() {
        isAttacking = false;
        hasDealtDamage = false;
        attackTimer = 0f;
    }
}
