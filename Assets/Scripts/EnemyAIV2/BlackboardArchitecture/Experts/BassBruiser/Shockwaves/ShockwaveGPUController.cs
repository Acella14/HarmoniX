using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ShockwaveGPUController : MonoBehaviour {
    public Material shockwaveMaterial;
    public int maxEffects = 32;
    public AudioSource sfxSource;

    private ComputeBuffer effectBuffer;
    private ComputeBuffer lineDataBuffer;
    private ComputeBuffer radialDataBuffer;

    private List<ActiveShockwave> activeEffects = new();
    private List<LineShockwaveData> activeLineData = new();
    private List<RadialShockwaveData> radialShockwaveData = new();

    [StructLayout(LayoutKind.Sequential)]
    public struct ShockwaveEffectData {
        public Vector3 origin;
        public float time;

        public float strength;
        public int type;
        public float emissionStrength;
        public float padding0;

        public Vector4 emissionColorStart;
        public Vector4 emissionColorEnd;

        public Vector4 padding1; // <-- Now total size = 96 bytes
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct RadialShockwaveData {
        public float maxRadius;
        public float thickness;
        public float clearRadius;
        public float padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LineShockwaveData {
        public Vector3 endPoint;
        public float width;
    }



    class ActiveShockwave {
        public ShockwaveEffectData data;
        public float timer = 0f;
        public float duration;
        public float originalStrength;
    }

    void OnEnable() {
        effectBuffer = new ComputeBuffer(maxEffects, Marshal.SizeOf<ShockwaveEffectData>(), ComputeBufferType.Structured);
        lineDataBuffer = new ComputeBuffer(maxEffects, Marshal.SizeOf(typeof(LineShockwaveData)));
        radialDataBuffer = new ComputeBuffer(maxEffects, Marshal.SizeOf(typeof(RadialShockwaveData)));

        shockwaveMaterial.SetBuffer("_EffectBuffer", effectBuffer);
        shockwaveMaterial.SetBuffer("_LineShockwaveBuffer", lineDataBuffer);
        shockwaveMaterial.SetBuffer("_RadialShockwaveBuffer", radialDataBuffer);
    }

    void OnDisable() {
        effectBuffer?.Release();
        lineDataBuffer?.Release();
        radialDataBuffer?.Release();

        effectBuffer = null;
        lineDataBuffer = null;
        radialDataBuffer = null;
    }

    void LateUpdate() {
        List<ShockwaveEffectData> toSend = new();
        List<LineShockwaveData> toSendLineData = new();
        List<RadialShockwaveData> toSendRadialData = new();

        for (int i = activeEffects.Count - 1; i >= 0; i--) {
            var effect = activeEffects[i];
            effect.timer += Time.deltaTime;

            float progress = effect.timer / effect.duration;

            if (progress >= 1f) {
                activeEffects.RemoveAt(i);
                activeLineData.RemoveAt(i);
                radialShockwaveData.RemoveAt(i);
                continue;
            }

            effect.data.time = Mathf.Clamp01(effect.timer / effect.duration);
            effect.data.strength = Mathf.Lerp(effect.originalStrength, 0f, progress);
            effect.data.emissionStrength = Mathf.Clamp01(1f - progress * progress);
            activeEffects[i] = effect;

            toSend.Add(effect.data);

            // Match effect index exactly for both radial and line data
            if (effect.data.type == 1 && i < activeLineData.Count) {
                toSendLineData.Add(activeLineData[i]);
                toSendRadialData.Add(new RadialShockwaveData()); // dummy
            } else if (effect.data.type == 0 && i < radialShockwaveData.Count) {
                toSendRadialData.Add(radialShockwaveData[i]);
                toSendLineData.Add(new LineShockwaveData()); // dummy
            } else {
                // Just in case something is missing
                toSendLineData.Add(new LineShockwaveData());
                toSendRadialData.Add(new RadialShockwaveData());
            }
        }

        if (toSend.Count > 0) {
            effectBuffer.SetData(toSend);
            lineDataBuffer.SetData(toSendLineData);
            radialDataBuffer.SetData(toSendRadialData);
            shockwaveMaterial.SetFloat("_EffectCount", toSend.Count);
        } else {
            shockwaveMaterial.SetFloat("_EffectCount", 0);
        }
    }


    public void AddShockwave(Vector3 origin, ShockwaveSettings settings) {
        if (activeEffects.Count >= maxEffects) return;

        var data = new ShockwaveEffectData {
            origin = origin,
            time = 0f,
            strength = settings.strength,
            type = settings.GetTypeId(),
            emissionColorStart = settings.emissionColorStart,
            emissionColorEnd = settings.emissionColorEnd,
            emissionStrength = 1f
        };

        var effect = new ActiveShockwave {
            data = data,
            duration = settings.duration,
            originalStrength = settings.strength,
            timer = 0f
        };

        activeEffects.Add(effect);

        switch (settings) {
            case LineShockwaveSettings line:
                Vector3 direction = (line.target - origin).normalized;
                float extensionDistance = 20.0f;
                Vector3 extendedTarget = line.target + direction * extensionDistance;

                activeLineData.Add(new LineShockwaveData {
                    endPoint = extendedTarget,
                    width = line.width
                });

                radialShockwaveData.Add(new RadialShockwaveData()); // dummy
                break;


            case RadialShockwaveSettings radial:
                radialShockwaveData.Add(new RadialShockwaveData {
                    maxRadius = radial.maxRadius,
                    thickness = radial.thickness,
                    clearRadius = radial.clearRadius,
                    padding = 0f
                });
                activeLineData.Add(new LineShockwaveData()); // dummy
                break;
        }
    }

}

