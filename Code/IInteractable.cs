using Sandbox;

public interface IInteractable
{
    // This function runs when the player presses "Use"
    void OnInteract( GameObject player );

    // This is the text shown in the UI (e.g., "Open Door", "Sit Down")
    string ActionName => "Interact"; 
}