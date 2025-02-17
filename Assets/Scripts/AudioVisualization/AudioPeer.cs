using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPeer : MonoBehaviour
{
    AudioSource audioSource;
    public static float[] samples = new float[512];
    public static float[] freqBand = new float[8];
    public static float[] bandBuffer = new float[8];
    float[] bufferDecrease = new float[8];

    float[] freqBandHighest = new float[8];
    public static float[] audioBand = new float[8];
    public static float[] audioBandBuffer = new float[8];

    public float bufferDecreaseStart = 0.005f;
    public float bufferDecreaseMultiplier = 1.2f;
    public float smoothingSpeed = 0.5f;

    public static float amplitude, amplitudeBuffer;
    private float amplitudeHighest;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        GetSpectrumAudioSource();
        MakeFrequencyBands();
        BandBuffer();
        CreateAudioBands();
        GetAmplitude();
    }

    void GetAmplitude()
    {
        float currentAmplitude = 0;
        float currentAmplitudeBuffer = 0;

        for (int i = 0; i < 8;  i++)
        {
            currentAmplitude += audioBand[i];
            currentAmplitudeBuffer += audioBandBuffer[i];
        }
        if (currentAmplitude > amplitudeHighest)
        {
            amplitudeHighest = currentAmplitude;
        }
        amplitude = currentAmplitude / amplitudeHighest;
        amplitudeBuffer = currentAmplitudeBuffer / amplitudeHighest;
    }

    void CreateAudioBands()
    {
        for (int i = 0; i < 8; i++)
        {
            if (freqBand[i] > freqBandHighest[i])
            {
                freqBandHighest[i] = freqBand[i];
            }
            audioBand[i] = (freqBand[i] / freqBandHighest[i]);
            audioBandBuffer[i] = (bandBuffer[i] / freqBandHighest[i]);
        }
    }

    void GetSpectrumAudioSource()
    {
        audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
    }

    void BandBuffer()
    {
        for (int g = 0; g < 8; g++)
        {
            if (freqBand[g] > bandBuffer[g])
            {
                bandBuffer[g] = freqBand[g];
                bufferDecrease[g] = bufferDecreaseStart;
            }
            else if (freqBand[g] < bandBuffer[g])
            {
                bandBuffer[g] = Mathf.Lerp(bandBuffer[g], freqBand[g], Time.deltaTime * smoothingSpeed);
                bufferDecrease[g] *= bufferDecreaseMultiplier;
                bandBuffer[g] -= bufferDecrease[g] * Time.deltaTime;

                if (bandBuffer[g] < 0)
                {
                    bandBuffer[g] = 0;
                }
            }
        }
    }

    void MakeFrequencyBands()
    {
        // custom sample counts for each frequency band
        int[] customSampleCounts = new int[8] { 20, 30, 40, 80, 100, 100, 80, 60 };
        //{ 10, 20, 40, 80, 160, 40, 80, 80 }

        int count = 0;

        for (int i = 0; i < 8; i++)
        {
            float average = 0;
            int sampleCount = customSampleCounts[i];

            for (int j = 0; j < sampleCount && count < samples.Length; j++)
            {
                average += samples[count] * (count + 1);
                count++;
            }

            if (count > 0)
            {
                average /= count;
            }

            freqBand[i] = average * 10;
        }
    }

    /*
     * 30 hz per sample
     * 
     * 0 - 2 = 60 hz
     * 1 - 4 = 120 hz - 61:180
     * 2 - 8 = 240 hz - 181:420
     * 3 - 16 = 480 hz - 421:900
     * 4 - 32 = 960 hz - 901:1860
     * 5 - 64 = 1920 hz - 1861:3780
     * 6 - 128 = 3840 hz - 3781:7620
     * 7 - 256 = 7680 hz - 7621:15300
     * 510 total samples
     */
}
