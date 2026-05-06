using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();

        if (cameraTransform != null)
            originalCameraLocalPosition = cameraTransform.localPosition;

        if (playerHealth != null)
            lastHealth = playerHealth.currentHealth;

        SetOverlayAlpha(0f);
    }

    void Update()
    {
        if (playerHealth == null) return;

        if (playerHealth.currentHealth < lastHealth)
        {
            PlayDamageShake();
        }

        UpdateLowHealthOverlay();

        lastHealth = playerHealth.currentHealth;
    }

    void PlayDamageShake()
    {
        if (cameraTransform == null) return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
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

    void UpdateLowHealthOverlay()
    {
        if (lowHealthOverlay == null) return;

        float targetAlpha = playerHealth.currentHealth <= lowHealthThreshold ? maxOverlayAlpha : 0f;

        Color color = lowHealthOverlay.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * overlayFadeSpeed);
        lowHealthOverlay.color = color;
    }

    void SetOverlayAlpha(float alpha)
    {
        if (lowHealthOverlay == null) return;

        Color color = lowHealthOverlay.color;
        color.a = alpha;
        lowHealthOverlay.color = color;
    }
}