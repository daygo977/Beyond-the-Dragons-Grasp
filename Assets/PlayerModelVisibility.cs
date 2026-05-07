using UnityEngine;

public class PlayerModelVisibility : MonoBehaviour
{
    [Header("Local Player")]
    public bool isLocalPlayer = true;

    [Header("Models")]
    public GameObject firstPersonHands;
    public GameObject thirdPersonModel;

    private Renderer[] thirdPersonRenderers;

    void Awake()
    {
        if (thirdPersonModel != null)
            thirdPersonRenderers = thirdPersonModel.GetComponentsInChildren<Renderer>(true);
    }

    void Start()
    {
        ApplyVisibility();
    }

    public void ApplyVisibility()
    {
        if (!Application.isPlaying)
            return;

        if (firstPersonHands != null)
            firstPersonHands.SetActive(isLocalPlayer);

        if (thirdPersonRenderers != null)
        {
            foreach (Renderer rend in thirdPersonRenderers)
                rend.enabled = !isLocalPlayer;
        }
    }

    public void SetLocalPlayer(bool local)
    {
        isLocalPlayer = local;
        ApplyVisibility();
    }
}