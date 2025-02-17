using UnityEngine;
using UnityEngine.UI;

public class WaveformVisualizer : MonoBehaviour
{
    public RawImage waveformImage;
    public int visibleWidth = 4096; // Width of the waveform texture
    public int height = 100; // Height of the waveform texture

    private Texture2D waveformTexture;

    float red = 36 / 255.0f;
    float green = 159 / 255.0f;
    float blue = 222 / 255.0f;
    private Color waveformColor;

    void Start() {
        waveformColor = new Color(red, green, blue, 1);
    }
    public void GenerateWaveform(AudioClip clip)
    {
        int totalSamples = clip.samples * clip.channels;
        float[] samples = new float[totalSamples];
        clip.GetData(samples, 0);

        float[] normalizedSamples = NormalizeWaveform(samples);

        waveformTexture = new Texture2D(visibleWidth, height, TextureFormat.RGBA32, false);
        DrawWaveform(waveformTexture, normalizedSamples);

        waveformImage.texture = waveformTexture;
        waveformImage.rectTransform.sizeDelta = new Vector2(visibleWidth, height);
    }

    private float[] NormalizeWaveform(float[] samples)
    {
        float max = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > max) max = Mathf.Abs(samples[i]);
        }

        float[] normalizedSamples = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            normalizedSamples[i] = samples[i] / max;
        }
        return normalizedSamples;
    }

    private void DrawWaveform(Texture2D texture, float[] samples)
    {
        
        Color[] colors = new Color[texture.width * texture.height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        texture.SetPixels(colors);

        int packSize = Mathf.CeilToInt((float)samples.Length / texture.width);
        for (int x = 0; x < texture.width; x++)
        {
            float max = 0f;
            int startSample = x * packSize;
            int endSample = Mathf.Min(startSample + packSize, samples.Length);
            for (int i = startSample; i < endSample; i++)
            {
                if (Mathf.Abs(samples[i]) > max) max = Mathf.Abs(samples[i]);
            }

            int heightPos = (int)(max * (texture.height / 2));
            for (int y = (texture.height / 2) - heightPos; y < (texture.height / 2) + heightPos; y++)
            {
                texture.SetPixel(x, y, waveformColor);
            }
        }

        texture.Apply();
    }
}
