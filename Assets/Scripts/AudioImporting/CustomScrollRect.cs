using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomScrollRect : ScrollRect, IScrollHandler, IDragHandler, IPointerClickHandler
{
    public new float decelerationRate = 0.5f;

    public float maxScrollSpeed = 2000f;
    public float minScrollSpeed = -2000f;

    public float maxDragSpeed = 2000f;
    public float minDragSpeed = -2000f;

    private float scrollVelocity;
    private bool isInertiaActive = false;

    public float minX = -8000f;
    public float maxX = 40f;

    private Vector2 lastDragPosition;
    private bool isDragging;

    private AudioImporter audioImporter;

    protected override void Start()
    {
        base.Start();
        
        GameObject audioImporterObject = GameObject.Find("AudioImporter");
        if (audioImporterObject != null)
        {
            audioImporter = audioImporterObject.GetComponent<AudioImporter>();
        }

        if (audioImporter == null)
        {
            Debug.LogError("AudioImporter component not found on the AudioImporter GameObject. Please ensure it is present in the scene.");
        }
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        StopInertia();
    }

    public override void OnScroll(PointerEventData eventData)
    {
        float baseScrollDelta = eventData.scrollDelta.y * (scrollSensitivity * 0.1f);
        float scrollDelta = baseScrollDelta * Mathf.Abs(eventData.scrollDelta.y);

        scrollVelocity += scrollDelta / Time.deltaTime;
        scrollVelocity = Mathf.Clamp(scrollVelocity, minScrollSpeed, maxScrollSpeed);

        float newPositionX = content.anchoredPosition.x + scrollDelta;
        SetClampedPosition(newPositionX);
        isInertiaActive = true;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        StopInertia();
        lastDragPosition = eventData.position;
        isDragging = true;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        Vector2 currentPosition = eventData.position;
        Vector2 delta = currentPosition - lastDragPosition;
        lastDragPosition = currentPosition;

        float newPositionX = content.anchoredPosition.x + delta.x;
        SetClampedPosition(newPositionX);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        Vector2 currentPosition = eventData.position;
        Vector2 dragDelta = currentPosition - lastDragPosition;

        scrollVelocity = dragDelta.x / Time.deltaTime;
        scrollVelocity = Mathf.Clamp(scrollVelocity, minDragSpeed, maxDragSpeed);

        isDragging = false;
        isInertiaActive = true;
    }

    void Update()
    {
        if (isInertiaActive && !isDragging)
        {
            float scrollDelta = scrollVelocity * Time.deltaTime;
            float newPositionX = content.anchoredPosition.x + scrollDelta;

            if (newPositionX <= minX || newPositionX >= maxX)
            {
                StopInertia();
                newPositionX = Mathf.Clamp(newPositionX, minX, maxX);
            }

            SetClampedPosition(newPositionX);

            scrollVelocity *= Mathf.Pow(decelerationRate, Time.deltaTime);

            if (Mathf.Abs(scrollVelocity) <= 0.01f)
            {
                StopInertia();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2)
        {
            Vector2 localMousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out localMousePosition);

            float clickXPosition = localMousePosition.x;

            if (audioImporter != null)
            {
                audioImporter.OnWaveformDoubleClick(clickXPosition);
            }
        }
    }

    private void StartInertia(float velocity)
    {
        scrollVelocity = velocity;
        isInertiaActive = true;
    }

    public void StopInertia()
    {
        scrollVelocity = 0;
        isInertiaActive = false;
    }

    private void SetClampedPosition(float newPositionX)
    {
        newPositionX = Mathf.Clamp(newPositionX, minX, maxX);
        content.anchoredPosition = new Vector2(newPositionX, content.anchoredPosition.y);
    }

    public void UpdateScrollLimits(float min, float max)
    {
        minX = min;
        maxX = max;
    }
}
