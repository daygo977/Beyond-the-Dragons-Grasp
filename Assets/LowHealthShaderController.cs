using UnityEngine;
using Unity.Netcode;

//Local screen feedback in multiplayer
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

    private void Start()
    {
        //Multiplayer edit
        FindLocalPlayerHealth();

        if (lowHealthMaterial != null)
            lowHealthMaterial.SetFloat(healthFadeParameter, 0f);
    }

    //Multiplayer edit, new logic
    private void Update()
    {
        if (playerHealth == null)
            FindLocalPlayerHealth();

        if (playerHealth == null || lowHealthMaterial == null)
            return;

        if (!IsLocalPlayersHealth())
        {
            SetShaderFade(0f);
            return;
        }

        float targetFade = playerHealth.CurrentHealth <= lowHealthThreshold ? 1f : 0f;

        currentFade = Mathf.Lerp(currentFade, targetFade, Time.deltaTime * fadeSpeed);

        SetShaderFade(currentFade);
    }

    //Multiplayer new function
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

    //Multiplayer new function
    private bool IsLocalPlayersHealth()
    {
        if (playerHealth == null)
            return false;

        if (!playerHealth.IsSpawned)
            return true;

        return playerHealth.IsOwner;
    }

    //Multiplayer new function
    private void SetShaderFade(float value)
    {
        if (lowHealthMaterial == null)
            return;

        lowHealthMaterial.SetFloat(healthFadeParameter, value);
    }
}