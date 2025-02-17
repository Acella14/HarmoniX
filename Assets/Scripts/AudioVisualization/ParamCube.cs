using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParamCube : MonoBehaviour
{
    public int band;
    public float startScale, scaleMultiplier;
    public bool useBuffer;
    public bool manipulateEmission = true;
    public float minIntensity = 0f;
    public float maxIntensity = 2f;
    Material material;
    Color initialColor;

    void Start()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length > 0)
        {
            material = meshRenderers[0].material;
            initialColor = material.GetColor("_EmissionColor");
            Debug.Log("Initial Emission Color: " + initialColor);
        }
    }

    void Update()
    {
        float audioValue = useBuffer ? AudioPeer.bandBuffer[band] : AudioPeer.audioBand[band];
        float scaleValue = (audioValue * scaleMultiplier) + startScale;
        transform.localScale = new Vector3(transform.localScale.x, scaleValue, transform.localScale.z);

        if (manipulateEmission)
        {
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, audioValue);
            Color emissionColor = initialColor * intensity;
            material.SetColor("_EmissionColor", emissionColor);
        }
    }
}
