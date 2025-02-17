using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CircularProgressBar : MonoBehaviour
{
    private Image radialProgressBar;
    public Color originalColor;
    private Coroutine currentCoroutine;
    private float currentFill = 0f;

    private void Awake()
    {
        radialProgressBar = GetComponent<Image>();
        originalColor = radialProgressBar.color;
    }

    public void UpdateProgressSmooth(float targetProgress, Color color, float duration)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(SmoothFill(targetProgress, color, duration));
    }

    public void FlashRedAndReset(float duration)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(SmoothFlashRedAndReset(duration));
    }

    private IEnumerator SmoothFill(float targetFill, Color color, float duration)
    {
        float startFill = currentFill;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentFill = Mathf.Lerp(startFill, targetFill, elapsedTime / duration);
            radialProgressBar.fillAmount = currentFill;
            radialProgressBar.color = color;
            yield return null;
        }

        currentFill = targetFill;
        radialProgressBar.fillAmount = currentFill;
        radialProgressBar.color = color;
    }

    private IEnumerator SmoothFlashRedAndReset(float duration)
    {
        float startFill = currentFill;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentFill = Mathf.Lerp(startFill, 0f, elapsedTime / duration);
            radialProgressBar.fillAmount = currentFill;
            radialProgressBar.color = Color.red;
            yield return null;
        }

        currentFill = 0f;
        radialProgressBar.fillAmount = 0f;
        radialProgressBar.color = originalColor;
    }

    public void StopCurrentAnimation()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
    }
}
