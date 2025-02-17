using System.Collections;
using UnityEngine;

public class BpmMover : MonoBehaviour
{
    public Vector3 positionDelta = new Vector3(-0.1f, 0f, 0f);
    public bool rebound = false;
    public float reboundFactor = 0.3f;

    private Vector3 initialPosition;
    private float moveDuration = 0.1f;
    private bool isMoving = false;

    void Start()
    {
        initialPosition = transform.position;

        // Subscribe to the OnBeat event
        RhythmManager.Instance.OnBeat.AddListener(OnBeat);
    }

    void OnDestroy()
    {
        // Unsubscribe from the event when destroyed
        if (RhythmManager.Instance != null)
            RhythmManager.Instance.OnBeat.RemoveListener(OnBeat);
    }

    public void OnBeat()
    {
        if (!isMoving)
        {
            StartCoroutine(MoveOnBeat());
        }
    }

    IEnumerator MoveOnBeat()
    {
        isMoving = true;

        Vector3 targetPosition = initialPosition + positionDelta;

        yield return StartCoroutine(SmoothMove(transform.position, targetPosition, moveDuration));

        yield return StartCoroutine(SmoothMove(targetPosition, initialPosition, moveDuration));

        if (rebound)
        {
            Vector3 reboundPosition = initialPosition + positionDelta * reboundFactor;

            yield return StartCoroutine(SmoothMove(initialPosition, reboundPosition, moveDuration * 0.5f));

            yield return StartCoroutine(SmoothMove(reboundPosition, initialPosition, moveDuration * 0.5f));
        }

        isMoving = false;
    }

    IEnumerator SmoothMove(Vector3 startPos, Vector3 endPos, float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / duration);
            yield return null;
        }
        transform.position = endPos;
    }
}
