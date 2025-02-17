using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [Header("Crosshair Elements")]
    public Image dotImage;
    public Image leftArrow;
    public Image rightArrow;
    public Image halfBeatLeftArrow;
    public Image halfBeatRightArrow;

    [Header("Arrow Prefab")]
    public GameObject arrowPrefab;

    [Header("Arrow Animation Settings")]
    public float arrowScrollDistance = 200f;
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.5f;

    [Header("Half-Beat Settings")]
    public bool includeHalfBeats = true;
    public float halfBeatArrowScale = 0.25f;

    private Vector2 leftArrowTargetPosition;
    private Vector2 rightArrowTargetPosition;
    private Vector2 halfBeatLeftTargetPosition;
    private Vector2 halfBeatRightTargetPosition;
    private Vector2 leftArrowStartPosition;
    private Vector2 rightArrowStartPosition;

    private List<float> beatTimes;
    private List<float> halfBeatTimes;
    private HashSet<int> scheduledBeatIndices;
    private float currentTime;

    private void Start()
    {
        if (!arrowPrefab)
        {
            Debug.LogError("Arrow prefab must be assigned in CrosshairManager.");
            return;
        }

        if (!leftArrow || !rightArrow || !halfBeatLeftArrow || !halfBeatRightArrow)
        {
            Debug.LogError("All crosshair images must be assigned in CrosshairManager.");
            return;
        }

        leftArrowTargetPosition = leftArrow.rectTransform.anchoredPosition;
        rightArrowTargetPosition = rightArrow.rectTransform.anchoredPosition;
        halfBeatLeftTargetPosition = halfBeatLeftArrow.rectTransform.anchoredPosition;
        halfBeatRightTargetPosition = halfBeatRightArrow.rectTransform.anchoredPosition;

        leftArrowStartPosition = leftArrowTargetPosition - new Vector2(arrowScrollDistance, 0f);
        rightArrowStartPosition = rightArrowTargetPosition + new Vector2(arrowScrollDistance, 0f);

        scheduledBeatIndices = new HashSet<int>();

        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnSongChanged.AddListener(OnSongChanged);
            RhythmManager.Instance.OnBeat.AddListener(OnBeat);
            UpdateBeatTimes();
        }
        else
        {
            Debug.LogError("RhythmManager instance not found.");
        }
    }

    private void OnDestroy()
    {
        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnSongChanged.RemoveListener(OnSongChanged);
            RhythmManager.Instance.OnBeat.RemoveListener(OnBeat);
        }
    }

    private void OnSongChanged()
    {
        UpdateBeatTimes();
    }

    private void OnBeat()
    {
        if (RhythmManager.Instance != null)
        {
            float currentTime = RhythmManager.Instance.GetCurrentSongTime();
            if (currentTime < 0.1f) // Small threshold to detect a song loop near the start
            {
                Debug.Log("Song loop detected. Resetting crosshair scheduling.");
                ResetScheduledArrows();
            }
        }
    }

    private void ResetScheduledArrows()
    {
        scheduledBeatIndices.Clear();
        UpdateBeatTimes();
    }

    private void UpdateBeatTimes()
    {
        if (RhythmManager.Instance != null)
        {
            beatTimes = RhythmManager.Instance.GetBeatTimes();
            scheduledBeatIndices.Clear();

            if (includeHalfBeats)
            {
                GenerateHalfBeatTimes();
            }
        }
    }

    private void GenerateHalfBeatTimes()
    {
        halfBeatTimes = new List<float>();

        for (int i = 0; i < beatTimes.Count - 1; i++)
        {
            float halfBeatTime = (beatTimes[i] + beatTimes[i + 1]) / 2f;
            halfBeatTimes.Add(halfBeatTime);
        }
    }

    private void Update()
    {
        if (RhythmManager.Instance != null && beatTimes != null)
        {
            currentTime = RhythmManager.Instance.GetCurrentSongTime();

            // Time window to look ahead for beats (e.g., next 2 beats)
            float lookAheadTime = RhythmManager.Instance.GetBeatInterval() * 2f;

            ScheduleArrows(beatTimes, lookAheadTime, isHalfBeat: false);

            if (includeHalfBeats && halfBeatTimes != null)
            {
                ScheduleArrows(halfBeatTimes, lookAheadTime, isHalfBeat: true);
            }
        }
    }

    private void ScheduleArrows(List<float> times, float lookAheadTime, bool isHalfBeat)
    {
        for (int i = 0; i < times.Count; i++)
        {
            float beatTime = times[i];

            if (beatTime > currentTime && beatTime <= currentTime + lookAheadTime)
            {
                int uniqueIndex = isHalfBeat ? i + 100000 : i;

                if (!scheduledBeatIndices.Contains(uniqueIndex))
                {
                    float timeUntilBeat = beatTime - currentTime;
                    ScheduleArrow(timeUntilBeat, isHalfBeat);
                    scheduledBeatIndices.Add(uniqueIndex);
                }
            }
        }
    }

    public void ScheduleArrow(float timeUntilBeat, bool isHalfBeat)
    {
        GameObject leftArrowInstance = Instantiate(arrowPrefab, transform);
        GameObject rightArrowInstance = Instantiate(arrowPrefab, transform);

        Vector3 rightArrowScale = rightArrowInstance.transform.localScale;
        rightArrowScale.x = -1 * Mathf.Abs(rightArrowScale.x);
        rightArrowInstance.transform.localScale = rightArrowScale;

        Vector2 leftTarget = leftArrowTargetPosition;
        Vector2 rightTarget = rightArrowTargetPosition;

        if (isHalfBeat)
        {
            leftTarget = halfBeatLeftTargetPosition;
            rightTarget = halfBeatRightTargetPosition;

            Vector3 halfBeatScale = leftArrowInstance.transform.localScale * halfBeatArrowScale;
            leftArrowInstance.transform.localScale = halfBeatScale;
            rightArrowInstance.transform.localScale = halfBeatScale;

            rightArrowScale = rightArrowInstance.transform.localScale;
            rightArrowScale.x = -1 * Mathf.Abs(rightArrowScale.x);
            rightArrowInstance.transform.localScale = rightArrowScale;
        }

        ArrowBehavior leftArrowBehavior = leftArrowInstance.GetComponent<ArrowBehavior>();
        ArrowBehavior rightArrowBehavior = rightArrowInstance.GetComponent<ArrowBehavior>();

        if (leftArrowBehavior == null)
        {
            Debug.LogError("Left arrow prefab must have an ArrowBehavior script attached.");
            Destroy(leftArrowInstance);
        }
        else
        {
            leftArrowBehavior.Initialize(leftArrowStartPosition, leftTarget, timeUntilBeat, fadeOutTime, true, isHalfBeat);
        }

        if (rightArrowBehavior == null)
        {
            Debug.LogError("Right arrow prefab must have an ArrowBehavior script attached.");
            Destroy(rightArrowInstance);
        }
        else
        {
            rightArrowBehavior.Initialize(rightArrowStartPosition, rightTarget, timeUntilBeat, fadeOutTime, false, isHalfBeat);
        }
    }
}
