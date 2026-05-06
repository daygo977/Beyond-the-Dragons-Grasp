using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    [Header("UI")]
    public GameObject pauseMenuUI;
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioMixer audioMixer;
    public string volumeParameter = "MasterVolume";

    [Header("Scene Options")]
    public string mainMenuSceneName = "";

    public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadSettings();

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        IsPaused = true;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        if (audioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Clamp(savedVolume, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat(volumeParameter, dB);
        }
        else
        {
            AudioListener.volume = savedVolume;
        }
    }

    public void LeaveGame()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            ResumeGame();
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Application.Quit();
        }
    }
}