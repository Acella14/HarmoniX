using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Light))]
public class LightOnAudio : MonoBehaviour
{
    public int band;
    public float minIntesity, maxInstensity;
    new Light light;

    void Start()
    {
        light = GetComponent<Light>();
    }

    void Update()
    {
        light.intensity = (AudioPeer.audioBandBuffer[band] * (maxInstensity - minIntesity)) + minIntesity;
    }
}
