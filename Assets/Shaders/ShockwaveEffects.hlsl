#define MAX_EFFECTS 32

struct ShockwaveEffectData {
    float3 origin;
    float radius;
    float maxRadius;
    float strength;
    float thickness;
    float clearRadius;
    int type;
    float3 direction;
    float4 emissionColor;
    float emissionStrength;
};

struct LineShockwaveData {
    float3 endPoint;
    float width;
};

StructuredBuffer<ShockwaveEffectData> _EffectBuffer;
StructuredBuffer<LineShockwaveData> _LineShockwaveBuffer;
float _EffectCount;

float3 ClosestPointOnLine(float3 a, float3 b, float3 p)
{
    float3 ap = p - a;
    float3 ab = b - a;
    float t = dot(ap, ab) / dot(ab, ab);
    t = clamp(t, 0.0, 1.0);
    return a + t * ab;
}

void ApplyShockwaves_float(float3 objectPos, out float3 displacement, out float3 emission)
{
    displacement = float3(0, 0, 0);
    emission = float3(0, 0, 0);

    for (int i = 0; i < MAX_EFFECTS; i++) {
        if (i >= (int)_EffectCount)
            break;

        ShockwaveEffectData e = _EffectBuffer[i];

        if (e.type == 0) {
            float3 localOrigin = mul(unity_WorldToObject, float4(e.origin, 1.0)).xyz;
            float dist = length(objectPos - localOrigin);
            float clearStep = smoothstep(e.clearRadius - 0.01, e.clearRadius + 0.01, dist);
            float inner = smoothstep(e.radius, e.radius + e.thickness, dist);
            float outer = smoothstep(e.radius + e.thickness, e.radius + e.thickness * 2, dist);
            float falloff = max(inner - outer, 0);
            float intensity = falloff * clearStep;

            displacement += float3(0, 0, 1) * e.strength * intensity;
            emission += e.emissionColor.rgb * intensity * e.emissionStrength;
        }

        if (e.type == 1) {
            LineShockwaveData lineData = _LineShockwaveBuffer[i];
            float3 start = mul(unity_WorldToObject, float4(e.origin, 1.0)).xyz;
            float3 end = mul(unity_WorldToObject, float4(lineData.endPoint, 1.0)).xyz;

            float3 closest = ClosestPointOnLine(start, end, objectPos);
            float dist = distance(objectPos, closest);
            float band = smoothstep(lineData.width, lineData.width * 1.5, dist);
            float intensity = (1 - band) * e.strength;

            displacement += float3(0, 0, 1) * intensity;
            emission += e.emissionColor.rgb * intensity * e.emissionStrength;
        }
    }
}
