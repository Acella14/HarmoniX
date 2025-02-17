using System.Collections.Generic;
using UnityEngine;


public class ProjectileStandard : ProjectileBase
{
    [Header("General")]
    public float Radius = 0.01f;
    public Transform Root;
    public Transform Tip;
    public float MaxLifeTime = 5f;
    public GameObject ImpactVfx;
    public float ImpactVfxLifetime = 5f;
    public float ImpactVfxSpawnOffset = 0.1f;
    public AudioClip ImpactSfxClip;
    public LayerMask HittableLayers = -1;

    [Header("Movement")]
    public float Speed = 20f;
    public float GravityDownAcceleration = 0f;
    public float TrajectoryCorrectionDistance = -1;
    public bool InheritWeaponVelocity = false;

    [Header("Damage")]
    public float Damage = 40f;
    public DamageArea AreaOfDamage;

    [Header("Debug")]
    public Color RadiusColor = Color.cyan * 0.2f;

    ProjectileBase m_ProjectileBase;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    bool m_HasTrajectoryOverride;
    float m_ShootTime;
    Vector3 m_TrajectoryCorrectionVector;
    Vector3 m_ConsumedTrajectoryCorrectionVector;
    List<Collider> m_IgnoredColliders;

    const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;

    void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProjectileStandard>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.OnShoot += OnShoot;

        Destroy(gameObject, MaxLifeTime);
    }

    new void OnShoot()
    {
        m_ShootTime = Time.time;
        m_LastRootPosition = Root.position;
        m_IgnoredColliders = new List<Collider>();
        m_ConsumedTrajectoryCorrectionVector = Vector3.zero;

        // Ignore colliders of owner
        Collider[] ownerColliders = m_ProjectileBase.Owner.GetComponentsInChildren<Collider>();
        m_IgnoredColliders.AddRange(ownerColliders);

        // Handle case of player shooting (make projectiles not go through walls, and remember center-of-screen trajectory)
        PlayerWeaponsManager playerWeaponsManager = m_ProjectileBase.Owner.GetComponent<PlayerWeaponsManager>();
        if (playerWeaponsManager)
        {
            m_HasTrajectoryOverride = true;

            // Perform a raycast from the camera to the center of the screen
            Camera weaponCamera = playerWeaponsManager.WeaponCamera;
            Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Check for hits
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, HittableLayers, k_TriggerInteraction))
            {
                // Adjust velocity to aim at the hit point
                Vector3 aimDirection = (hit.point - Root.position).normalized;
                m_Velocity = aimDirection * Speed;

                // Perform initial correction
                if (TrajectoryCorrectionDistance == 0)
                {
                    transform.position += m_TrajectoryCorrectionVector;
                    m_ConsumedTrajectoryCorrectionVector = m_TrajectoryCorrectionVector;
                }
                else if (TrajectoryCorrectionDistance < 0)
                {
                    m_HasTrajectoryOverride = false;
                }
            }
            else
            {
                // If no hit, just shoot forward
                m_Velocity = transform.forward * Speed;
            }
        }
        else
        {
            // If not a player weapon, shoot forward
            m_Velocity = transform.forward * Speed;
        }
    }

    void Update()
    {
        // Move
        transform.position += m_Velocity * Time.deltaTime;
        if (InheritWeaponVelocity)
        {
            transform.position += m_ProjectileBase.InheritedMuzzleVelocity * Time.deltaTime;
        }

        // Drift towards trajectory override
        if (m_HasTrajectoryOverride && m_ConsumedTrajectoryCorrectionVector.sqrMagnitude < m_TrajectoryCorrectionVector.sqrMagnitude)
        {
            Vector3 correctionLeft = m_TrajectoryCorrectionVector - m_ConsumedTrajectoryCorrectionVector;
            float distanceThisFrame = (Root.position - m_LastRootPosition).magnitude;
            Vector3 correctionThisFrame = (distanceThisFrame / TrajectoryCorrectionDistance) * m_TrajectoryCorrectionVector;
            correctionThisFrame = Vector3.ClampMagnitude(correctionThisFrame, correctionLeft.magnitude);
            m_ConsumedTrajectoryCorrectionVector += correctionThisFrame;

            if (m_ConsumedTrajectoryCorrectionVector.sqrMagnitude == m_TrajectoryCorrectionVector.sqrMagnitude)
            {
                m_HasTrajectoryOverride = false;
            }

            transform.position += correctionThisFrame;
        }

        // Orient towards velocity
        transform.forward = m_Velocity.normalized;

        // Gravity
        if (GravityDownAcceleration > 0)
        {
            m_Velocity += Vector3.down * GravityDownAcceleration * Time.deltaTime;
        }

        // Hit detection
        RaycastHit closestHit = new RaycastHit();
        closestHit.distance = Mathf.Infinity;
        bool foundHit = false;

        Vector3 displacementSinceLastFrame = Tip.position - m_LastRootPosition;
        RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, Radius, displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, HittableLayers, k_TriggerInteraction);
        foreach (var hit in hits)
        {
            if (IsHitValid(hit) && hit.distance < closestHit.distance)
            {
                foundHit = true;
                closestHit = hit;
            }
        }

        if (foundHit)
        {
            if (closestHit.distance <= 0f)
            {
                closestHit.point = Root.position;
                closestHit.normal = -transform.forward;
            }

            OnHit(closestHit.point, closestHit.normal, closestHit.collider);
        }

        m_LastRootPosition = Root.position;
    }

    bool IsHitValid(RaycastHit hit)
    {
        if (hit.collider.GetComponent<IgnoreHitDetection>())
        {
            return false;
        }

        if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
        {
            return false;
        }

        if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
        {
            return false;
        }

        return true;
    }

    void OnHit(Vector3 point, Vector3 normal, Collider collider)
    {
        if (AreaOfDamage)
        {
            AreaOfDamage.InflictDamageInArea(Damage, point, HittableLayers, k_TriggerInteraction, m_ProjectileBase.Owner);
        }
        else
        {
            Damageable damageable = collider.GetComponent<Damageable>();
            if (damageable)
            {
                damageable.InflictDamage(Damage, false, m_ProjectileBase.Owner);
            }
        }

        if (ImpactVfx)
        {
            GameObject impactVfxInstance = Instantiate(ImpactVfx, point + (normal * ImpactVfxSpawnOffset), Quaternion.LookRotation(normal));
            if (ImpactVfxLifetime > 0)
            {
                Destroy(impactVfxInstance.gameObject, ImpactVfxLifetime);
            }
        }

        if (ImpactSfxClip)
        {
            AudioUtility.CreateSFX(ImpactSfxClip, point, AudioUtility.AudioGroups.Impact, 1f, 3f);
        }

        Destroy(this.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = RadiusColor;
        Gizmos.DrawSphere(transform.position, Radius);
    }
}


