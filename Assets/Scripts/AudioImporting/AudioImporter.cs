using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using SFB;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AudioImporter : MonoBehaviour
{
    [Header("Audio Source and Playback")]
    public AudioSource audioSource;
    private bool isPlaying = false;

    [Header("UI Components")]
    public WaveformVisualizer waveformVisualizer;
    public BeatManager beatManager;
    public Scrollbar horizontalScrollbar;
    public UIActionHandling UIActionHandling;

    [Header("Playhead Marker")]
    public GameObject playheadMarkerPrefab;
    private GameObject playheadMarker;
    private PlayheadMarkerMover playheadMarkerMover;
    private bool ignorePlayhead = true;

    [Header("Waveform Handles")]
    public RectTransform leftHandleRect;
    public RectTransform rightHandleRect;
    public GameObject leftHandle;
    public GameObject rightHandle;
    private RectTransform leftHandleGreyMaskRect;
    private RectTransform rightHandleGreyMaskRect;

    [Header("Waveform Cropping")]
    public RectTransform waveformMaskRect;
    public RectTransform leftCroppedObject;
    public RectTransform rightCroppedObject;
    public static int requiredHandleSpacingDistance = 1480;
    private float waveformCroppedLeftPoint;
    private float waveformCroppedRightPoint;
    private bool ignoreHandles = false;

    [Header("Auto-Scroll Settings")]
    public bool autoScrollEnabled = false;
    [SerializeField] private float scrollZoneSize = 50f;
    [SerializeField] private float scrollSpeed = 1000f;
    [SerializeField] private float handleScrollZoneSize = 80f;
    [SerializeField] private float handleScrollSpeed = 1000f;

    private bool isDraggingWaveform = false;
    private bool draggedMostRecently = false;

    private float leftmostVisiblePoint;
    private float rightmostVisiblePoint;
    private float completeLeftPoint;
    private float completeRightPoint;
    private float waveformClampMaxX;
    private float waveformClampMinX;
    private CustomScrollRect customScrollRect;
    private RectTransform waveformVisualizerRect;
    private RawImage waveformVisualizerRawImage;

    private float waveformWidth;
    private Vector2 waveformPos;

    private float[] playbackSpeeds = { 1f, 1.5f, 0.5f };
    private int currentSpeedIndex = 0;

    private void Start()
    {
        leftHandleGreyMaskRect = leftHandle.GetComponent<WaveformHandles>().GetHandleGreyMask();
        rightHandleGreyMaskRect = rightHandle.GetComponent<WaveformHandles>().GetHandleGreyMask();

        customScrollRect = waveformVisualizer.GetComponent<CustomScrollRect>();
        waveformVisualizerRect = waveformVisualizer.GetComponent<RectTransform>();
        waveformVisualizerRawImage = waveformVisualizer.GetComponent<RawImage>();
        waveformWidth = waveformVisualizerRect.rect.width;
    }

    public void OnManualDrag()
    {
        SetAutoScroll(false);
        isDraggingWaveform = true;
        UpdateWaveformCroppedPoints();
    }

    public void OnEndDrag()
    {
        isDraggingWaveform = false;
        draggedMostRecently = true;
    }

    public void OnWaveformDoubleClick(float clickXPosition)
    {
        if (clickXPosition > waveformCroppedLeftPoint && clickXPosition < waveformCroppedRightPoint)
        {
            float newPlayheadTime = Mathf.InverseLerp(0, waveformVisualizerRect.rect.width, clickXPosition) * audioSource.clip.length;
            playheadMarkerMover.SetAnchoredPosition(new Vector2(clickXPosition, playheadMarkerMover.GetAnchoredPosition().y));
            audioSource.time = newPlayheadTime;
        }
    }

    public void LoadAudio()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Audio File", "", extensions, false);
        if (paths.Length > 0)
        {
            StartCoroutine(LoadAudioClip(paths[0]));
            UIActionHandling.OnImportAudio();
        }
        else
        {
            return;
        }
    }

    private IEnumerator LoadAudioClip(string path)
    {
        AudioType audioType = GetAudioType(path);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null)
                {
                    Debug.LogError("Failed to load audio clip.");
                    yield break;
                }

                InitializeAudioComponents(clip);
                beatManager.SaveOriginalFilePath(path);
            }
        }
    }

    private AudioType GetAudioType(string path)
    {
        if (path.EndsWith(".mp3")) return AudioType.MPEG;
        if (path.EndsWith(".wav")) return AudioType.WAV;
        if (path.EndsWith(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.UNKNOWN;
    }

    private void InitializeAudioComponents(AudioClip clip)
    {
        if (audioSource == null || waveformVisualizer == null || beatManager == null)
        {
            Debug.LogError("Missing component assignments.");
            return;
        }

        audioSource.clip = clip;
        waveformVisualizer.GenerateWaveform(audioSource.clip);
        beatManager.isAudioLoaded = true;

        SetWaveformAlpha(1f);

        waveformClampMaxX = leftHandleRect.rect.width;
        waveformClampMinX = waveformClampMaxX - waveformVisualizerRect.rect.width - (leftHandle.GetComponent<RectTransform>().rect.width * 2) + waveformMaskRect.rect.width;

        customScrollRect?.UpdateScrollLimits(waveformClampMinX, waveformClampMaxX);

        if (playheadMarker == null)
        {
            playheadMarker = Instantiate(playheadMarkerPrefab, waveformVisualizer.transform);
            playheadMarkerMover = playheadMarker.GetComponent<PlayheadMarkerMover>();
            playheadMarkerMover.Initialize(waveformVisualizerRect, this);
        }

        playheadMarkerMover.SetAnchoredPosition(new Vector2(0, 0));

        beatManager.SetInitialBeatMarker();

        SetInitialHandlePositions();

        UpdateWaveformCroppedPoints();

        if (horizontalScrollbar != null)
        {
            horizontalScrollbar.value = 0;
        }
    }

    private void SetWaveformAlpha(float alpha)
    {
        Color currentColor = waveformVisualizerRawImage.color;
        currentColor.a = alpha;
        waveformVisualizerRawImage.color = currentColor;
    }

    private void SetInitialHandlePositions()
    {
        leftHandleRect.anchoredPosition = new Vector2(-leftHandleRect.rect.width / 2, 0);
        rightHandleRect.anchoredPosition = new Vector2(waveformVisualizerRect.rect.width + (rightHandleRect.rect.width / 2), 0);
        leftHandleGreyMaskRect.anchoredPosition = new Vector2(-leftHandleRect.rect.width - leftHandleGreyMaskRect.rect.width, 0);
        rightHandleGreyMaskRect.anchoredPosition = new Vector2(rightHandleRect.rect.width - rightHandleGreyMaskRect.rect.width, 0);
    }

    public void UpdateWaveformCroppedPoints()
    {
        waveformCroppedLeftPoint = leftHandleRect.anchoredPosition.x + (leftHandleRect.rect.width / 2);
        waveformCroppedRightPoint = rightHandleRect.anchoredPosition.x - (rightHandleRect.rect.width / 2);
    }

    public void TogglePlayPause()
    {
        if (audioSource == null || audioSource.clip == null || playheadMarker == null)
        {
            Debug.LogError("Missing component assignments.");
            return;
        }

        if (isPlaying)
        {
            audioSource.Pause();
        }
        else
        {
            if (autoScrollEnabled)
            {
                SnapWaveformToLeftPoint();
            }
            audioSource.time = playheadMarkerMover.GetPlayheadTime(audioSource.clip.length);
            audioSource.Play();
        }

        isPlaying = !isPlaying;
        UIActionHandling.OnTogglePlayPause(isPlaying);
    }

    public void PauseAudio()
    {
        if (isPlaying)
        {
            audioSource.Pause();
            isPlaying = false;
            UIActionHandling.OnTogglePlayPause(isPlaying);
        }
    }

    private void Update()
    {
        waveformWidth = waveformVisualizerRect.rect.width;
        waveformPos = waveformVisualizerRect.anchoredPosition;

        UpdateScrollbar();
        UpdateVisiblePoints();
        UpdateCroppedObjects();

        if (isPlaying)
        {
            playheadMarkerMover.UpdatePlayheadPosition(audioSource.time, waveformWidth, audioSource.clip.length);
            if (autoScrollEnabled)
            {
                UpdateWaveformPosition();
            }
        }

        if (playheadMarker != null && !isPlaying)
        {
            if (!isDraggingWaveform && !draggedMostRecently)
            {
                if (!ignorePlayhead)
                {
                    CheckManualPlayheadScrollZones();
                }
                else if (!ignoreHandles)
                {
                    CheckManualHandleScrollZones();
                    CheckIfHandlesPushPlayhead();
                }
            }
        }
    }

    private void UpdateScrollbar()
    {
        if (horizontalScrollbar != null)
        {
            float normalizedValue = Mathf.InverseLerp(waveformClampMaxX - leftHandleRect.rect.width, waveformClampMinX, waveformPos.x);
            horizontalScrollbar.SetValueWithoutNotify(normalizedValue);
        }
    }

    private void UpdateVisiblePoints()
    {
        if (waveformPos.x > 0)
        {
            leftmostVisiblePoint = Mathf.Max(0, waveformCroppedLeftPoint);
            rightmostVisiblePoint = Mathf.Min(waveformMaskRect.rect.width - waveformPos.x, waveformCroppedRightPoint);
        }
        else
        {
            leftmostVisiblePoint = Mathf.Max(-waveformPos.x, waveformCroppedLeftPoint);
            rightmostVisiblePoint = leftmostVisiblePoint == waveformCroppedLeftPoint
                ? Mathf.Min(leftmostVisiblePoint + (waveformMaskRect.rect.width - (waveformCroppedLeftPoint + waveformPos.x)), waveformCroppedRightPoint)
                : Mathf.Min(leftmostVisiblePoint + waveformMaskRect.rect.width, waveformCroppedRightPoint);
        }

        completeLeftPoint = -waveformPos.x + leftHandleRect.rect.width / 2;
        completeRightPoint = -waveformPos.x + waveformMaskRect.rect.width - rightHandleRect.rect.width / 2;
    }

    private void UpdateCroppedObjects()
    {
        leftCroppedObject.anchoredPosition = new Vector2(waveformCroppedLeftPoint, 0);
        rightCroppedObject.anchoredPosition = new Vector2(waveformCroppedRightPoint, 0);
    }

    private void LateUpdate()
    {
        if (playheadMarker != null)
        {
            ClampWaveformPosition();
        }
    }

    private void CheckIfHandlesPushPlayhead()
    {
        float leftHandlePosition = leftHandleRect.anchoredPosition.x + (leftHandleRect.rect.width / 2);
        float rightHandlePosition = rightHandleRect.anchoredPosition.x - (rightHandleRect.rect.width / 2);
        float playheadMarkerPosition = playheadMarkerMover.GetAnchoredPosition().x;

        if (leftHandlePosition > playheadMarkerPosition)
        {
            playheadMarkerMover.SetAnchoredPosition(new Vector2(leftHandlePosition, playheadMarkerMover.GetAnchoredPosition().y));
            ignorePlayhead = true;
        }
        else if (rightHandlePosition < playheadMarkerPosition)
        {
            playheadMarkerMover.SetAnchoredPosition(new Vector2(rightHandlePosition, playheadMarkerMover.GetAnchoredPosition().y));
            ignorePlayhead = true;
        }
    }

    public void SnapWaveformToLeftPoint()
    {
        Vector2 playheadPosition = playheadMarkerMover.GetAnchoredPosition();
        float newWaveformPosX = Mathf.Clamp(-playheadPosition.x + waveformMaskRect.rect.width / 2, -waveformCroppedRightPoint - rightHandleRect.rect.width + waveformMaskRect.rect.width, -waveformCroppedLeftPoint + leftHandleRect.rect.width);
        Vector2 targetPosition = new Vector2(newWaveformPosX, waveformVisualizerRect.anchoredPosition.y);

        StartCoroutine(SmoothMoveWaveform(targetPosition, 0.2f));
    }

    private IEnumerator SmoothMoveWaveform(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = waveformVisualizerRect.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            waveformVisualizerRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        waveformVisualizerRect.anchoredPosition = targetPosition;

        waveformPos = waveformVisualizerRect.anchoredPosition;

        UpdateVisiblePoints();
        UpdateWaveformPosition();
    }

    public void UpdateWaveformPosition()
    {
        if (playheadMarker != null)
        {
            Vector2 playheadPosition = playheadMarkerMover.GetAnchoredPosition();

            float currentCenterPoint = ((rightmostVisiblePoint - leftmostVisiblePoint) / 2) + leftmostVisiblePoint;

            if (isPlaying)
            {
                if (playheadPosition.x > currentCenterPoint)
                {
                    float difference = playheadPosition.x - currentCenterPoint;
                    float targetWaveformPosX = waveformPos.x - difference;

                    float newWaveformPosX = Mathf.Lerp(waveformPos.x, targetWaveformPosX, Time.deltaTime * 10f);
                    newWaveformPosX = Mathf.Clamp(newWaveformPosX, -waveformCroppedRightPoint - rightHandleRect.rect.width + waveformMaskRect.rect.width, -waveformCroppedLeftPoint + leftHandleRect.rect.width);

                    waveformVisualizerRect.anchoredPosition = new Vector2(newWaveformPosX, waveformVisualizerRect.anchoredPosition.y);
                }
                else if (playheadPosition.x < currentCenterPoint)
                {
                    float difference = currentCenterPoint - playheadPosition.x;
                    float targetWaveformPosX = waveformPos.x + difference;

                    float newWaveformPosX = Mathf.Lerp(waveformPos.x, targetWaveformPosX, Time.deltaTime * 10f);
                    newWaveformPosX = Mathf.Clamp(newWaveformPosX, -waveformCroppedRightPoint - rightHandleRect.rect.width + waveformMaskRect.rect.width, -waveformCroppedLeftPoint + leftHandleRect.rect.width);

                    waveformVisualizerRect.anchoredPosition = new Vector2(newWaveformPosX, waveformVisualizerRect.anchoredPosition.y);
                }
            }
        }
    }

    private void CheckManualHandleScrollZones()
    {
        float leftHandleXPosition = leftHandleRect.anchoredPosition.x;
        float rightHandleXPosition = rightHandleRect.anchoredPosition.x;

        if (waveformPos.x < leftHandleRect.rect.width)
        {
            if (leftHandleXPosition > completeLeftPoint && leftHandleXPosition < completeLeftPoint + handleScrollZoneSize)
            {
                HandleScrollWaveformRight(true);
            }
            else if (rightHandleXPosition > completeLeftPoint && rightHandleXPosition < completeLeftPoint + handleScrollZoneSize)
            {
                HandleScrollWaveformRight(false);
            }
        }

        if (waveformPos.x > -waveformWidth - leftHandleRect.rect.width + waveformMaskRect.rect.width)
        {
            if (leftHandleXPosition < completeRightPoint && leftHandleXPosition > completeRightPoint - handleScrollZoneSize)
            {
                HandleScrollWaveformLeft(true);
            }
            else if (rightHandleXPosition < completeRightPoint && rightHandleXPosition > completeRightPoint - handleScrollZoneSize)
            {
                HandleScrollWaveformLeft(false);
            }
        }
    }

    private void HandleScrollWaveformRight(bool isLeftHandle)
    {
        float newWaveformPosX = waveformPos.x + handleScrollSpeed * Time.deltaTime;
        waveformVisualizerRect.anchoredPosition = new Vector2(newWaveformPosX, 0);

        if (isLeftHandle)
        {
            float newHandlePosX = Mathf.Max(leftHandleRect.anchoredPosition.x - handleScrollSpeed * Time.deltaTime, -(leftHandleRect.rect.width / 2));
            leftHandleRect.GetComponent<WaveformHandles>().SetHandleAnchoredPosition(new Vector2(newHandlePosX, 0));
        }
        else
        {
            float newHandlePosX = Mathf.Max(rightHandleRect.anchoredPosition.x - handleScrollSpeed * Time.deltaTime, -(rightHandleRect.rect.width / 2));
            rightHandleRect.GetComponent<WaveformHandles>().SetHandleAnchoredPosition(new Vector2(newHandlePosX, 0));
        }
        UpdateWaveformCroppedPoints();
    }

    private void HandleScrollWaveformLeft(bool isLeftHandle)
    {
        float newWaveformPosX = waveformPos.x - handleScrollSpeed * Time.deltaTime;
        waveformVisualizerRect.anchoredPosition = new Vector2(newWaveformPosX, 0);

        if (isLeftHandle)
        {
            float newHandlePosX = Mathf.Min(leftHandleRect.anchoredPosition.x + handleScrollSpeed * Time.deltaTime, waveformWidth + leftHandleRect.rect.width / 2);
            leftHandleRect.GetComponent<WaveformHandles>().SetHandleAnchoredPosition(new Vector2(newHandlePosX, 0));
        }
        else
        {
            float newHandlePosX = Mathf.Min(rightHandleRect.anchoredPosition.x + handleScrollSpeed * Time.deltaTime, waveformWidth + rightHandleRect.rect.width / 2);
            rightHandleRect.GetComponent<WaveformHandles>().SetHandleAnchoredPosition(new Vector2(newHandlePosX, 0));
        }
        UpdateWaveformCroppedPoints();
    }

    private void CheckManualPlayheadScrollZones()
    {
        float playheadXPosition = playheadMarkerMover.GetAnchoredPosition().x;

        if (waveformPos.x < 0 && waveformPos.x < (-leftmostVisiblePoint) + leftHandleRect.rect.width)
        {
            if (playheadXPosition > leftmostVisiblePoint && playheadXPosition < leftmostVisiblePoint + scrollZoneSize)
            {
                PlayheadScrollWaveformRight();
            }
        }
        else if (waveformPos.x > 0 && waveformPos.x < leftHandleRect.rect.width)
        {
            if (playheadXPosition > leftmostVisiblePoint && playheadXPosition < leftmostVisiblePoint + scrollZoneSize)
            {
                PlayheadScrollWaveformRight();
            }
        }

        if (waveformPos.x > (-waveformWidth + waveformMaskRect.rect.width) && waveformPos.x > -(rightmostVisiblePoint + rightHandleRect.rect.width) + waveformMaskRect.rect.width)
        {
            if (playheadXPosition < rightmostVisiblePoint && playheadXPosition > rightmostVisiblePoint - scrollZoneSize)
            {
                PlayheadScrollWaveformLeft();
            }
        }
        else if (waveformPos.x < (-waveformWidth + waveformMaskRect.rect.width) && waveformPos.x > (-waveformWidth + waveformMaskRect.rect.width - rightHandleRect.rect.width))
        {
            if (playheadXPosition < rightmostVisiblePoint && playheadXPosition > rightmostVisiblePoint - scrollZoneSize)
            {
                PlayheadScrollWaveformLeft();
            }
        }
    }

    private void PlayheadScrollWaveformRight()
    {
        float newWaveformPosX = waveformPos.x + scrollSpeed * Time.deltaTime;
        waveformVisualizerRect.anchoredPosition = new Vector2(newWaveformPosX, 0);

        float newPlayheadPosX = playheadMarkerMover.GetAnchoredPosition().x - scrollSpeed * Time.deltaTime;
        if (newPlayheadPosX > 0 && newPlayheadPosX > leftmostVisiblePoint)
        {
            playheadMarkerMover.SetAnchoredPosition(new Vector2(newPlayheadPosX, 0));
        }
    }

    private void PlayheadScrollWaveformLeft()
    {
        float newWaveformPosX = waveformPos.x - scrollSpeed * Time.deltaTime;
        waveformVisualizerRect.anchoredPosition = new Vector2(newWaveformPosX, 0);

        float newPlayheadPosX = playheadMarkerMover.GetAnchoredPosition().x + scrollSpeed * Time.deltaTime;
        if (newPlayheadPosX < waveformWidth && newPlayheadPosX < (rightmostVisiblePoint))
        {
            playheadMarkerMover.SetAnchoredPosition(new Vector2(newPlayheadPosX, 0));
        }
    }

    public void ToggleAutoScroll()
    {
        SetAutoScroll(!autoScrollEnabled);
    }

    private void SetAutoScroll(bool enabled)
    {
        autoScrollEnabled = enabled;
        UIActionHandling.OnToggleAutoScroll(enabled);
    }

    private void ClampWaveformPosition()
    {
        waveformPos = waveformVisualizerRect.anchoredPosition;
        waveformPos.x = Mathf.Clamp(waveformPos.x, waveformClampMinX, waveformClampMaxX);
        waveformVisualizerRect.anchoredPosition = new Vector2(waveformPos.x, 0);
    }

    public void OnScrollbarValueChanged(float value)
    {
        if (waveformVisualizer != null)
        {
            float newX = Mathf.Lerp(waveformClampMaxX + (leftHandleRect.rect.width / 2), waveformClampMinX, value);
            waveformVisualizerRect.anchoredPosition = new Vector2(newX, 0);
        }
    }

    public void TogglePlaybackSpeed()
    {
        currentSpeedIndex = (currentSpeedIndex + 1) % playbackSpeeds.Length;
        audioSource.pitch = playbackSpeeds[currentSpeedIndex];
        UIActionHandling.OnTogglePlaybackSpeed(playbackSpeeds[currentSpeedIndex]);
    }

    public void ResetButton()
    {
        if (isPlaying)
        {
            TogglePlayPause();
        }

        ignoreHandles = true;
        waveformVisualizerRect.anchoredPosition = new Vector2(leftHandleRect.rect.width - waveformCroppedLeftPoint, 0);
        playheadMarkerMover.SetAnchoredPosition(new Vector2(waveformCroppedLeftPoint, 0));
    }

    public float GetLeftmostVisiblePoint() => leftmostVisiblePoint;
    public float GetRightmostVisiblePoint() => rightmostVisiblePoint;
    public float GetCompleteLeftPoint() => completeLeftPoint;
    public float GetCompleteRightPoint() => completeRightPoint;
    public float GetWaveformCroppedLeftPoint() => waveformCroppedLeftPoint;
    public float GetWaveformCroppedRightPoint() => waveformCroppedRightPoint;
    public float GetWaveformClampMin() => waveformClampMaxX;
    public float GetWaveformClampMax() => waveformClampMinX;
    public CustomScrollRect GetCustomScrollRect() => customScrollRect;

    public void SetDraggedMostRecently(bool draggedMostRecently)
    {
        this.draggedMostRecently = draggedMostRecently;
    }

    public void SetIsDraggingWaveform(bool isDraggingWaveform)
    {
        this.isDraggingWaveform = isDraggingWaveform;
    }

    public void SetIgnorePlayhead(bool ignorePlayhead)
    {
        this.ignorePlayhead = ignorePlayhead;
    }

    public void SetIgnoreHandles(bool ignoreHandles)
    {
        this.ignoreHandles = ignoreHandles;
    }

    public void OnPlayButtonPress()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("InitialScene", LoadSceneMode.Single);
    }
}
