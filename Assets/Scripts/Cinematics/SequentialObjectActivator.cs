using System.Collections.Generic;
using UnityEngine;

public class SequentialObjectActivator : MonoBehaviour
{
    [Header("Objects to Manage")]
    public List<GameObject> objectsToActivate;
    private int currentIndex = -1; // Tracks the last activated object (-1 means none)

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip activationSFX;

    void Start()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
            else
            {
                Debug.LogWarning("A null object was found in the list and will be skipped.");
            }
        }
    }

    public void ActivateNextObject()
    {
        if (objectsToActivate.Count == 0)
        {
            Debug.LogWarning("No objects in the list to activate.");
            return;
        }

        currentIndex = (currentIndex + 1) % objectsToActivate.Count;

        if (objectsToActivate[currentIndex] != null)
        {
            objectsToActivate[currentIndex].SetActive(true);

            PlayActivationSFX();
        }
        else
        {
            Debug.LogWarning($"Object at index {currentIndex} is null and will be skipped.");
        }
    }

    private void PlayActivationSFX()
    {
        if (audioSource != null && activationSFX != null)
        {
            audioSource.PlayOneShot(activationSFX);
        }
        else
        {
            if (audioSource == null)
            {
                Debug.LogWarning("AudioSource is not assigned.");
            }
            if (activationSFX == null)
            {
                Debug.LogWarning("Activation SFX is not assigned.");
            }
        }
    }
}
