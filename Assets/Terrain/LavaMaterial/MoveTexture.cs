using UnityEngine;

public class MoveTexture : MonoBehaviour
{
    public float scrollSpeed = 0.1f;
    private Material material;

    void Start()
    {
        // Get the material from the MeshRenderer
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            material = meshRenderer.material; // Fetch the material instance
        }
    }

    void Update()
    {
        if (material != null)
        {
            // Scroll the Base Map texture (specific to URP Lit Shader)
            float moveThis = Time.time * scrollSpeed;
            material.SetTextureOffset("_BaseMap", new Vector2(0, moveThis));
        }
    }
}
