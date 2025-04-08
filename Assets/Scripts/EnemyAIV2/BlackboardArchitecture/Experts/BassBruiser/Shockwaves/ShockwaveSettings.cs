using UnityEngine;

[System.Serializable]
public abstract class ShockwaveSettings {
    public float maxRadius = 10f;
    public float duration = 1.5f;
    public float strength = 0.5f;
    public float thickness = 1.0f;
    public Color emissionColor = Color.cyan;
    public float particleScale = 1.0f;

    public abstract int GetTypeId();
}

// === TYPE 0: Radial ===
[System.Serializable]
public class RadialShockwaveSettings : ShockwaveSettings {
    public override int GetTypeId() => 0;
}

// === TYPE 1: Line ===
[System.Serializable]
public class LineShockwaveSettings : ShockwaveSettings {
    public Vector3 target = Vector3.forward;
    public float width = 0.5f;

    public override int GetTypeId() => 1;
}
