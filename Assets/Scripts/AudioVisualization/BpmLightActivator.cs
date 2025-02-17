using System.Collections;
using UnityEngine;

public class BpmLightOscillator : MonoBehaviour
{
    public Light[] lightsToOscillate;
    public float maxIntensity = 5f;
    public float minIntensity = 0f;
    public float holdTimeOffset = 0.05f;  // Time offset to hold intensity before the downbeat

    public bool halfTime = false;      // If true, lights blink at half the BPM speed

    private bool isAtMaxIntensity;
    private float beatInterval;        // Time interval between beats
    private bool isChangingIntensity;
    private int beatCounter;

    void Start()
    {
        isAtMaxIntensity = false;
        isChangingIntensity = false;
        beatCounter = 0;

        SetLightsIntensity(minIntensity);

        // Subscribe to the OnBeat event
        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnBeat.AddListener(OnBeat);
            beatInterval = RhythmManager.Instance.GetBeatInterval();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the OnBeat event
        if (RhythmManager.Instance != null)
            RhythmManager.Instance.OnBeat.RemoveListener(OnBeat);
    }

    public void OnBeat()
    {
        beatCounter++;

        if (RhythmManager.Instance != null)
        {
            beatInterval = RhythmManager.Instance.GetBeatInterval();
        }

        if (halfTime)
        {
            // Trigger every other beat
            if (beatCounter % 2 == 0)
            {
                ToggleLightIntensity();
            }
        }
        else
        {
            // Trigger every beat
            ToggleLightIntensity();
        }
    }

    private void ToggleLightIntensity()
    {
        isAtMaxIntensity = !isAtMaxIntensity;

        if (!isChangingIntensity)
        {
            float duration = (halfTime ? beatInterval * 2 : beatInterval) - holdTimeOffset;
            StartCoroutine(ChangeIntensity(isAtMaxIntensity ? maxIntensity : minIntensity, duration));
        }
    }

    IEnumerator ChangeIntensity(float targetIntensity, float duration)
    {
        isChangingIntensity = true;
        float initialIntensity = lightsToOscillate[0].intensity;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed >= duration - holdTimeOffset)
            {
                SetLightsIntensity(targetIntensity);
                yield return new WaitForSeconds(holdTimeOffset);
                break;
            }

            float newIntensity = Mathf.Lerp(initialIntensity, targetIntensity, timeElapsed / (duration - holdTimeOffset));

            SetLightsIntensity(newIntensity);
            yield return null;
        }

        isChangingIntensity = false;
    }

    void SetLightsIntensity(float intensity)
    {
        foreach (Light light in lightsToOscillate)
        {
            light.intensity = intensity;
        }
    }
}
