using UnityEngine;
using UnityEngine.UI;

public class UIActionHandling : MonoBehaviour
{
    [Header("Play Button")]
    public Sprite playButton;
    public Sprite pauseButton;
    public Button playPauseButton;

    [Header("Playback Speed")]
    public Sprite[] playbackSpeedSprites;
    public Button playbackSpeedButton;

    [Header("Auto Scroll")]
    public Sprite autoScrollDisabledSprite;
    public Sprite autoScrollEnabledSprite;
    public Button autoScrollButton;

    [Header("Waveform Mask")]
    public Sprite noImportedAudioSprite;
    public Sprite importedAudioSprite;
    public GameObject waveformMask;

    public void OnTogglePlayPause(bool isPlaying) {
        playPauseButton.GetComponentInChildren<Image>().sprite = isPlaying ? pauseButton : playButton;
    }

    public void OnTogglePlaybackSpeed(float playbackSpeed) {
        switch (playbackSpeed) {
            case 1f:
                playbackSpeedButton.GetComponentInChildren<Image>().sprite = playbackSpeedSprites[0];
                break;
            case 1.5f:
                playbackSpeedButton.GetComponentInChildren<Image>().sprite = playbackSpeedSprites[1];
                break;
            case 0.5f:
                playbackSpeedButton.GetComponentInChildren<Image>().sprite = playbackSpeedSprites[2];
                break;
            default:
                break;
        };
    }

    public void OnToggleAutoScroll(bool enabled) {
        if (enabled) {
            autoScrollButton.GetComponentInChildren<Image>().sprite = autoScrollEnabledSprite;
        } else {
            autoScrollButton.GetComponentInChildren<Image>().sprite = autoScrollDisabledSprite;
        }
    }

    public void OnImportAudio() {
        waveformMask.GetComponentInChildren<Image>().sprite = importedAudioSprite;
    }
}
