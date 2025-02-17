using UnityEngine;
using System.Collections;

public class DamageNumber : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float lifeDuration = 1f;
    public Vector3 randomOffsetRange;
    public Sprite[] numberSprites;
    
    public SpriteRenderer[] digitRenderers;

    public float minScale = 2f;         // Minimum scale for close-up damage numbers
    public float maxScale = 6f;         // Maximum scale for distant damage numbers
    public float maxDistance = 50f;     // Maximum distance to scale up to maxScale

    private Transform playerCamera;
    private Transform playerTransform;

    private void Start()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
            Random.Range(-randomOffsetRange.y, randomOffsetRange.y),
            Random.Range(-randomOffsetRange.z, randomOffsetRange.z)
        );
        transform.position += randomOffset;

        playerCamera = Camera.main.transform;
        playerTransform = Camera.main.transform;

        SetScaleBasedOnDistance();

        StartCoroutine(DestroyAfterTime());
    }

    private void Update()
    {
        // Make the number always face the player
        Vector3 directionToCamera = playerCamera.position - transform.position;
        directionToCamera.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
        transform.rotation = targetRotation;

        // Move the number upwards over time
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }

    public void SetDamageAmount(float damage)
    {
        int damageInt = Mathf.RoundToInt(damage);

        string damageString = damageInt.ToString();

        foreach (SpriteRenderer renderer in digitRenderers)
        {
            renderer.gameObject.SetActive(false);
        }

        for (int i = 0; i < damageString.Length; i++)
        {
            digitRenderers[i].gameObject.SetActive(true);

            int currentDigit = int.Parse(damageString[i].ToString());

            digitRenderers[i].sprite = numberSprites[currentDigit];
        }
    }

    private void SetScaleBasedOnDistance()
    {
        float distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);

        // Map the distance to a scale value between minScale and maxScale
        float t = Mathf.Clamp01(distanceToPlayer / maxDistance);
        float scale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = Vector3.one * scale;
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifeDuration);
        Destroy(gameObject);
    }
}
