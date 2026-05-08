using UnityEngine;
using Unity.Netcode;

public class PuzzleLever : NetworkBehaviour, IInteractable
{
    [Header("Lever Settings")]
    [SerializeField] private PuzzleManager puzzleManager;
    [SerializeField] private PuzzleManager.LeverType leverType;

    [Header("Prompt")]
    [TextArea]
    [SerializeField] private string promptText = "Press E to pull lever";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pullSound;
    [SerializeField] private float pullVolume = 1f;

    private NetworkVariable<bool> hasBeenPulled = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);
    }

    public string GetPromptText()
    {
        if (hasBeenPulled.Value)
            return "";

        return promptText;
    }

    public void Interact()
    {
        Debug.Log($"{name}: lever interact called. IsServer={IsServer}, IsSpawned={IsSpawned}");

        if (hasBeenPulled.Value)
            return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
                PullLeverServerSide();
            else
                PullLeverServerRpc();

            return;
        }

        PullLeverServerSide();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PullLeverServerRpc()
    {
        PullLeverServerSide();
    }

    private void PullLeverServerSide()
    {
        if (hasBeenPulled.Value)
            return;

        hasBeenPulled.Value = true;

        Debug.Log($"{name}: pulled lever type {leverType}");

        PlayPullSoundClientRpc();

        if (puzzleManager != null)
        {
            puzzleManager.PullLever(leverType);
        }
        else
        {
            Debug.LogWarning($"{name}: PuzzleManager is missing.");
        }
    }

    public void ResetLeverState()
    {
        if (!IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;

        hasBeenPulled.Value = false;
    }

    [ClientRpc]
    private void PlayPullSoundClientRpc()
    {
        if (audioSource == null || pullSound == null)
            return;

        audioSource.PlayOneShot(pullSound, pullVolume);
    }
}