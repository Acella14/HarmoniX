using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance;

    public List<Song> songs = new List<Song>();
    public Song currentSong;

    private string saveDirectory = "SavedSongs";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllSongs();

            if (songs.Count > 0 && currentSong == null)
            {
                currentSong = songs[0];
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadAllSongs()
    {
        songs.Clear();
        string directoryPath = Path.Combine(Application.persistentDataPath, saveDirectory);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            return;
        }

        // Get all JSON files in the directory
        string[] jsonFiles = Directory.GetFiles(directoryPath, "*.json");
        Debug.Log($"Found {jsonFiles.Length} song(s) in directory.");

        foreach (string jsonFile in jsonFiles)
        {
            string jsonContent = File.ReadAllText(jsonFile);
            RhythmGameSaveData saveData = JsonUtility.FromJson<RhythmGameSaveData>(jsonContent);

            string audioFilePath = saveData.audioFilePath;

            // Load the associated audio clip
            AudioClip audioClip = LoadAudioClip(audioFilePath);

            if (audioClip != null)
            {
                Song song = new Song
                {
                    saveData = saveData,
                    audioClip = audioClip
                };
                songs.Add(song);
                Debug.Log($"Loaded song: {saveData.userGivenName}");
            }
            else
            {
                Debug.LogError($"Failed to load audio clip at path: {audioFilePath}");
            }
        }
        
        Debug.Log($"Total songs loaded: {songs.Count}");

        if (songs.Count > 0 && currentSong == null)
        {
            currentSong = songs[0];
        }
    }

    private AudioClip LoadAudioClip(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Audio file does not exist at the specified path: " + path);
            return null;
        }

        AudioType audioType = GetAudioType(path);

        using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, audioType))
        {
            var request = www.SendWebRequest();
            while (!request.isDone) { }

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
                return null;
            }
            else
            {
                return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
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

    public void RefreshSongList()
    {
        LoadAllSongs();
    }

    public void SetCurrentSong(Song song)
    {
        currentSong = song;
    }
}
