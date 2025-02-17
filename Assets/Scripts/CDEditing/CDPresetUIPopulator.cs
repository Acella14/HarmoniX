using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CDPresetUIPopulator : MonoBehaviour
{
    public GameObject content;
    public GameObject prefab;
    public List<CDPresetData> presetList;

    public CDControl cdControl;

    private void Start()
    {
        PopulateScrollView();
    }

    public void PopulateScrollView()
    {
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (CDPresetData preset in presetList)
        {
            GameObject newPreset = Instantiate(prefab, content.transform);

            Image frontFaceImage = newPreset.transform.Find("Front Side").GetComponent<Image>();
            Image backSideImage = newPreset.transform.Find("Back Side").GetComponent<Image>();

            if (frontFaceImage != null && preset.frontFaceTexture != null)
            {
                frontFaceImage.sprite = TextureToSprite(preset.frontFaceTexture);
            }

            if (backSideImage != null && preset.backSideTexture != null)
            {
                backSideImage.sprite = TextureToSprite(preset.backSideTexture);
            }
        }
    }

    private Sprite TextureToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /*

    // Apply the preset when clicked
    private void ApplyPreset(CDPresetData preset)
    {
        Debug.Log("Preset Clicked: " + preset.frontFaceTexture.name);

        // Send the preset data to the CDControl script to apply it
        if (cdControl != null)
        {
            cdControl.ApplyPresetToCD(preset);
        }
        else
        {
            Debug.LogError("CDControl reference is missing!");
        }
    }

    */
}
