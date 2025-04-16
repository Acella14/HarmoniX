using UnityEngine;
using BlackboardSystem;
using System.Collections;

public class LineSlamStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly Animator animator;
    private readonly Blackboard blackboard;
    private readonly BlackboardKey playerGroundPointKey;
    private readonly ShockwaveEffect shockwaveEffect;

    private bool isSlamming = false;
    private bool slamDone = false;
    private bool isOnCooldown = false;
    private float timer = 0f;
    private float windupTime = 1f;
    private float cooldownTime = 2f;

    private EnemyVision vision;

    public LineSlamStrategy(MonoBehaviour enemy, Animator animator, Blackboard blackboard, ShockwaveEffect shockwaveEffect, float windupTime = 1f, float cooldownTime = 2f) {
        this.enemy = enemy;
        this.animator = animator;
        this.blackboard = blackboard;
        this.shockwaveEffect = shockwaveEffect;
        this.playerGroundPointKey = blackboard.GetOrRegisterKey("PlayerGroundPoint");
        this.windupTime = windupTime;
        this.cooldownTime = cooldownTime;

        this.vision = enemy.GetComponent<EnemyVision>();
    }

    public Node.Status Process() {
        if (isOnCooldown) return Node.Status.Failure;

        if (slamDone) {
            StartCooldown();
            return Node.Status.Success;
        }

        if (!isSlamming) {
            isSlamming = true;
            timer = 0f;
            animator.SetTrigger("Charge");
            return Node.Status.Running;
        }

        timer += Time.deltaTime;

        if (timer < windupTime) {
            return Node.Status.Running;
        }

        PerformSlam();
        slamDone = true;
        return Node.Status.Running;
    }

    private void PerformSlam() {
        Vector3 slamOrigin = enemy.transform.position;

        if (Physics.Raycast(slamOrigin + Vector3.up, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground"))) {
            slamOrigin = hit.point;
        }

        if (!blackboard.TryGetValue(playerGroundPointKey, out Vector3 target)) {
            return;
        }

        if (shockwaveEffect?.shockwaveSettings is LineShockwaveSettings baseSettings) {
            LineShockwaveSettings clonedSettings = new LineShockwaveSettings {
                duration = baseSettings.duration,
                strength = baseSettings.strength,
                width = baseSettings.width,
                emissionColorStart = baseSettings.emissionColorStart,
                emissionColorEnd = baseSettings.emissionColorEnd,
                target = target
            };

            shockwaveEffect.TriggerShockwave(slamOrigin, clonedSettings);
        }

        // Maintain tracking like in JumpAndSlamStrategy
        if (vision != null && blackboard.TryGetValue(playerGroundPointKey, out Vector3 currentPlayerPos)) {
            float dist = Vector3.Distance(enemy.transform.position, currentPlayerPos);
            vision.SetDetectionOverride(dist <= vision.trackingRange);
        }

        animator.SetTrigger("Slam");
    }

    private void StartCooldown() {
        isOnCooldown = true;
        enemy.StartCoroutine(CooldownCoroutine());
    }

    private IEnumerator CooldownCoroutine() {
        float elapsed = 0f;
        while (elapsed < cooldownTime) {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset state so the strategy can run again
        isOnCooldown = false;
        isSlamming = false;
        slamDone = false;
        timer = 0f;

        // Optional: reset behavior tree if needed
        enemy.GetComponent<Thrasher>()?.ResetTree();
    }

    public void Reset() {
        // Prevent interruption unless desired
    }
}
