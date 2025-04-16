#define MAX_EFFECTS 32

struct ShockwaveEffectData {
    float3 origin;
    float time;

    float strength;
    int type;
    float emissionStrength;
    float padding0;

    float4 emissionColorStart;
    float4 emissionColorEnd;

    float4 padding1;
};

struct RadialShockwaveData {
    float maxRadius;
    float thickness;
    float clearRadius;
    float padding;
};

struct LineShockwaveData {
    float3 endPoint;
    float width;
};


StructuredBuffer<ShockwaveEffectData> _EffectBuffer;
StructuredBuffer<RadialShockwaveData> _RadialShockwaveBuffer;
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
        
            RadialShockwaveData radialData = _RadialShockwaveBuffer[i];
        
            if (dist < radialData.clearRadius)
                continue;
        
            float fadeIn = smoothstep(radialData.clearRadius, radialData.clearRadius + 0.1, dist);
        
            float inner = smoothstep(radialData.maxRadius * e.time, radialData.maxRadius * e.time + radialData.thickness, dist);
            float outer = smoothstep(radialData.maxRadius * e.time + radialData.thickness, radialData.maxRadius * e.time + radialData.thickness * 2, dist);
            float falloff = max(inner - outer, 0);
            
            float intensity = falloff * fadeIn;
        
            displacement += float3(0, 0, 1) * e.strength * intensity;
        
            float shockStart = radialData.maxRadius * e.time;
            float shockEnd = shockStart + radialData.thickness * 2;
        
            float waveProgress = saturate(e.time);
            float timeBias = pow(waveProgress, 2.0);
            float spatialT = saturate((dist - shockStart) / (shockEnd - shockStart));
            float combinedT = saturate(spatialT * timeBias);
        
            float3 gradientColor = lerp(e.emissionColorStart.rgb, e.emissionColorEnd.rgb, combinedT);
            emission += gradientColor * e.emissionStrength * intensity;
        }        
        

        if (e.type == 1)
        {
            LineShockwaveData lineData = _LineShockwaveBuffer[i];

            float3 start = mul(unity_WorldToObject, float4(e.origin, 1.0)).xyz;
            float3 end   = mul(unity_WorldToObject, float4(lineData.endPoint, 1.0)).xyz;

            float3 lineDir = normalize(end - start);
            float totalLength = distance(start, end);
            float3 toPoint = objectPos - start;

            float alongLine = dot(toPoint, lineDir);
            float3 closest = start + lineDir * alongLine;
            float distToLine = distance(objectPos, closest);

            if (distToLine > lineData.width || alongLine < 0 || alongLine > totalLength)
                continue;

            float tLine = saturate(alongLine / totalLength);

            // Configured durations
            float growTime = 0.5;
            float fallTime = 0.5;

            // Time at which this point starts rising
            float riseStart = tLine * growTime;
            float riseEnd = riseStart + growTime * 0.1;

            float heightAlpha = 0.0;

            if (e.time < riseStart)
            {
                // Wave hasn't reached this point
                heightAlpha = 0.0;
            }
            else if (e.time < riseEnd)
            {
                // Rising
                float localT = (e.time - riseStart) / (riseEnd - riseStart);
                heightAlpha = smoothstep(0.0, 1.0, localT);
            }
            else if (e.time < growTime)
            {
                // Fully risen and holding until wave reaches the end
                heightAlpha = 1.0;
            }
            else if (e.time < growTime + fallTime)
            {
                // Entire line is built — start falling uniformly
                float fallT = (e.time - growTime) / fallTime;
                heightAlpha = 1.0 - smoothstep(0.0, 1.0, fallT);
            }
            else
            {
                heightAlpha = 0.0;
            }

            // Apply vertical displacement
            displacement += float3(0, 0, 1) * e.strength * heightAlpha;

            // Emission (unchanged)
            float3 gradientColor = lerp(e.emissionColorStart.rgb, e.emissionColorEnd.rgb, tLine);
            emission += gradientColor * e.emissionStrength * saturate(heightAlpha);
        }


        
    }
}