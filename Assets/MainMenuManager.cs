using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Startup Logo")]
    public GameObject logoPanel;
    public float fadeDuration = 2f;
    public float mainMenuInputDelay = 2f;

    [Header("Main Panels")]
    public GameObject mainMenuPanel;
    public GameObject playPlaceholderPanel;
    public GameObject settingsPanel;

    [Header("Exit Confirmation")]
    public GameObject exitPromptPanel;
    public GameObject exitBlocker;

    [Header("Settings")]
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioMixer audioMixer;
    public string volumeParameter = "MasterVolume";

    private bool logoFinishedThisLaunch;
    private bool mainMenuButtonsEnabled;
    private bool exitPromptOpen;
    private bool isTransitioning;

    private static bool logoSeenThisAppLaunch;

    private Dictionary<Graphic, float> originalAlphas = new Dictionary<Graphic, float>();

    void Awake()
    {
        CacheOriginalAlphas(logoPanel);
        CacheOriginalAlphas(mainMenuPanel);
        CacheOriginalAlphas(playPlaceholderPanel);
        CacheOriginalAlphas(settingsPanel);
        CacheOriginalAlphas(exitPromptPanel);
        CacheOriginalAlphas(exitBlocker);
    }

    void Start()
    {
        LoadSettings();

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);

        CloseAllPanelsImmediate();

        if (logoSeenThisAppLaunch)
        {
            ShowMainMenuImmediate();
        }
        else
        {
            ShowLogoImmediate();
        }
    }

    void Update()
    {
        if (!logoFinishedThisLaunch && !isTransitioning && Input.anyKeyDown)
        {
            StartCoroutine(LogoToMainMenuRoutine());
        }
    }

    void CacheOriginalAlphas(GameObject panel)
    {
        if (panel == null) return;

        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (!originalAlphas.ContainsKey(graphic))
                originalAlphas.Add(graphic, graphic.color.a);
        }
    }

    void CloseAllPanelsImmediate()
    {
        if (logoPanel != null)
            logoPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (playPlaceholderPanel != null)
            playPlaceholderPanel.SetActive(false);

        if (exitPromptPanel != null)
            exitPromptPanel.SetActive(false);

        if (exitBlocker != null)
            exitBlocker.SetActive(false);
    }

    void ShowLogoImmediate()
    {
        logoFinishedThisLaunch = false;
        mainMenuButtonsEnabled = false;

        if (logoPanel != null)
        {
            logoPanel.SetActive(true);
            SetPanelAlphaMultiplier(logoPanel, 1f);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            SetPanelAlphaMultiplier(mainMenuPanel, 0f);
            SetPanelInteractable(mainMenuPanel, false);
        }
    }

    void ShowMainMenuImmediate()
    {
        logoFinishedThisLaunch = true;
        mainMenuButtonsEnabled = true;

        if (logoPanel != null)
            logoPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            SetPanelAlphaMultiplier(mainMenuPanel, 1f);
            SetPanelInteractable(mainMenuPanel, true);
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (playPlaceholderPanel != null)
            playPlaceholderPanel.SetActive(false);

        CloseExitPrompt();
    }

    IEnumerator LogoToMainMenuRoutine()
    {
        isTransitioning = true;
        logoFinishedThisLaunch = true;
        logoSeenThisAppLaunch = true;
        mainMenuButtonsEnabled = false;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            SetPanelAlphaMultiplier(mainMenuPanel, 0f);
            SetPanelInteractable(mainMenuPanel, false);
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            if (logoPanel != null)
                SetPanelAlphaMultiplier(logoPanel, 1f - t);

            if (mainMenuPanel != null)
                SetPanelAlphaMultiplier(mainMenuPanel, t);

            yield return null;
        }

        if (logoPanel != null)
            logoPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            SetPanelAlphaMultiplier(mainMenuPanel, 1f);
            mainMenuPanel.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(mainMenuInputDelay);

        mainMenuButtonsEnabled = true;
        isTransitioning = false;

        if (mainMenuPanel != null)
            SetPanelInteractable(mainMenuPanel, true);
    }

    public void Play()
    {
        if (!CanUseMainButtons()) return;

        if (playPlaceholderPanel != null)
            playPlaceholderPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        if (!CanUseMainButtons()) return;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            SetPanelAlphaMultiplier(settingsPanel, 1f);
            SetPanelInteractable(settingsPanel, true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            SetPanelAlphaMultiplier(mainMenuPanel, 1f);
            SetPanelInteractable(mainMenuPanel, true);
        }
    }

    public void OpenExitPrompt()
    {
        if (!CanUseMainButtons()) return;

        exitPromptOpen = true;

        if (exitBlocker != null)
            exitBlocker.SetActive(true);

        if (exitPromptPanel != null)
        {
            exitPromptPanel.SetActive(true);
            SetPanelAlphaMultiplier(exitPromptPanel, 1f);
            SetPanelInteractable(exitPromptPanel, true);
        }

        if (mainMenuPanel != null)
            SetPanelInteractable(mainMenuPanel, false);
    }

    public void CloseExitPrompt()
    {
        exitPromptOpen = false;

        if (exitPromptPanel != null)
            exitPromptPanel.SetActive(false);

        if (exitBlocker != null)
            exitBlocker.SetActive(false);

        if (mainMenuPanel != null && mainMenuButtonsEnabled)
            SetPanelInteractable(mainMenuPanel, true);
    }

    public void ConfirmExit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SetVolume(float sliderValue)
    {
        sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);

        if (audioMixer != null)
        {
            float dB = Mathf.Log10(sliderValue) * 20f;
            audioMixer.SetFloat(volumeParameter, dB);
        }
        else
        {
            AudioListener.volume = sliderValue;
        }

        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

        if (volumeSlider != null)
            volumeSlider.value = savedVolume;

        SetVolume(savedVolume);
    }

    bool CanUseMainButtons()
    {
        return mainMenuButtonsEnabled && !exitPromptOpen && !isTransitioning;
    }

    void SetPanelAlphaMultiplier(GameObject panel, float alphaMultiplier)
    {
        if (panel == null) return;

        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (!originalAlphas.ContainsKey(graphic))
                originalAlphas.Add(graphic, graphic.color.a);

            float originalAlpha = originalAlphas[graphic];

            Color color = graphic.color;
            color.a = originalAlpha * alphaMultiplier;
            graphic.color = color;
        }
    }

    void SetPanelInteractable(GameObject panel, bool interactable)
    {
        if (panel == null) return;

        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);

        foreach (Selectable selectable in selectables)
            selectable.interactable = interactable;
    }
}