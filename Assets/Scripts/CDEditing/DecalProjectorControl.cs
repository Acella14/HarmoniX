using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class DecalProjectorControl : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDraggingDecal = false;
    public Material decalMaterial;
    private BoxCollider boxCollider;

    public Texture2D baseTexture;
    public Texture2D normalMap;
    public Vector3 decalScale = new Vector3(0.5f, 0.5f, 0.1f);

    private void Awake()
    {
        mainCamera = Camera.main;
        boxCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if (isDraggingDecal)
        {
            DragDecal();
        }
    }

    private void DragDecal()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            transform.position = new Vector3(hit.point.x, hit.point.y, transform.position.z);
        }
    }

    public void StartDragging()
    {
        isDraggingDecal = true;
    }

    public void StopDragging()
    {
        isDraggingDecal = false;
    }

    public void EnableDecalInteraction(bool enable)
    {
        boxCollider.enabled = enable;
    }

    public void SetDecalSize(float size)
    {
        decalScale = new Vector3(size, size, decalScale.z);
        ApplyProperties();
    }

    public float GetDecalSize()
    {
        return decalScale.x;
    }

    public void SetDecalMaterial(Material baseMaterial, Texture2D texture, Texture2D normalMap, bool createNewMaterial = false)
    {
        if (createNewMaterial)
        {
            decalMaterial = new Material(baseMaterial);
            decalMaterial.name = baseMaterial.name + "_" + gameObject.GetInstanceID();
        }
        else
        {
            decalMaterial = baseMaterial;
        }

        this.baseTexture = texture;
        this.normalMap = normalMap;

        ApplyProperties();
    }


    public void ApplyProperties()
    {
        DecalProjector projector = GetComponent<DecalProjector>();
        if (projector != null)
        {
            projector.material = decalMaterial;
            projector.size = decalScale;

            if (decalMaterial != null && baseTexture != null && normalMap != null)
            {
                decalMaterial.SetTexture("Base_Map", baseTexture);
                decalMaterial.SetTexture("Normal_Map", normalMap);
            }
        }
    }

    public Material GetDecalMaterial()
    {
        return decalMaterial;
    }
}