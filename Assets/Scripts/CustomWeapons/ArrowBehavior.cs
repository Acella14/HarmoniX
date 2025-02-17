using UnityEngine;
using UnityEngine.UI;

public class ArrowBehavior : MonoBehaviour
{
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float travelDuration;
    private float fadeOutDuration;
    private bool isLeftArrow;
    private Image arrowImage;
    private float elapsedTime = 0f;
    private bool reachedTarget = false;
    private bool isInitialized = false;
    private bool isHalfBeat;

    public AnimationCurve opacityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

  
    public void Initialize(Vector2 startPosition, Vector2 targetPosition, float travelDuration, float fadeOutDuration, bool isLeftArrow, bool isHalfBeat)
    {
        this.startPosition = startPosition;
        this.targetPosition = targetPosition;
        this.travelDuration = travelDuration;
        this.fadeOutDuration = fadeOutDuration;
        this.isLeftArrow = isLeftArrow;
        this.isHalfBeat = isHalfBeat;

        arrowImage = GetComponent<Image>();
        if (arrowImage == null)
        {
            Debug.LogError("ArrowBehavior requires an Image component.");
            Destroy(gameObject);
            return;
        }

        arrowImage.rectTransform.anchoredPosition = startPosition;
        SetOpacity(0f);

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        elapsedTime += Time.deltaTime;

        if (!reachedTarget)
        {
            float t = Mathf.Clamp01(elapsedTime / travelDuration);

            arrowImage.rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

            float adjustedAlpha = opacityCurve.Evaluate(t);
            SetOpacity(adjustedAlpha);

            // Check if the arrow has reached the target
            if (t >= 1f)
            {
                reachedTarget = true;
                elapsedTime = 0f; // Reset elapsed time for fade-out
            }
        }
        else
        {
            float t = 0.0f;
            if (!isHalfBeat) 
            {
                t = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            }
            else 
            {
                t = Mathf.Clamp01(elapsedTime / (fadeOutDuration/2));
            }

            float adjustedAlpha = Mathf.Lerp(1f, 0f, t);
            SetOpacity(adjustedAlpha);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void SetOpacity(float alpha)
    {
        if (arrowImage != null)
        {
            Color color = arrowImage.color;
            color.a = alpha;
            arrowImage.color = color;
        }
    }
}
