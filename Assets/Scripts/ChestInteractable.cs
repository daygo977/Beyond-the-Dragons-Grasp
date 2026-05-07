using UnityEngine;

public class ChestInteractable : MonoBehaviour, IHoldInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string closedPrompt = "Hold E to open chest";

    [TextArea]
    public string openedPrompt = "";

    [Header("Hold Settings")]
    public float holdDuration = 1.5f;

    [Header("Chest Models")]
    public GameObject closedChestModel;
    public GameObject openedChestModel;

    [Header("Reward")]
    public GameObject healthPickupObject;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip holdSound;
    public AudioClip[] openSounds;
    [Range(0f, 1f)] public float holdVolume = 0.6f;
    [Range(0f, 1f)] public float openVolume = 1f;

    private float holdTimer;
    private bool isOpen;
    private bool isHoldingSoundPlaying;

    void Start()
    {
        if (closedChestModel != null)
            closedChestModel.SetActive(true);

        if (openedChestModel != null)
            openedChestModel.SetActive(false);

        if (healthPickupObject != null)
            healthPickupObject.SetActive(false);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public string GetPromptText()
    {
        if (isOpen)
            return openedPrompt;

        return closedPrompt;
    }

    public void Interact()
    {
    }

    public void HoldInteract(float deltaTime)
    {
        if (isOpen) return;

        holdTimer += deltaTime;

        PlayHoldSound();

        if (holdTimer >= holdDuration)
            OpenChest();
    }

    public void ResetHold()
    {
        if (isOpen) return;

        holdTimer = 0f;
        StopHoldSound();
    }

    void OpenChest()
    {
        if (isOpen) return;

        isOpen = true;
        holdTimer = holdDuration;

        StopHoldSound();

        if (closedChestModel != null)
            closedChestModel.SetActive(false);

        if (openedChestModel != null)
            openedChestModel.SetActive(true);

        if (healthPickupObject != null)
            healthPickupObject.SetActive(true);

        PlayRandomOpenSound();
    }

    void PlayHoldSound()
    {
        if (audioSource == null) return;
        if (holdSound == null) return;
        if (isHoldingSoundPlaying) return;

        audioSource.clip = holdSound;
        audioSource.loop = true;
        audioSource.volume = holdVolume;
        audioSource.Play();

        isHoldingSoundPlaying = true;
    }

    void StopHoldSound()
    {
        if (audioSource == null) return;
        if (!isHoldingSoundPlaying) return;

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;

        isHoldingSoundPlaying = false;
    }

    void PlayRandomOpenSound()
    {
        if (openSounds == null || openSounds.Length == 0)
            return;

        AudioClip clip = openSounds[Random.Range(0, openSounds.Length)];

        if (clip == null)
            return;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, openVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, openVolume);
        }
    }
}