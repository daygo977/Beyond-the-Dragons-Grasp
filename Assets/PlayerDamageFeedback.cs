using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

//Prevents one player from triggering another player cam shake when taking damage
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("References")]
    public Health playerHealth;
    public Transform cameraTransform;
    public Image lowHealthOverlay;

    [Header("Camera Shake")]
    public float shakeDuration = 0.15f;
    public float shakeStrength = 0.08f;

    [Header("Low Health Overlay")]
    public int lowHealthThreshold = 30;
    public float maxOverlayAlpha = 0.35f;
    public float overlayFadeSpeed = 6f;

    private int lastHealth;
    private Vector3 originalCameraLocalPosition;
    private Coroutine shakeRoutine;

    private void Start()
    {
        //Multiplayer edit
        FindLocalPlayerHealth();

        if (cameraTransform != null)
            originalCameraLocalPosition = cameraTransform.localPosition;

        if (playerHealth != null)
            lastHealth = playerHealth.CurrentHealth;

        SetOverlayAlpha(0f);
    }

    //Multiplayer edit, new logic
    private void Update()
    {
        if (playerHealth == null)
            FindLocalPlayerHealth();

        if (playerHealth == null)
            return;

        if (!IsLocalPlayersHealth())
        {
            SetOverlayAlpha(0f);
            return;
        }

        int current = playerHealth.CurrentHealth;

        if (lastHealth < 0)
            lastHealth = current;

        if (current < lastHealth)
            PlayDamageShake();

        UpdateLowHealthOverlay();

        lastHealth = current;
    }

    //Multiplayer new function,
    private void FindLocalPlayerHealth()
    {
        if (playerHealth != null)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.LocalClient == null)
            return;

        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        playerHealth = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Health>();
    }

    //Multiplayer new function, 
    private bool IsLocalPlayersHealth()
    {
        if (playerHealth == null)
            return false;

        if (!playerHealth.IsSpawned)
            return true;

        return playerHealth.IsOwner;
    }

    private void PlayDamageShake()
    {
        if (cameraTransform == null) return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            Vector3 randomOffset = Random.insideUnitSphere * shakeStrength;
            randomOffset.z = 0f;

            cameraTransform.localPosition = originalCameraLocalPosition + randomOffset;

            yield return null;
        }

        cameraTransform.localPosition = originalCameraLocalPosition;
        shakeRoutine = null;
    }

    private void UpdateLowHealthOverlay()
    {
        if (lowHealthOverlay == null) return;

        //Multiplayer edit
        float targetAlpha = playerHealth.CurrentHealth <= lowHealthThreshold ? maxOverlayAlpha : 0f;

        Color color = lowHealthOverlay.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * overlayFadeSpeed);
        lowHealthOverlay.color = color;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (lowHealthOverlay == null) return;

        Color color = lowHealthOverlay.color;
        color.a = alpha;
        lowHealthOverlay.color = color;
    }
}