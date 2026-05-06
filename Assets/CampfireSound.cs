using UnityEngine;

public class CampfireSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip fireClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 0.2f;

    [Header("Distance")]
    public float fullVolumeDistance = 1f;
    public float weakVolumeDistance = 8f;
    public float silentDistance = 10f;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (audioSource == null || fireClip == null) return;

        audioSource.clip = fireClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = fullVolumeDistance;
        audioSource.maxDistance = silentDistance;

        AnimationCurve rolloffCurve = new AnimationCurve();

        rolloffCurve.AddKey(0f, 1f);
        rolloffCurve.AddKey(fullVolumeDistance, 1f);
        rolloffCurve.AddKey(weakVolumeDistance, 0.15f);
        rolloffCurve.AddKey(silentDistance, 0f);

        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);

        audioSource.Play();
    }
}