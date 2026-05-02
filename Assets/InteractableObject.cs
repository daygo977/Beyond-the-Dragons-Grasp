using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [TextArea]
    public string promptText = "Press E to interact";

    public void Interact()
    {
        gameObject.SetActive(false);
    }
}