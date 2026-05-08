using UnityEngine;
using Unity.Netcode;

public class EscapeZoneTrigger : MonoBehaviour
{
    [Header("References")]
    public EscapeDoorInteractable escapeDoor;

    [Header("Detection")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        NetworkObject playerNetworkObject = other.GetComponentInParent<NetworkObject>();

        if (escapeDoor != null && playerNetworkObject != null)
        {
            escapeDoor.SetPlayerInEscapeZone(playerNetworkObject.OwnerClientId, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        NetworkObject playerNetworkObject = other.GetComponentInParent<NetworkObject>();

        if (escapeDoor != null && playerNetworkObject != null)
        {
            escapeDoor.SetPlayerInEscapeZone(playerNetworkObject.OwnerClientId, false);
        }
    }
}