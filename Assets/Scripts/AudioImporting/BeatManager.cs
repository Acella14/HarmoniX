using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class BeatManager : MonoBehaviour
{
    public AudioImporter audioImporter;
    public AudioSource audioSource;
    public RectTransform waveformRect;
    public Button detectBeatButton;
    public GameObject beatMarkerPrefab;
    public TMP_InputField bpmInputField;

    private GameObject firstBeatMarker;
    private float bpm;
    private bool isSetBPMValid = false;
    [HideInInspector] public bool isAudioLoaded = false;
    private float startTime;

    private string saveFileName = "RhythmGameSaveData.json";
    private string audioFilePath;

    public List<float> markerPositions = new List<float>();  // Pixel positions of beat markers

    public TMP_InputField songNameInputField;
    public string saveDirectory = "SavedSongs";

    private void Start()
    {
        string fullSavePath = Path.Combine(Application.persistentDataPath, saveDirectory);
        if (!Directory.Exists(fullSavePath))
        {
            Directory.CreateDirectory(fullSavePath);
        }

        detectBeatButton.onClick.AddListener(DetectBeatsAndPlaceMarkers);
        bpmInputField.onValueChanged.AddListener(ValidateBpmInput);
    }

    public void SetInitialBeatMarker()
    {
        firstBeatMarker = Instantiate(beatMarkerPrefab, waveformRect.transform);
        firstBeatMarker.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        firstBeatMarker.GetComponent<BeatMarkerMover>().Initialize(waveformRect);
        firstBeatMarker.name = "FirstBeatMarker";
    }

    private void ValidateBpmInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            isSetBPMValid = false;
            return;
        }

        if (int.TryParse(input, out int enteredBpm))
        {
            bpm = enteredBpm;
            isSetBPMValid = true;
            bpmInputField.textComponent.color = Color.black;
            Debug.Log("Manually set BPM: " + bpm);
        }
        else
        {
            isSetBPMValid = false;
            bpmInputField.textComponent.color = Color.red;
        }
    }

    private void DetectBeatsAndPlaceMarkers()
    {
        if (!isAudioLoaded)
        {
            Debug.LogError("Audio clip not loaded.");
            return;
        }

        if (!isSetBPMValid || bpm <= 0)
        {
            bpm = UniBpmAnalyzer.AnalyzeBpm(audioSource.clip);
            Debug.Log("Auto-detected BPM: " + bpm);
        }

        float initialBeatXPosition = firstBeatMarker.GetComponent<RectTransform>().anchoredPosition.x;
        float waveformWidth = waveformRect.rect.width;
        float clipLength = audioSource.clip.length;

        startTime = (initialBeatXPosition) / waveformWidth * clipLength;
        PlaceMarkers(startTime, waveformWidth, clipLength);
    }

    private void PlaceMarkers(float startTime, float waveformWidth, float clipLength)
    {
        float secondsPerBeat = 60f / bpm;
        float pixelsPerSecond = waveformWidth / clipLength;

        markerPositions.Clear();

        float firstBeatTime = startTime;
        while (firstBeatTime - secondsPerBeat >= 0)
        {
            firstBeatTime -= secondsPerBeat;
        }

        for (float t = firstBeatTime; t < clipLength; t += secondsPerBeat)
        {
            float xPosition = (t / clipLength) * waveformWidth;
            if (xPosition < 0 || xPosition > waveformWidth) continue;

            var marker = Instantiate(beatMarkerPrefab, waveformRect);
            marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPosition, 0);
            marker.GetComponent<BeatMarkerMover>().Initialize(waveformRect, this, markerPositions.Count);
            markerPositions.Add(xPosition);
        }

        firstBeatMarker.SetActive(false);
    }

    public void ResetMarkers()
    {
        foreach (Transform child in waveformRect)
        {
            if (child.gameObject.GetComponent<BeatMarkerMover>() != null && child.gameObject != firstBeatMarker)
            {
                Destroy(child.gameObject);
            }
        }

        firstBeatMarker.SetActive(true);
        firstBeatMarker.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        markerPositions.Clear();
    }

    public void UpdateMarkerPosition(int index, float newPosition)
    {
        if (index >= 0 && index < markerPositions.Count)
        {
            markerPositions[index] = newPosition;
        }
    }

    public List<float> GetMarkerPositions()
    {
        return new List<float>(markerPositions);
    }

    public List<float> GetMarkerTimes()
    {
        List<float> markerTimes = new List<float>();
        float waveformWidth = waveformRect.rect.width;
        float clipLength = audioSource.clip.length;

        foreach (float xPosition in markerPositions)
        {
            float time = (xPosition / waveformWidth) * clipLength;
            markerTimes.Add(time);
        }

        return markerTimes;
    }

    public void SaveOriginalFilePath(string path)
    {
        audioFilePath = path;
    }

    public void SaveData()
    {
        if (string.IsNullOrEmpty(audioFilePath))
        {
            Debug.LogError("No audio file loaded to save.");
            return;
        }

        if (string.IsNullOrEmpty(songNameInputField.text))
        {
            Debug.LogError("Please enter a name for the song before saving.");
            return;
        }

        List<float> beatTimes = GetMarkerTimes();

        RhythmGameSaveData saveData = new RhythmGameSaveData
        {
            UID = Guid.NewGuid().ToString(),
            originalFileName = Path.GetFileName(audioFilePath),
            userGivenName = songNameInputField.text,
            audioFilePath = SaveAudioClip(),
            bpm = bpm,
            importedByUser = true,
            croppedStartTime = GetCroppedStartTime(),
            croppedEndTime = GetCroppedEndTime(),
            beatMarkerPositions = GetMarkerPositions(),
            beatMarkerTimes = beatTimes
        };

        // Save JSON metadata
        string json = JsonUtility.ToJson(saveData);
        string fileName = $"{saveData.userGivenName}.json";
        string fullSavePath = Path.Combine(Application.persistentDataPath, saveDirectory, fileName);
        File.WriteAllText(fullSavePath, json);

        Debug.Log("Data saved successfully to " + fullSavePath);

        Song song = new Song
        {
            saveData = saveData,
            audioClip = audioSource.clip
        };
        SongManager.Instance.songs.Add(song);
        SongManager.Instance.SetCurrentSong(song);

        // load the next scene for now
        UnityEngine.SceneManagement.SceneManager.LoadScene("CDEditor");
    }

    private string SaveAudioClip()
    {
        if (!File.Exists(audioFilePath))
        {
            Debug.LogError("Audio file does not exist at the specified path: " + audioFilePath);
            return null;
        }

        // Ensure save directory exists
        string directoryPath = Path.Combine(Application.persistentDataPath, saveDirectory);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Determine file extension of original audio file
        string fileExtension = Path.GetExtension(audioFilePath);

        string audioFileName = $"{songNameInputField.text}{fileExtension}";

        string audioSavePath = Path.Combine(directoryPath, audioFileName);

        if (File.Exists(audioSavePath))
        {
            File.Delete(audioSavePath);
        }

        // Copy the file to the new directory
        try
        {
            File.Copy(audioFilePath, audioSavePath, overwrite: false);
            Debug.Log($"Audio file copied successfully to: {audioSavePath}");
        }
        catch (IOException ioEx)
        {
            Debug.LogError($"Failed to copy audio file. Exception: {ioEx.Message}");
            return null;
        }

        return audioSavePath;
    }

    public void LoadData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError("Save file not found.");
            return;
        }

        string json = File.ReadAllText(filePath);
        RhythmGameSaveData saveData = JsonUtility.FromJson<RhythmGameSaveData>(json);

        string audioClipPath = Path.Combine(Application.persistentDataPath, saveData.audioFilePath);
        StartCoroutine(LoadAudioClip(audioClipPath));
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

                audioSource.clip = clip;
                isAudioLoaded = true;

                InitializeAudioComponents(clip);
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
        SetInitialBeatMarker();
    }

    private float GetCroppedStartTime()
    {
        return audioImporter.GetWaveformCroppedLeftPoint() / waveformRect.rect.width * audioSource.clip.length;
    }

    private float GetCroppedEndTime()
    {
        return audioImporter.GetWaveformCroppedRightPoint() / waveformRect.rect.width * audioSource.clip.length;
    }
}

[System.Serializable]
public class RhythmGameSaveData
{
    public string UID;  // Unique Identifier for each song
    public string originalFileName; 
    public string userGivenName;
    public string audioFilePath;
    public float bpm;
    public bool importedByUser;  // Whether the audio was imported by the user or is part of the base soundtrack
    public float croppedStartTime;
    public float croppedEndTime;
    public List<float> beatMarkerPositions;  // List of positions of beat markers (pixel positions)
    public List<float> beatMarkerTimes;      // List of times of beat markers (in seconds)
}
