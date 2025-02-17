using UnityEngine;
using UnityEngine.EventSystems;

public class BeatMarkerMover : MonoBehaviour, IDragHandler
{
    private RectTransform rectTransform;
    private RectTransform waveformRect;
    private BeatManager beatManager;
    private int markerIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(RectTransform waveformRect)
    {
        this.waveformRect = waveformRect;
    }

    public void Initialize(RectTransform waveformRect, BeatManager beatManager, int markerIndex)
    {
        this.waveformRect = waveformRect;
        this.beatManager = beatManager;
        this.markerIndex = markerIndex;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (beatManager != null)
        {
            float previousMarkerX = markerIndex > 0 ? beatManager.markerPositions[markerIndex - 1] : 0;
            float nextMarkerX = markerIndex < beatManager.markerPositions.Count - 1 ? beatManager.markerPositions[markerIndex + 1] : waveformRect.rect.width;

            float newX = rectTransform.anchoredPosition.x + eventData.delta.x;

            newX = Mathf.Clamp(newX, previousMarkerX + rectTransform.rect.width, nextMarkerX - rectTransform.rect.width);

            rectTransform.anchoredPosition = new Vector2(newX, 0);

            beatManager.UpdateMarkerPosition(markerIndex, newX);
        }
        else
        {
            rectTransform.anchoredPosition += new Vector2(eventData.delta.x, 0);
            rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(rectTransform.anchoredPosition.x, 0, waveformRect.rect.width),
                0);
        }
    }

    public float GetBeatTime(float waveformWidth, float clipLength)
    {
        return rectTransform.anchoredPosition.x / waveformWidth * clipLength;
    }
}
