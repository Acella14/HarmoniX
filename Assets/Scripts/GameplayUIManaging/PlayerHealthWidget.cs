using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthWidget : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI healthText;
    public Image barFillImage;
    public float animationDuration = 0.25f;

    private Coroutine currentAnim;

    private void Start()
    {
        SetHealth(100, 100); // Placeholder test
    }

    public void SetHealth(int current, int max)
    {
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(AnimateHealth(current, max));
    }

    private IEnumerator AnimateHealth(int current, int max)
    {
        float targetFill = current / (float)max;
        float startFill = barFillImage.fillAmount;
        int startValue = int.TryParse(healthText.text, out var parsed) ? parsed : current;

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.deltaTime;
            float p = t / animationDuration;

            barFillImage.fillAmount = Mathf.Lerp(startFill, targetFill, p);
            healthText.text = Mathf.RoundToInt(Mathf.Lerp(startValue, current, p)).ToString();

            yield return null;
        }

        barFillImage.fillAmount = targetFill;
        healthText.text = current.ToString();
    }
}
