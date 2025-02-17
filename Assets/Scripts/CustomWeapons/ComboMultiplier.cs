using System.Collections;
using UnityEngine;
using TMPro;

public class ComboMultiplier : MonoBehaviour
{
    [Header("Combo Settings")]
    public TMP_Text comboText;
    public float comboResetTime = 10f;
    public int maxComboLevel = 10;
    public int shotsPerComboLevel = 4;

    [Header("Progress Bar Settings")]
    public CircularProgressBar progressBar;
    public float progressAnimationDuration = 0.2f;
    public float redFlashDuration = 0.3f;

    [Header("Crit Ability Settings")]
    public int maxCritStacks = 3;
    public int critCostPerStack = 1;
    [HideInInspector]
    public bool IsCriticalShot { get; private set; }

    private int currentComboLevel = 0;
    private int currentStreak = 0;
    private int shotsInCurrentCombo = 0;
    private int critMultiplierStack = 0;
    private Coroutine comboResetCoroutine;

    private void Start()
    {
        UpdateComboText();
        progressBar.UpdateProgressSmooth(0f, progressBar.originalColor, 0f);
    }

    public void RegisterSuccessfulShot()
    {
        if (RhythmManager.Instance != null && RhythmManager.Instance.IsOnBeatNow())
        {
            currentStreak++;

            if (currentComboLevel < maxComboLevel)
            {
                shotsInCurrentCombo++;

                float targetProgress = (float)shotsInCurrentCombo / shotsPerComboLevel;
                progressBar.UpdateProgressSmooth(targetProgress, progressBar.originalColor, progressAnimationDuration);

                if (shotsInCurrentCombo >= shotsPerComboLevel)
                {
                    currentComboLevel++;
                    shotsInCurrentCombo = 0;
                    progressBar.UpdateProgressSmooth(0f, progressBar.originalColor, 0f);
                }

                IsCriticalShot = false;
            }

            UpdateComboText();

            if (comboResetCoroutine != null)
            {
                StopCoroutine(comboResetCoroutine);
            }
        }
        else
        {
            RegisterMissedShot();
        }
    }

    public void RegisterMissedShot()
    {
        currentComboLevel = 0;
        currentStreak = 0;
        shotsInCurrentCombo = 0;
        critMultiplierStack = 0; // Reset crit stacks

        progressBar.FlashRedAndReset(redFlashDuration);
        UpdateComboText();

        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }
        comboResetCoroutine = StartCoroutine(ComboResetTimer());
    }

    public bool UseCritAbility()
    {
        if (critMultiplierStack < maxCritStacks && currentComboLevel >= critCostPerStack)
        {
            critMultiplierStack++;
            currentComboLevel -= critCostPerStack;
            UpdateComboText();
            return true;
        }

        return false; // Not enough combo levels or max stacks reached
    }

    public int ConsumeCritMultiplier()
    {
        int stacks = critMultiplierStack;
        critMultiplierStack = 0; // Reset stacks after consumption
        return stacks;
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo();
    }

    private void ResetCombo()
    {
        currentComboLevel = 0;
        currentStreak = 0;
        shotsInCurrentCombo = 0;
        critMultiplierStack = 0;
        progressBar.UpdateProgressSmooth(0f, progressBar.originalColor, 0f);
        UpdateComboText();
    }

    private void UpdateComboText()
    {
        comboText.text = $"{currentComboLevel}x";
    }
}