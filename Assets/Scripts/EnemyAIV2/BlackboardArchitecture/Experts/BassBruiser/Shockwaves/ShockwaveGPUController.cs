using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ShockwaveGPUController : MonoBehaviour {
    public Material shockwaveMaterial;
    public int maxEffects = 32;
    public AudioSource sfxSource;

    private ComputeBuffer effectBuffer;
    private ComputeBuffer lineDataBuffer;

    private List<ActiveShockwave> activeEffects = new();
    private List<LineShockwaveData> activeLineData = new();

    [StructLayout(LayoutKind.Sequential)]
    public struct ShockwaveEffectData {
        public Vector3 origin;
        public float radius;
        public float maxRadius;
        public float strength;
        public float thickness;
        public float clearRadius;
        public int type;
        public Vector3 direction;
        public Vector4 emissionColor;
        public float emissionStrength;
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
        effectBuffer = new ComputeBuffer(maxEffects, Marshal.SizeOf(typeof(ShockwaveEffectData)));
        lineDataBuffer = new ComputeBuffer(maxEffects, Marshal.SizeOf(typeof(LineShockwaveData)));
        shockwaveMaterial.SetBuffer("_EffectBuffer", effectBuffer);
        shockwaveMaterial.SetBuffer("_LineShockwaveBuffer", lineDataBuffer);
    }

    void OnDisable() {
        effectBuffer?.Release();
        lineDataBuffer?.Release();
    }

    void LateUpdate() {
        List<ShockwaveEffectData> toSend = new();
        List<LineShockwaveData> toSendLineData = new();

        for (int i = activeEffects.Count - 1; i >= 0; i--) {
            var effect = activeEffects[i];
            effect.timer += Time.deltaTime;

            float progress = effect.timer / effect.duration;

            if (progress >= 1f) {
                activeEffects.RemoveAt(i);
                continue;
            }

            effect.data.radius = Mathf.Lerp(0f, effect.data.maxRadius, progress);
            effect.data.strength = Mathf.Lerp(effect.originalStrength, 0f, progress);
            effect.data.emissionStrength = Mathf.Clamp01(1f - progress * progress);

            activeEffects[i] = effect;
            toSend.Add(effect.data);

            if (effect.data.type == 1 && i < activeLineData.Count) {
                toSendLineData.Add(activeLineData[i]);
            }
        }

        if (toSend.Count > 0) {
            effectBuffer.SetData(toSend);
            shockwaveMaterial.SetFloat("_EffectCount", toSend.Count);
        } else {
            shockwaveMaterial.SetFloat("_EffectCount", 0);
        }

        if (toSendLineData.Count > 0) {
            lineDataBuffer.SetData(toSendLineData);
        }
    }

    public void AddShockwave(Vector3 origin, ShockwaveSettings settings) {
        if (activeEffects.Count >= maxEffects) return;

        var effect = new ActiveShockwave {
            data = new ShockwaveEffectData {
                origin = origin,
                radius = 0f,
                maxRadius = settings.maxRadius,
                strength = settings.strength,
                thickness = settings.thickness,
                clearRadius = 0.01f,
                type = settings.GetTypeId(),
                direction = Vector3.zero,
                emissionColor = settings.emissionColor,
                emissionStrength = 1f
            },
            duration = settings.duration,
            originalStrength = settings.strength,
            timer = 0f
        };

        activeEffects.Add(effect);

        if (settings.GetTypeId() == 1 && settings is LineShockwaveSettings lineSettings) {
            activeLineData.Add(new LineShockwaveData {
                endPoint = lineSettings.target,
                width = lineSettings.width
            });
        } else {
            activeLineData.Add(new LineShockwaveData());
        }
    }
}
