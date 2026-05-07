using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Collections;

//Multiplayer edit, whole class logic change
public class PlayerModelVisibility : NetworkBehaviour
{
    [Header("First Person")]
    public GameObject firstPersonHands;

    [Header("Third Person Models")]
    public GameObject[] thirdPersonModels = new GameObject[4];

    public Animator ActiveThirdPersonAnimator { get; private set; }
    public NetworkAnimator ActiveThirdPersonNetworkAnimator { get; private set; }

    private NetworkVariable<int> selectedModelIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        selectedModelIndex.OnValueChanged += OnModelIndexChanged;

        if (IsOwner)
        {
            FixedString64Bytes authPlayerId = AuthenticationService.Instance.PlayerId;
            SubmitPlayerIdServerRpc(authPlayerId);
        }

        ApplyVisibility();
    }

    public override void OnNetworkDespawn()
    {
        selectedModelIndex.OnValueChanged -= OnModelIndexChanged;
    }

    private void OnModelIndexChanged(int oldValue, int newValue)
    {
        ApplyVisibility();
    }

    [ServerRpc]
    private void SubmitPlayerIdServerRpc(FixedString64Bytes authPlayerId)
    {
        int modelIndex = GetModelIndexFromLobby(authPlayerId.ToString());
        selectedModelIndex.Value = modelIndex;
    }

    private int GetModelIndexFromLobby(string authPlayerId)
    {
        if (UnityLobbyManager.Instance != null &&
            UnityLobbyManager.Instance.CurrentLobby != null &&
            UnityLobbyManager.Instance.CurrentLobby.Players != null)
        {
            for (int i = 0; i < UnityLobbyManager.Instance.CurrentLobby.Players.Count; i++)
            {
                Player lobbyPlayer = UnityLobbyManager.Instance.CurrentLobby.Players[i];

                if (lobbyPlayer.Id == authPlayerId)
                    return Mathf.Clamp(i, 0, thirdPersonModels.Length - 1);
            }
        }

        return Mathf.Clamp((int)OwnerClientId, 0, thirdPersonModels.Length - 1);
    }

    private void ApplyVisibility()
    {
        if (firstPersonHands != null)
            firstPersonHands.SetActive(IsOwner);

        int modelIndex = selectedModelIndex.Value;

        if (modelIndex < 0)
            modelIndex = Mathf.Clamp((int)OwnerClientId, 0, thirdPersonModels.Length - 1);

        ActiveThirdPersonAnimator = null;
        ActiveThirdPersonNetworkAnimator = null;

        for (int i = 0; i < thirdPersonModels.Length; i++)
        {
            GameObject model = thirdPersonModels[i];

            if (model == null)
                continue;

            // IMPORTANT:
            // Keep model objects active so Animator and NetworkAnimator keep running.
            model.SetActive(true);

            bool isSelectedModel = i == modelIndex;

            if (isSelectedModel)
            {
                ActiveThirdPersonAnimator = model.GetComponentInChildren<Animator>(true);
                ActiveThirdPersonNetworkAnimator = model.GetComponentInChildren<NetworkAnimator>(true);
            }

            // Owner should not see their own third-person body.
            // Remote players should see only the selected third-person body.
            bool renderVisible = !IsOwner && isSelectedModel;

            SetRenderersVisible(model, renderVisible);
        }
    }
    
    //Multiplayer new function,
    //Signature: 05/07/2026 11:31AM
    private void SetRenderersVisible(GameObject root, bool visible)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}