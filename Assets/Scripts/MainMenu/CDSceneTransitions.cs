using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CDSceneTransitions : MonoBehaviour
{
    public void TransitionToInitialScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("InitialScene", LoadSceneMode.Single);
    }

    public void TransitionToAudioManaging()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("AudioManaging", LoadSceneMode.Single);
    }

    public void TransitionToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
