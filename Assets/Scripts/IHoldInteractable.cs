public interface IHoldInteractable : IInteractable
{
    void HoldInteract(float deltaTime);
    void ResetHold();
}