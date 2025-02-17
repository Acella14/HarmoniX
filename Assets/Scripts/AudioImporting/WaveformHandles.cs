using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WaveformHandles : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public RectTransform waveformRect;
    public AudioImporter audioImporter;
    public RectTransform handleGreyMask;
    public RectTransform oppositeEndHandle;

    private RectTransform rectTransform;
    private int greyMaskInitialWidth = 1;

    [SerializeField] private bool isLeftHandle;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        audioImporter.PauseAudio();
        audioImporter.SetDraggedMostRecently(false);
        audioImporter.SetIsDraggingWaveform(false);
        audioImporter.SetIgnorePlayhead(true);
        audioImporter.SetIgnoreHandles(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaX = eventData.delta.x;
        float clampedDeltaX = ClampHandlePosition(deltaX);
        UpdateGreyMaskSize(clampedDeltaX);
    }

    public void SetHandleAnchoredPosition(Vector2 newPos)
    {
        Vector2 difference = newPos - rectTransform.anchoredPosition;
        float clampedDeltaX = ClampHandlePosition(difference.x);
        UpdateGreyMaskSize(clampedDeltaX);
    }

    private float ClampHandlePosition(float deltaX)
    {
        Vector2 originalPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition += new Vector2(deltaX, 0);

        if (isLeftHandle)
        {
            float rightPoint = Mathf.Min(oppositeEndHandle.anchoredPosition.x - (oppositeEndHandle.rect.width * 2) - AudioImporter.requiredHandleSpacingDistance, audioImporter.GetCompleteRightPoint());
            rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(rectTransform.anchoredPosition.x,
                            audioImporter.GetCompleteLeftPoint(),
                            rightPoint),
                0);
        }
        else
        {
            float leftPoint = Mathf.Max(oppositeEndHandle.anchoredPosition.x + (oppositeEndHandle.rect.width * 2) + AudioImporter.requiredHandleSpacingDistance, audioImporter.GetCompleteLeftPoint());
            rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(rectTransform.anchoredPosition.x,
                            leftPoint,
                            audioImporter.GetCompleteRightPoint()),
                0);
        }

        return rectTransform.anchoredPosition.x - originalPosition.x;
    }

    private void UpdateGreyMaskSize(float clampedDeltaX)
    {
        if (isLeftHandle)
        {
            handleGreyMask.sizeDelta = new Vector2(handleGreyMask.sizeDelta.x + clampedDeltaX, handleGreyMask.sizeDelta.y);
            handleGreyMask.sizeDelta = new Vector2(
                Mathf.Clamp(handleGreyMask.sizeDelta.x,
                            greyMaskInitialWidth,
                            rectTransform.anchoredPosition.x + (rectTransform.rect.width / 2) + greyMaskInitialWidth),
                handleGreyMask.sizeDelta.y);
        }
        else
        {
            handleGreyMask.sizeDelta = new Vector2(handleGreyMask.sizeDelta.x - clampedDeltaX, handleGreyMask.sizeDelta.y);
            handleGreyMask.sizeDelta = new Vector2(
                Mathf.Clamp(handleGreyMask.sizeDelta.x,
                            greyMaskInitialWidth,
                            waveformRect.rect.width - rectTransform.anchoredPosition.x + (rectTransform.rect.width / 2) - greyMaskInitialWidth),
                handleGreyMask.sizeDelta.y);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        audioImporter.UpdateWaveformCroppedPoints();
    }

    public RectTransform GetHandleGreyMask()
    {
        return handleGreyMask;
    }
}
