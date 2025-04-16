using UnityEngine;
using BlackboardSystem;
using UnityServiceLocator;

public class KnockbackStrikeStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly float attackRange;
    private readonly int damage;
    private readonly float knockbackForce;
    private readonly float windupTime;

    private readonly Blackboard blackboard;
    private readonly BlackboardKey playerLastPositionKey;
    private readonly Transform enemyTransform;
    private readonly EnemyVision vision;

    private bool isAttacking;
    private float attackTimer;
    private bool hasDealtDamage;
    private Vector3 playerPosition;

    public KnockbackStrikeStrategy(
        MonoBehaviour enemy,
        float attackRange,
        int damage,
        float knockbackForce,
        float windupTime = 0.7f
    ) {
        this.enemy = enemy;
        this.attackRange = attackRange;
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.windupTime = windupTime;

        this.enemyTransform = enemy.transform;
        this.vision = enemy.GetComponent<EnemyVision>();

        this.blackboard = ServiceLocator.For(enemy).Get<BlackboardController>().GetBlackboard();
        this.playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
    }

    public Node.Status Process() {

        if (!blackboard.TryGetValue(playerLastPositionKey, out playerPosition)) {
            return Node.Status.Failure;
        }

        float dist = Vector3.Distance(enemyTransform.position, playerPosition);
        if (dist > attackRange + 0.5f) {
            return Node.Status.Failure; // Out of range
        }

        // === Begin Windup ===
        if (!isAttacking) {
            isAttacking = true;
            attackTimer = 0f;
            hasDealtDamage = false;

            if (enemy.TryGetComponent<Animator>(out var anim)) {
                anim.SetTrigger("Backhand"); // <- customize if needed
            }
        }

        attackTimer += Time.deltaTime;

        // Face player
        Vector3 toPlayer = (playerPosition - enemyTransform.position).normalized;
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude > 0.01f) {
            enemyTransform.rotation = Quaternion.Slerp(
                enemyTransform.rotation,
                Quaternion.LookRotation(toPlayer),
                Time.deltaTime * 10f
            );
        }

        // Apply damage and knockback
        if (!hasDealtDamage && attackTimer >= windupTime * 0.6f) {
            Collider[] hits = Physics.OverlapSphere(enemyTransform.position, attackRange, vision.playerLayer);

            foreach (var hit in hits) {
                Vector3 dirToTarget = (hit.transform.position - enemyTransform.position).normalized;

                // Fake knockback movement logic (if you use a controller script)
                if (hit.TryGetComponent(out IKnockbackReceiver knockbackReceiver)) {
                    knockbackReceiver.ApplyKnockback(dirToTarget * knockbackForce);
                }

                if (hit.TryGetComponent(out PlayerHealth health)) {
                    health.TakeDamage(damage);
                }

                Debug.Log($"{enemy.name} hit {hit.name} with knockback strike.");
            }

            hasDealtDamage = true;
        }

        if (attackTimer >= windupTime) {
            Reset();
            (enemy as BassBruiser)?.ResetTree();
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
