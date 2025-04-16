using UnityEngine;

[System.Serializable]
public abstract class ShockwaveSettings {
    public float duration = 1.5f;
    public float strength = 0.5f;
    public Color emissionColorStart;
    public Color emissionColorEnd;
    public float particleScale = 1.0f;

    public abstract int GetTypeId();
}

// === TYPE 0: Radial ===
[System.Serializable]
public class RadialShockwaveSettings : ShockwaveSettings {
    public float maxRadius = 10f;
    public float thickness = 1.0f;
    public float clearRadius = 0f;

    public override int GetTypeId() => 0;
}

// === TYPE 1: Line ===
[System.Serializable]
public class LineShockwaveSettings : ShockwaveSettings {
    public Vector3 target = Vector3.forward;
    public float width = 0.5f;

    public override int GetTypeId() => 1;
}
