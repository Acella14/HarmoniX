using UnityEngine;
using UnityEngine.AI;
using BlackboardSystem;
using System.Collections;

public class JumpAndSlamStrategy : IStrategy {
    private readonly MonoBehaviour enemy;
    private readonly Animator animator;
    private readonly Blackboard blackboard;
    private readonly AnimationCurve jumpCurve;
    private readonly float jumpDuration;
    private readonly float slamCooldown;
    private readonly float jumpWindupTime;
    private readonly BlackboardKey playerLastPositionKey;
    private readonly float targetLockProgress;


    private bool jumpStarted = false;
    private bool slamTriggered = false;
    private readonly ShockwaveEffect shockwave;
    private bool isOnCooldown = false;
    private bool windupDone = false;
    private float timer = 0f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 currentTargetMidAir;
    private NavMeshAgent agent;
    private EnemyVision vision;
    private Transform enemyTransform;

    public JumpAndSlamStrategy(MonoBehaviour enemy, Animator animator, Blackboard blackboard, AnimationCurve jumpCurve, float jumpDuration = 2f, float slamCooldown = 3f, float jumpWindupTime = 1f, float targetLockProgress = 0.95f) {
        this.enemy = enemy;
        this.animator = animator;
        this.blackboard = blackboard;
        this.jumpCurve = jumpCurve;
        this.jumpDuration = jumpDuration;
        this.slamCooldown = slamCooldown;
        this.jumpWindupTime = jumpWindupTime;
        this.targetLockProgress = Mathf.Clamp01(targetLockProgress);

        this.playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
        this.agent = enemy.GetComponent<NavMeshAgent>();
        this.vision = enemy.GetComponent<EnemyVision>();
        this.enemyTransform = enemy.transform;

        this.shockwave = enemy.GetComponent<ShockwaveEffect>();
    }

    public Node.Status Process() {
        if (isOnCooldown)
            return Node.Status.Failure;

        if (!jumpStarted) {
            if (!blackboard.TryGetValue(playerLastPositionKey, out targetPosition)) {
                return Node.Status.Failure;
            }

            startPosition = enemyTransform.position;
            currentTargetMidAir = targetPosition;
            timer = 0f;
            jumpStarted = true;
            slamTriggered = false;
            windupDone = false;

            animator.SetTrigger("Charge");
            if (enemy is BassBruiser bruiser && bruiser.chargeClip != null) {
                bruiser.audioSource.PlayOneShot(bruiser.chargeClip, 1f);
            }

            return Node.Status.Running;
        }

        // === WINDUP PHASE ===
        if (!windupDone) {
            timer += Time.deltaTime;
            if (timer >= jumpWindupTime) {
                timer = 0f;
                windupDone = true;
                if (enemy is BassBruiser bruiser && bruiser.jumpLaunchClip != null) {
                    bruiser.audioSource.PlayOneShot(bruiser.jumpLaunchClip, 0.3f);
                }
            }
            agent.enabled = false;
            return Node.Status.Running;
        }

        // === JUMP PHASE ===
        if (jumpStarted && !slamTriggered) {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;

            if (progress >= 1f) {
                CompleteSlam();
                return Node.Status.Success;
            }

            if (progress <= targetLockProgress) {
                if (blackboard.TryGetValue(playerLastPositionKey, out Vector3 updatedPlayerPos)) {
                    currentTargetMidAir = Vector3.Lerp(currentTargetMidAir, updatedPlayerPos, Time.deltaTime * 1.5f);
                }
            }

            float height = jumpCurve.Evaluate(progress);
            Vector3 horizontalPos = Vector3.Lerp(startPosition, currentTargetMidAir, progress);
            Vector3 newPos = horizontalPos + Vector3.up * height;
            enemyTransform.position = newPos;

            return Node.Status.Running;
        }

        return Node.Status.Success;
    }

    private void CompleteSlam() {
        slamTriggered = true;
        animator.SetTrigger("Slam");

        agent.enabled = true;
        agent.Warp(enemyTransform.position);

        // PHYSICS: Launch player if in range BEFORE visual effect
        if (shockwave != null) {
            shockwave.LaunchNearby();
            shockwave.TriggerShockwave(enemyTransform.position, shockwave.shockwaveSettings);
        }

        // Maintain tracking after slam
        if (blackboard.TryGetValue(playerLastPositionKey, out Vector3 currentPlayerPos)) {
            float dist = Vector3.Distance(enemyTransform.position, currentPlayerPos);
            vision.SetDetectionOverride(dist <= vision.trackingRange);
        }

        enemy.StartCoroutine(CooldownCoroutine());
    }


    private IEnumerator CooldownCoroutine() {
        isOnCooldown = true;

        float cooldownTime = slamCooldown;
        float elapsed = 0f;

        while (elapsed < cooldownTime) {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isOnCooldown = false;
        jumpStarted = false;
        slamTriggered = false;
        windupDone = false;
        timer = 0f;

        enemy.GetComponent<BassBruiser>()?.ResetTree();
    }

    public void Reset() {
        // Don't allow Reset to interrupt jump or cooldown
    }
}

