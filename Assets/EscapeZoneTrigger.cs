using UnityEngine;

public class EscapeZoneTrigger : MonoBehaviour
{
    public EscapeDoorInteractable escapeDoor;
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (escapeDoor != null)
            escapeDoor.SetPlayerInEscapeZone(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (escapeDoor != null)
            escapeDoor.SetPlayerInEscapeZone(false);
    }
}