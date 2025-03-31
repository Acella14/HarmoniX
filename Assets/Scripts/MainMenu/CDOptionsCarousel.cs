using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CDOptionsCarousel : MonoBehaviour
{
    [System.Serializable]
    public class OptionButton
    {
        public GameObject buttonObject; // The button game object
        [HideInInspector] public CanvasGroup canvasGroup; // For fading
        [HideInInspector] public Vector3 originalScale; // Stores the original scale
        [HideInInspector] public BoxCollider clickCollider;
    }

    public List<OptionButton> optionButtons = new List<OptionButton>(); // All buttons
    public float leftX = -20f;
    public float centerX = 0f;
    public float rightX = 20f;

    public float transitionDuration = 0.5f; // Time for shifting buttons
    public float fadeDuration = 0.3f; // Time for fading buttons
    public float scaledDownFactor = 0.5f; // Scale factor for side buttons
    public float transparentAlpha = 0.5f; // Alpha value for side buttons

    public int centerIndex = 0; // Index of the currently active (center) button
    private bool isShifting = false; // Prevents multiple clicks during transition

    void Start()
    {
        foreach (OptionButton btn in optionButtons)
        {
            if (btn.buttonObject != null)
            {
                btn.originalScale = btn.buttonObject.transform.localScale;
                btn.canvasGroup = btn.buttonObject.GetComponent<CanvasGroup>() ?? btn.buttonObject.AddComponent<CanvasGroup>();

                // Assign or create a BoxCollider
                btn.clickCollider = btn.buttonObject.GetComponent<BoxCollider>();
                if (btn.clickCollider == null)
                    btn.clickCollider = btn.buttonObject.AddComponent<BoxCollider>();

                btn.buttonObject.SetActive(false); // Start disabled
            }
        }

        UpdateButtonPositions();
    }


    void SetButtonPosition(OptionButton btn, float targetX)
    {
        RectTransform rectTransform = btn.buttonObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);
        }
    }

    void UpdateButtonPositions()
    {
        if (optionButtons.Count < 3) return;

        int leftIndex = (centerIndex - 1 + optionButtons.Count) % optionButtons.Count;
        int rightIndex = (centerIndex + 1) % optionButtons.Count;

        for (int i = 0; i < optionButtons.Count; i++)
        {
            OptionButton btn = optionButtons[i];
            Button buttonComponent = btn.buttonObject.GetComponent<Button>();

            RectTransform rectTransform = btn.buttonObject.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            if (i == centerIndex)
            {
                // Center button is clickable, collider disabled
                btn.clickCollider.enabled = false;
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = true; // Enable button
                    buttonComponent.enabled = true; // Ensure it's functional
                }
                StartCoroutine(EnableAndMoveButton(btn, centerX, btn.originalScale, 1f));
            }
            else if (i == leftIndex || i == rightIndex)
            {
                // Side buttons have collider enabled but disable UI Button component
                btn.clickCollider.enabled = true;
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = false; // Disable UI button interaction
                    buttonComponent.enabled = false; // Fully disable button
                }
                float targetX = i == leftIndex ? leftX : rightX;
                StartCoroutine(EnableAndMoveButton(btn, targetX, btn.originalScale * scaledDownFactor, transparentAlpha));
            }
            else
            {
                // Other buttons completely disabled
                btn.clickCollider.enabled = false;
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = false;
                    buttonComponent.enabled = false;
                }
                StartCoroutine(FadeOutAndDisable(btn));
            }
        }
    }


    public void ShiftButtons(int clickedIndex)
    {
        if (isShifting) return;
        isShifting = true;

        int totalButtons = optionButtons.Count;
        int distanceRight = (clickedIndex - centerIndex + totalButtons) % totalButtons;
        int distanceLeft = (centerIndex - clickedIndex + totalButtons) % totalButtons;

        bool shiftRight = distanceRight <= distanceLeft; // Move in the shortest direction
        int steps = Mathf.Min(distanceRight, distanceLeft); // Steps needed for correct shift

        StartCoroutine(ShiftMultipleSteps(shiftRight, steps));
    }

    private IEnumerator ShiftMultipleSteps(bool shiftRight, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            int fadingOutIndex = shiftRight
                ? (centerIndex - 1 + optionButtons.Count) % optionButtons.Count
                : (centerIndex + 1) % optionButtons.Count;

            StartCoroutine(FadeOutAndDisable(optionButtons[fadingOutIndex]));

            centerIndex = shiftRight
                ? (centerIndex + 1) % optionButtons.Count
                : (centerIndex - 1 + optionButtons.Count) % optionButtons.Count;

            UpdateButtonPositions();
            yield return new WaitForSeconds(transitionDuration); // Wait for each shift to complete
        }

        isShifting = false;
    }



    private IEnumerator ShiftRoutine()
    {
        yield return new WaitForSeconds(transitionDuration * 0.5f);
        UpdateButtonPositions();
        yield return new WaitForSeconds(transitionDuration);
        isShifting = false;
    }

    IEnumerator EnableAndMoveButton(OptionButton btn, float targetX, Vector3 targetScale, float targetAlpha)
    {
        if (btn.buttonObject == null) yield break;

        btn.buttonObject.SetActive(true);
        
        RectTransform rectTransform = btn.buttonObject.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);
        Vector3 startScale = btn.buttonObject.transform.localScale;
        float startAlpha = btn.canvasGroup.alpha; // Preserve current alpha to avoid unnecessary fading
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            btn.buttonObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // Fix: Only fade in if it's actually hidden
            if (startAlpha == 0f || targetAlpha == 1f)
            {
                btn.canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        btn.buttonObject.transform.localScale = targetScale;
        btn.canvasGroup.alpha = targetAlpha; // Ensure final alpha is correct
    }


    private IEnumerator FadeOutAndDisable(OptionButton btn)
    {
        if (btn.buttonObject == null || !btn.buttonObject.activeSelf) yield break;

        float startAlpha = btn.canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;
            btn.canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t); // Fully fade out only this button
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        btn.canvasGroup.alpha = 0f;
        btn.buttonObject.SetActive(false);
    }


    public void OnButtonClick(GameObject clickedButton)
    {
        int clickedIndex = optionButtons.FindIndex(btn => btn.buttonObject == clickedButton);

        if (clickedIndex == -1 || clickedIndex == centerIndex) return;

        ShiftButtons(clickedIndex);
    }

}
