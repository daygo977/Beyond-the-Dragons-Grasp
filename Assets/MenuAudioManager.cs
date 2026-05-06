using UnityEngine;

public class MenuAudioManager : MonoBehaviour
{
    public static MenuAudioManager Instance;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    [Range(0f, 1f)] public float clickVolume = 0.6f;

    void Awake()
    {
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayHover()
    {
        PlaySound(hoverClip, hoverVolume);
    }

    public void PlayClick()
    {
        PlaySound(clickClip, clickVolume);
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null) return;
        if (clip == null) return;

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clip, volume);
    }
}