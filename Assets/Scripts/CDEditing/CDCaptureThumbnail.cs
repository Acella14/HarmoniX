using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.UI;

public class CDThumbnailCapture : MonoBehaviour
{
    public Camera captureCamera;
    public string saveFolderPath = "Assets/Thumbnails"; // Folder to save the screenshots
    public int imageWidth = 512;       // Thumbnail width
    public int imageHeight = 512;      // Thumbnail height
    public GameObject canvas;
    public GameObject cdObject;

    private void Start()
    {
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        captureCamera.enabled = false;
    }

    public void CaptureScreenshot(string fileName)
    {
        canvas.SetActive(false);
        captureCamera.enabled = true;
        cdObject.GetComponent<CDControl>().frontDecalProjector.gameObject.SetActive(false);
        cdObject.GetComponent<CDControl>().backDecalProjector.gameObject.SetActive(false);
        StartCoroutine(CaptureScreenshotCoroutine(fileName));
    }

    private IEnumerator CaptureScreenshotCoroutine(string fileName)
    {

        yield return new WaitForEndOfFrame();

        RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24);
        captureCamera.targetTexture = renderTexture;

        captureCamera.Render();

        // Create a texture to store the camera's output
        Texture2D screenshot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenshot.Apply();

        // Clean up
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // Encode the texture into a PNG file
        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(saveFolderPath, fileName + ".png"), bytes);
        Debug.Log($"Saved screenshot to {saveFolderPath}/{fileName}.png");

        cdObject.GetComponent<CDControl>().frontDecalProjector.gameObject.SetActive(true);
        cdObject.GetComponent<CDControl>().backDecalProjector.gameObject.SetActive(true);
        captureCamera.enabled = false;
        canvas.SetActive(true);
    }
}