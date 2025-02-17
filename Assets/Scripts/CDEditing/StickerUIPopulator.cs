using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StickerUIPopulator : MonoBehaviour
{
    public GameObject content;
    public GameObject imagePrefab;
    public List<StickerData> stickerList;

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

        foreach (StickerData sticker in stickerList)
        {
            GameObject newImage = Instantiate(imagePrefab, content.transform);

            Image imageComponent = newImage.transform.Find("Sticker Image").GetComponent<Image>();
            if (imageComponent != null && sticker.baseTexture != null)
            {
                imageComponent.sprite = TextureToSprite(sticker.baseTexture);
            }

            Button button = newImage.GetComponent<Button>();
            if (button != null)
            {
                StickerData capturedSticker = sticker;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ApplySticker(capturedSticker));
            }
        }
    }

    private Sprite TextureToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void ApplySticker(StickerData sticker)
    {
        Debug.Log("Sticker Clicked: " + sticker.baseTexture.name);

        if (cdControl != null)
        {
            cdControl.ApplyStickerToDecal(sticker);
        }
        else
        {
            Debug.LogError("CDControl reference is missing!");
        }
    }

}
