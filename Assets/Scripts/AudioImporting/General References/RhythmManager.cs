using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance;

    public AudioSource audioSource;
    public Song currentSong;
    public int startSongIndex = 0;

    private List<float> beatMarkers;
    private int nextBeatIndex = 0;
    [HideInInspector]
    public int CurrentBeatIndex { get; private set; } = -1;
    private List<BeatRecord> beatRecords = new List<BeatRecord>();

    public UnityEvent OnBeat;
    public UnityEvent<float> OnNextBeatTime; // event that provides time until the next beat
    public UnityEvent OnHalfBeat;
    public UnityEvent OnSongChanged = new UnityEvent();

    public InputAction nextSongAction;
    public InputAction previousSongAction;

    private float preBeatMargin = 0.18f; // Leniency before the beat (in seconds)
    private float postBeatMargin = 0.14f; // Leniency after the beat (in seconds)
    private float beatInterval;
    private float lastPlaybackTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        nextSongAction.performed += ctx => SwitchToNextSong();
        previousSongAction.performed += ctx => SwitchToPreviousSong();

        nextSongAction.Enable();
        previousSongAction.Enable();
    }

    private void OnDisable()
    {
        nextSongAction.performed -= ctx => SwitchToNextSong();
        previousSongAction.performed -= ctx => SwitchToPreviousSong();

        nextSongAction.Disable();
        previousSongAction.Disable();
    }

    void Start()
    {
        if (SongManager.Instance != null && SongManager.Instance.songs.Count > 0)
        {
            startSongIndex = Mathf.Clamp(startSongIndex, 0, SongManager.Instance.songs.Count - 1);

            StartSong(SongManager.Instance.songs[startSongIndex]);
        }
        else
        {
            Debug.LogError("No songs available to start.");
        }
    }


    public void StartSong(Song song)
    {
        currentSong = song;
        if (SongManager.Instance != null)
        {
            SongManager.Instance.SetCurrentSong(song);
        }
        beatMarkers = song.saveData.beatMarkerTimes;
        nextBeatIndex = 0;
        beatRecords.Clear();
        audioSource.clip = song.audioClip;

        if (song.saveData.croppedStartTime > 0f)
        {
            audioSource.time = song.saveData.croppedStartTime;
        }
        else
        {
            audioSource.time = 0f;
        }

        audioSource.Play();

        if (song.saveData.bpm > 0f)
        {
            beatInterval = 60f / song.saveData.bpm;
        }
        else
        {
            Debug.LogWarning("BPM is not set for the current song. Defaulting beat interval to 0.5 seconds.");
            beatInterval = 0.5f;
        }

        lastPlaybackTime = audioSource.time;
        OnSongChanged?.Invoke();
    }

    private void Update()
    {
        if (audioSource.isPlaying && beatMarkers != null && beatMarkers.Count > 0)
        {
            float currentTime = GetCurrentSongTime();

            // Check if the audio has looped
            if (currentTime < lastPlaybackTime)
            {
                ResetBeatTracking();
            }

            while (nextBeatIndex < beatMarkers.Count && currentTime >= beatMarkers[nextBeatIndex] - preBeatMargin)
            {
                float beatTime = beatMarkers[nextBeatIndex];
                Debug.Log($"Beat invoked at time {beatTime}");

                CurrentBeatIndex = nextBeatIndex;

                // Invoke the OnBeat event
                OnBeat?.Invoke();

                // Calculate time until next beat if available
                if (nextBeatIndex + 1 < beatMarkers.Count)
                {
                    float timeUntilNextBeat = beatMarkers[nextBeatIndex + 1] - currentTime;
                    OnNextBeatTime?.Invoke(timeUntilNextBeat);
                }

                beatRecords.Add(new BeatRecord(beatTime, nextBeatIndex)); // Record the beat with its index
                nextBeatIndex++;
            }

            lastPlaybackTime = currentTime;
        }
    }

    private void ResetBeatTracking()
    {
        Debug.Log("Audio loop detected, resetting beat tracking.");
        nextBeatIndex = 0;
        beatRecords.Clear();
    }

    public bool IsOnBeatNow(float timestamp)
    {
        if (audioSource.isPlaying && beatRecords.Count > 0)
        {
            float currentTime = timestamp;

            BeatRecord currentBeat = beatRecords[beatRecords.Count - 1];
            BeatRecord? previousBeat = beatRecords.Count > 1 ? (BeatRecord?)beatRecords[beatRecords.Count - 2] : null;

            // Check if the timestamp falls within the pre-beat and post-beat margin
            if (currentTime >= currentBeat.time - preBeatMargin && currentTime <= currentBeat.time + postBeatMargin)
            {
                Debug.Log($"On-beat detected at time {currentTime} (current beat: {currentBeat.time})");
                return true;
            }

            if (previousBeat.HasValue && currentTime >= previousBeat.Value.time - preBeatMargin && currentTime <= previousBeat.Value.time + postBeatMargin)
            {
                Debug.Log($"On-beat detected at time {currentTime} (previous beat: {previousBeat.Value.time})");
                return true;
            }

            if (currentTime < currentBeat.time - preBeatMargin)
            {
                Debug.Log($"Shot detected too early at time {currentTime} (next beat: {currentBeat.time})");
            }
            else if (currentTime > currentBeat.time + postBeatMargin)
            {
                Debug.Log($"Shot detected too late at time {currentTime} (current beat: {currentBeat.time})");
            }
        }
        return false;
    }

    public bool IsOnBeatNow()
    {
        float currentTime = GetCurrentSongTime();
        return IsOnBeatNow(currentTime);
    }

    public void SwitchToNextSong()
    {
        if (SongManager.Instance != null && SongManager.Instance.songs.Count > 0)
        {
            int currentIndex = SongManager.Instance.songs.FindIndex(song => song.saveData.userGivenName == currentSong.saveData.userGivenName);

            if (currentIndex == -1)
            {
                currentIndex = 0; // Default to first song if not found
            }

            int nextIndex = (currentIndex + 1) % SongManager.Instance.songs.Count;

            Debug.Log($"Switching to next song: {SongManager.Instance.songs[nextIndex].saveData.userGivenName}, Index: {nextIndex}");

            StartSong(SongManager.Instance.songs[nextIndex]);
        }
    }

    public void SwitchToPreviousSong()
    {
        if (SongManager.Instance != null && SongManager.Instance.songs.Count > 0)
        {
            int currentIndex = SongManager.Instance.songs.FindIndex(song => song.saveData.userGivenName == currentSong.saveData.userGivenName);

            if (currentIndex == -1)
            {
                currentIndex = SongManager.Instance.songs.Count - 1; // Default to last song if not found
            }

            int prevIndex = (currentIndex - 1 + SongManager.Instance.songs.Count) % SongManager.Instance.songs.Count;

            Debug.Log($"Switching to previous song: {SongManager.Instance.songs[prevIndex].saveData.userGivenName}, Index: {prevIndex}");

            StartSong(SongManager.Instance.songs[prevIndex]);
        }
    }

    public float GetBeatInterval()
    {
        return beatInterval;
    }

    public float GetCurrentSongTime()
    {
        return audioSource.time + (currentSong.saveData.croppedStartTime > 0f ? currentSong.saveData.croppedStartTime : 0f);
    }

    public List<float> GetBeatTimes()
    {
        return beatMarkers;
    }
}

public struct BeatRecord
{
    public float time;
    public int index;

    public BeatRecord(float time, int index)
    {
        this.time = time;
        this.index = index;
    }
}
