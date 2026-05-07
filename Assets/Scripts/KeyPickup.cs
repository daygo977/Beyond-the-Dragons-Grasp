using System.Collections;
using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Prompt Text")]
    [TextArea]
    public string promptText = "Press E to pick up key";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    [Header("Pickup")]
    public float disableDelay = 1f;

    private bool pickedUp;

    public string GetPromptText()
    {
        if (pickedUp)
            return "";

        return promptText;
    }

    public void Interact()
    {
        if (pickedUp) return;

        pickedUp = true;

        if (GameFlags.Instance != null)
        {
            GameFlags.Instance.hasDoorKey = true;
        }

        StartCoroutine(PickupRoutine());
    }

    IEnumerator PickupRoutine()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
            rend.enabled = false;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound, pickupVolume);

        yield return new WaitForSeconds(disableDelay);

        gameObject.SetActive(false);
    }
}