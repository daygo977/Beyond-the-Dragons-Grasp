using UnityEngine;

public class PuzzleLever : MonoBehaviour
{
    [Header("Lever Settings")]
    [SerializeField] private PuzzleManager puzzleManager;
    [SerializeField] private PuzzleManager.LeverType leverType;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject promptTextObject;

    [Header("Visual Lever Movement")]
    [SerializeField] private Transform leverHandle;
    [SerializeField] private Vector3 pulledRotation = new Vector3(-45f, 0f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pullSound;
    [SerializeField] private float pullVolume = 1f;

    private bool playerInRange = false;
    private bool hasBeenPulled = false;
    private Quaternion startingRotation;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (leverHandle != null)
        {
            startingRotation = leverHandle.localRotation;
        }

        if (promptTextObject != null)
        {
            promptTextObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange && !hasBeenPulled && Input.GetKeyDown(interactKey))
        {
            PullLever();
        }
    }

    private void PullLever()
    {
        hasBeenPulled = true;

        if (promptTextObject != null)
        {
            promptTextObject.SetActive(false);
        }

        if (leverHandle != null)
        {
            leverHandle.localRotation = Quaternion.Euler(pulledRotation);
        }

        PlayLeverSound();

        if (puzzleManager != null)
        {
            puzzleManager.PullLever(leverType);
        }
        else
        {
            Debug.LogWarning("PuzzleManager is missing on " + gameObject.name);
        }
    }

    private void PlayLeverSound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource assigned on " + gameObject.name);
            return;
        }

        if (pullSound == null)
        {
            Debug.LogWarning("No pull sound assigned on " + gameObject.name);
            return;
        }

        audioSource.PlayOneShot(pullSound, pullVolume);
    }

    public void ResetLever()
    {
        hasBeenPulled = false;

        if (leverHandle != null)
        {
            leverHandle.localRotation = startingRotation;
        }

        if (playerInRange && promptTextObject != null)
        {
            promptTextObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (!hasBeenPulled && promptTextObject != null)
            {
                promptTextObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (promptTextObject != null)
            {
                promptTextObject.SetActive(false);
            }
        }
    }
}