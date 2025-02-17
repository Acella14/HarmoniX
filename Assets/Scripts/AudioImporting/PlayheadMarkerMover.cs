using UnityEngine;
using UnityEngine.EventSystems;

public class PlayheadMarkerMover : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    private RectTransform rectTransform;
    private RectTransform waveformRect;
    private AudioImporter audioImporter;
    private float lerpSpeed = 10f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(RectTransform waveformRect, AudioImporter audioImporter)
    {
        this.waveformRect = waveformRect;
        this.audioImporter = audioImporter;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        audioImporter.PauseAudio();
        audioImporter.SetDraggedMostRecently(false);
        audioImporter.SetIsDraggingWaveform(false);
        audioImporter.SetIgnorePlayhead(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //keep inside visible bounds
        float newXPos = Mathf.Clamp(rectTransform.anchoredPosition.x + eventData.delta.x, audioImporter.GetLeftmostVisiblePoint(), audioImporter.GetRightmostVisiblePoint());

        //additional check to keep inside image outer bounds
        rectTransform.anchoredPosition = new Vector2(
            Mathf.Clamp(newXPos, audioImporter.GetWaveformCroppedLeftPoint(), audioImporter.GetWaveformCroppedRightPoint()),
            0);
    }

    public float GetPlayheadTime(float clipLength)
    {
        float playheadTime = Mathf.Max((rectTransform.anchoredPosition.x / waveformRect.rect.width) * clipLength, 0);
        return playheadTime;
    }

    
    public void UpdatePlayheadPosition(float time, float waveformWidth, float clipLength)
    {
        float targetXPosition = (time / clipLength) * waveformWidth;
        float waveformCroppedRightPoint = audioImporter.GetWaveformCroppedRightPoint();
        float clampedTargetXPosition = Mathf.Min(targetXPosition, waveformCroppedRightPoint);

        float smoothedXPosition = Mathf.Lerp(rectTransform.anchoredPosition.x, clampedTargetXPosition, Time.deltaTime * lerpSpeed);

        if (smoothedXPosition > targetXPosition + 0.01 || rectTransform.anchoredPosition.x > waveformCroppedRightPoint - 0.4) {
            audioImporter.TogglePlayPause();
            rectTransform.anchoredPosition = new Vector2(waveformCroppedRightPoint, 0);
        } else {
            rectTransform.anchoredPosition = new Vector2(smoothedXPosition, 0);
        }
    }


    public Vector2 GetAnchoredPosition()
    {
        return rectTransform.anchoredPosition;
    }

    public void SetAnchoredPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }
}
