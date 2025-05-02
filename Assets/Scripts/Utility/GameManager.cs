using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth          playerHealth;
    public PostProcessingFeedback postProcessingFeedback;
    public PlayerInputHandler    inputHandler;
    public CanvasGroup           deathScreenUI;
    public CanvasGroup            winScreenUI;
    [Tooltip("All gameplay AudioSources")]
    public AudioSource[]         audioSources;

    [Header("Death Sequence")]
    public float deathRampDuration = 1f;
    [Header("Win Sequence")]
    public float winFadeDuration = 1f;

    private int remainingEnemies;

    void Awake()
    {
        deathScreenUI.interactable    = false;
        deathScreenUI.blocksRaycasts  = false;
        deathScreenUI.alpha           = 0f;

        winScreenUI.alpha = 0;
        winScreenUI.blocksRaycasts = false;
        winScreenUI.interactable = false;
    }

    void Start()
    {
        remainingEnemies = FindObjectsOfType<Enemy>().Length;
    }

    void OnEnable()
    {
        playerHealth.OnDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        playerHealth.OnDeath -= HandlePlayerDeath;
    }

    void HandlePlayerDeath()
    {
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        inputHandler.enabled = false;

        // 2) ramp post-processing & audio down/up
        float timer = 0f;
        // cache original audio levels
        float[] origVolumes = audioSources.Select(a => a.volume).ToArray();

        while (timer < deathRampDuration)
        {
            float t = timer / deathRampDuration;

            float startV = postProcessingFeedback.InitialVignetteIntensity;
            float targetV = startV + postProcessingFeedback.vignetteIntensityBoost;
            float startC = postProcessingFeedback.InitialChromaticIntensity;
            float targetC = postProcessingFeedback.chromaticBoost;

            postProcessingFeedback.SetEffectIntensity(
                Mathf.Lerp(startV, targetV, t),
                Mathf.Lerp(startC, targetC, t)
            );

            // fade out audio
            for (int i = 0; i < audioSources.Length; i++)
                audioSources[i].volume = Mathf.Lerp(origVolumes[i], 0f, t);

            timer += Time.deltaTime;
            yield return null;
        }

        postProcessingFeedback.SetEffectIntensity(
            postProcessingFeedback.InitialVignetteIntensity + postProcessingFeedback.vignetteIntensityBoost,
            postProcessingFeedback.chromaticBoost
        );
        foreach (var a in audioSources) a.volume = 0f;

        deathScreenUI.alpha = 1;
        deathScreenUI.blocksRaycasts = true;
        deathScreenUI.interactable = true;

        Cursor.lockState   = CursorLockMode.None;
        Cursor.visible     = true;
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnEnemyKilled()
    {
        remainingEnemies--;
        if (remainingEnemies <= 0)
            StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        inputHandler.enabled = false;

        float timer = 0f;
        float[] origVolumes = audioSources.Select(a => a.volume).ToArray();

        while (timer < winFadeDuration)
        {
            float t = timer / winFadeDuration;
            winScreenUI.alpha = t;
            timer += Time.deltaTime;
            yield return null;
        }

        winScreenUI.alpha = 1;
        winScreenUI.blocksRaycasts = true;
        winScreenUI.interactable = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
