using UnityEngine;

public class LowHealthShaderController : MonoBehaviour
{
    public Health playerHealth;
    public Material lowHealthMaterial;

    [Header("Health")]
    public int lowHealthThreshold = 30;

    [Header("Shader")]
    public string healthFadeParameter = "_HealthFade";
    public float fadeSpeed = 5f;

    private float currentFade;

    void Start()
    {
        if (lowHealthMaterial != null)
            lowHealthMaterial.SetFloat(healthFadeParameter, 0f);
    }

    void Update()
    {
        if (playerHealth == null || lowHealthMaterial == null)
            return;

        float targetFade = playerHealth.currentHealth <= lowHealthThreshold ? 1f : 0f;

        currentFade = Mathf.Lerp(currentFade, targetFade, Time.deltaTime * fadeSpeed);

        lowHealthMaterial.SetFloat(healthFadeParameter, currentFade);
    }
}