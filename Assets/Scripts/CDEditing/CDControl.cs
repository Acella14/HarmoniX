using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Rendering.Universal;

public class CDControl : MonoBehaviour
{
    public float rotationSpeed = 200f;
    public GameObject cdObject;
    public float smoothRotationSpeed = 5f;

    public DecalProjectorControl frontDecalProjector;
    public DecalProjectorControl backDecalProjector;

    public Button toggleModeButton;
    public Button toggleFaceButton;

    private bool isDraggingCD = false;
    private bool isEditMode = false;
    private bool isShowingFrontFace = true;
    private Vector2 dragInput;
    private Vector2 originalMousePosition;
    private float currentRotationY;
    private bool isRotating = false;
    private Camera mainCamera;
    private DecalProjectorControl activeDecal;

    public Slider decalScaleSlider;
    private float initialDecalSize = 0.5f;

    public Button applyStickerButton;

    public AudioSource backgroundMusicSource;
    public TextMeshProUGUI songNameText;

    private float croppedStartTime;
    private float croppedEndTime;


    private void Awake()
    {
        mainCamera = Camera.main;
        UpdateModeButton(); 
    }

    private void Start() 
    {
        decalScaleSlider.value = initialDecalSize;
        if (applyStickerButton != null)
        {
            applyStickerButton.onClick.AddListener(OnApplyStickerButtonClick);
        }

        Song currentSong = SongManager.Instance.currentSong;

        if (currentSong != null)
        {
            if (songNameText != null)
            {
                songNameText.text = currentSong.saveData.userGivenName;
            }

            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.clip = currentSong.audioClip;

                croppedStartTime = currentSong.saveData.croppedStartTime;
                croppedEndTime = currentSong.saveData.croppedEndTime;

                backgroundMusicSource.time = croppedStartTime;
                backgroundMusicSource.Play();
            }
            else
            {
                Debug.LogError("Background music AudioSource is not assigned.");
            }
        }
        else
        {
            Debug.LogError("No current song found in SongManager.");
        }
    }

    private void Update()
    {
        if (!isEditMode)
        {
            if (isDraggingCD && dragInput != Vector2.zero)
            {
                RotateCD(dragInput);
            }
        }
        else if (activeDecal != null && dragInput != Vector2.zero)
        {
            activeDecal.StartDragging();
        }

        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            if (backgroundMusicSource.time >= croppedEndTime)
            {
                backgroundMusicSource.time = croppedStartTime;
            }
        }
    }

    private void RotateCD(Vector2 dragInput)
    {
        float rotationDelta = -dragInput.x * rotationSpeed * Time.deltaTime;
        currentRotationY += rotationDelta;
        currentRotationY = Mathf.Repeat(currentRotationY, 360f);
        transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (isEditMode)
            {
                activeDecal = GetDecalUnderMouse();
                if (activeDecal != null)
                {
                    activeDecal.StartDragging();
                }
            }
            else if (IsMouseOverCD()) 
            {
                isDraggingCD = true;
                originalMousePosition = Mouse.current.position.ReadValue(); 
                Cursor.visible = false; 
            }
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            if (isDraggingCD)
            {
                Mouse.current.WarpCursorPosition(originalMousePosition); 
                Cursor.visible = true; 
                isDraggingCD = false;
            }

            if (activeDecal != null)
            {
                activeDecal.StopDragging();
                activeDecal = null;
            }
        }
    }

    public void OnDrag(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            if (isEditMode && activeDecal != null)
            {
                dragInput = context.ReadValue<Vector2>();
            }
            else if (isDraggingCD)
            {
                dragInput = context.ReadValue<Vector2>();
                RotateCD(dragInput);
            }
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            dragInput = Vector2.zero;
        }
    }

    private bool IsMouseOverCD()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == cdObject.transform)
            {
                return true;
            }
        }
        return false;
    }

    private DecalProjectorControl GetDecalUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform.GetComponent<DecalProjectorControl>();
        }
        return null;
    }

    public void ToggleEditViewMode()
    {
        if (!isRotating)
        {
            if (isEditMode)
            {
                EnterViewMode();
            }
            else
            {
                EnterEditMode();
            }
            UpdateModeButton();
        }
    }

    public void EnterEditMode()
    {
        isEditMode = true;
        currentRotationY = transform.eulerAngles.y;

        if (currentRotationY < 90f || currentRotationY > 270f)
        {
            isShowingFrontFace = true;
            StartCoroutine(RotateToFace(0f));
            frontDecalProjector.EnableDecalInteraction(true);
            backDecalProjector.EnableDecalInteraction(false);
        }
        else
        {
            isShowingFrontFace = false;
            StartCoroutine(RotateToFace(180f));
            frontDecalProjector.EnableDecalInteraction(false);
            backDecalProjector.EnableDecalInteraction(true);
        }

        cdObject.GetComponent<BoxCollider>().enabled = false;
        UpdateFaceButton();

        EnableDecalInteraction(true);
    }

    public void EnterViewMode()
    {
        isEditMode = false;
        cdObject.GetComponent<BoxCollider>().enabled = true;
        EnableDecalInteraction(false);
    }

    private IEnumerator RotateToFace(float targetYRotation)
    {
        isRotating = true;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothRotationSpeed);
            yield return null;
        }
        transform.rotation = targetRotation;
        isRotating = false;
    }

    public void ToggleFace()
    {
        if (isEditMode && !isRotating)
        {
            if (isShowingFrontFace)
            {
                StartCoroutine(RotateToFace(180f));
                isShowingFrontFace = false;
                frontDecalProjector.EnableDecalInteraction(false);
                backDecalProjector.EnableDecalInteraction(true);
            }
            else
            {
                StartCoroutine(RotateToFace(0f));
                isShowingFrontFace = true;
                frontDecalProjector.EnableDecalInteraction(true);
                backDecalProjector.EnableDecalInteraction(false);
            }

            UpdateFaceButton();
        }
    }


    public void ApplyStickerToDecal(StickerData sticker)
    {
        DecalProjectorControl currentDecal = isShowingFrontFace ? frontDecalProjector : backDecalProjector;
        
        currentDecal.SetDecalMaterial(currentDecal.GetDecalMaterial(), sticker.baseTexture, sticker.normalMap, false);
    }


    private void ApplyStickerToMaterial(Material material, StickerData sticker)
    {
        if (material == null)
        {
            Debug.LogError("Material is null!");
            return;
        }

        if (sticker.baseTexture != null)
        {
            material.SetTexture("Base_Map", sticker.baseTexture);
        }

        if (sticker.normalMap != null)
        {
            material.SetTexture("Normal_Map", sticker.normalMap);
        }

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(material);
        #endif
    }

    public Material ApplyCurrentSticker()
    {
        DecalProjectorControl currentDecal = isShowingFrontFace ? frontDecalProjector : backDecalProjector;

        GameObject decalCopy = Instantiate(currentDecal.gameObject, currentDecal.transform.position, currentDecal.transform.rotation);
        decalCopy.transform.SetParent(cdObject.transform);

        DecalProjectorControl decalCopyControl = decalCopy.GetComponent<DecalProjectorControl>();
        if (decalCopyControl != null)
        {
            decalCopyControl.SetDecalMaterial(currentDecal.GetDecalMaterial(), currentDecal.baseTexture, currentDecal.normalMap, true);
            decalCopyControl.SetDecalSize(currentDecal.GetDecalSize());

            decalCopyControl.enabled = false;
            
            return decalCopyControl.GetDecalMaterial();
        }

        BoxCollider collider = decalCopy.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        return null;
    }


    private void EnableDecalInteraction(bool enable)
    {
        frontDecalProjector.EnableDecalInteraction(enable);
        backDecalProjector.EnableDecalInteraction(enable);
    }

    private void UpdateModeButton()
    {
        toggleModeButton.GetComponentInChildren<TextMeshProUGUI>().text = isEditMode ? "Current Mode: Edit" : "Current Mode: View";
    }

    private void UpdateFaceButton()
    {
        toggleFaceButton.GetComponentInChildren<TextMeshProUGUI>().text = isShowingFrontFace ? "Front Face" : "Back Face";
        decalScaleSlider.value = isShowingFrontFace ? frontDecalProjector.GetDecalSize() : backDecalProjector.GetDecalSize();
    }

    public void OnDecalScaleChanged(float value)
    {
        DecalProjectorControl currentDecal = isShowingFrontFace ? frontDecalProjector : backDecalProjector;
        currentDecal.SetDecalSize(value);
    }

    public void OnApplyStickerButtonClick()
    {
        ApplyCurrentSticker();
    }

}