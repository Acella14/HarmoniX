#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;

public class CDDeveloperTools : MonoBehaviour
{
    public GameObject cdObject;
    public CDThumbnailCapture thumbnailCapture;
    public Button captureThumbnailButton;
    public Button savePrefabButton;
    public TMP_InputField cdNameInputField;
    public string prefabSavePath = "Assets/Prefabs";

    private void Start()
    {
        if (!Directory.Exists(prefabSavePath))
        {
            Directory.CreateDirectory(prefabSavePath);
        }

        captureThumbnailButton.onClick.AddListener(CaptureThumbnail);
        savePrefabButton.onClick.AddListener(SaveAsPrefab);
    }

    private void CaptureThumbnail()
    {
        if (thumbnailCapture != null)
        {
            string fileName = "CDThumbnail_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            thumbnailCapture.CaptureScreenshot(fileName);
        }
        else
        {
            Debug.LogError("Thumbnail Capture reference is missing!");
        }
    }

    private void SaveAsPrefab()
    {
        if (cdObject != null)
        {
            string cdName = cdNameInputField != null ? cdNameInputField.text : "CDPreset";
            if (string.IsNullOrEmpty(cdName))
            {
                cdName = "CDPreset";
            }

            // Create a directory for this CD based on the entered name
            string cdFolder = Path.Combine(prefabSavePath, cdName);
            if (!Directory.Exists(cdFolder))
            {
                Directory.CreateDirectory(cdFolder);
            }

            CDControl cdControl = cdObject.GetComponent<CDControl>();
            if (cdControl != null)
            {
                // Disable the front and back projectors
                if (cdControl.frontDecalProjector != null)
                {
                    cdControl.frontDecalProjector.gameObject.SetActive(false);
                }
                if (cdControl.backDecalProjector != null)
                {
                    cdControl.backDecalProjector.gameObject.SetActive(false);
                }
            }

            DecalProjectorControl[] decalProjectors = cdObject.GetComponentsInChildren<DecalProjectorControl>(true);
            foreach (var decalProjector in decalProjectors)
            {
                decalProjector.ApplyProperties();

                // Save decal materials to disk if they are newly created (not the front/back projectors)
                if (decalProjector.decalMaterial != cdControl.frontDecalProjector?.GetDecalMaterial() &&
                    decalProjector.decalMaterial != cdControl.backDecalProjector?.GetDecalMaterial())
                {
                    decalProjector.decalMaterial = SaveMaterialToDisk(decalProjector.decalMaterial, cdFolder, cdName);
                }
            }

            // Save the CD prefab in the new directory
            string prefabName = cdName + ".prefab";
            string fullPath = Path.Combine(cdFolder, prefabName);
            PrefabUtility.SaveAsPrefabAssetAndConnect(cdObject, fullPath, InteractionMode.UserAction);
            Debug.Log($"CD saved as prefab: {fullPath}");
        }
        else
        {
            Debug.LogError("CD Object reference is missing!");
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("InitialScene", LoadSceneMode.Single);
    }

    private Material SaveMaterialToDisk(Material material, string cdFolder, string cdName)
    {
        if (material == null)
            return null;

        // Generate a unique file name for the material
        string materialName = cdName + "_" + material.name + "_" + material.GetInstanceID() + ".mat";
        string materialPath = Path.Combine(cdFolder, materialName);

        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMaterial == null)
        {
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            EditorUtility.CopySerialized(material, existingMaterial);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Material saved to: {materialPath}");

        // Load the saved material from disk to ensure reference is updated
        return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    }

}
#endif
