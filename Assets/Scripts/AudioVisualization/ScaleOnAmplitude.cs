using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleOnAmplitude : MonoBehaviour
{
    public float startScale, maxScale;
    public bool useBuffer;
    Material material;
    public float red, green, blue;

    void Start()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length > 0)
        {
            material = meshRenderers[0].material;
        }
    }

    void Update()
    {
        if (useBuffer)
        {
            transform.localScale = new Vector3((AudioPeer.amplitudeBuffer * maxScale) + startScale, (AudioPeer.amplitudeBuffer * maxScale) + startScale, (AudioPeer.amplitudeBuffer * maxScale) + startScale);
            Color color = new Color(red * AudioPeer.amplitudeBuffer, green * AudioPeer.amplitudeBuffer, blue * AudioPeer.amplitudeBuffer);
            material.SetColor("_EmissionColor", color);
        }
        else
        {
            transform.localScale = new Vector3((AudioPeer.amplitude * maxScale) + startScale, (AudioPeer.amplitude * maxScale) + startScale, (AudioPeer.amplitude * maxScale) + startScale);
            Color color = new Color(red * AudioPeer.amplitude, green * AudioPeer.amplitude, blue * AudioPeer.amplitude);
            material.SetColor("_EmissionColor", color);
        }
    }
}
