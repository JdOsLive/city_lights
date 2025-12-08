using Sandbox;
using System; // <--- FIX 1: Needed for "Guid"

public sealed class PlayerInteractor : Component
{
    [Property] public float InteractDistance { get; set; } = 80f;

    // We expose this so the UI can read it
    public IInteractable CurrentInteractable { get; private set; }

    protected override void OnUpdate()
    {
        // Only the local player (Owner) should scan for objects
        if ( IsProxy ) return;

        // 1. Scan for objects
        UpdateRaycast();

        // 2. Handle Input
        if ( CurrentInteractable != null && Input.Pressed( "use" ) )
        {
            var targetObject = (CurrentInteractable as Component)?.GameObject;
            if ( targetObject != null )
            {
                // Send the network message
                CmdTryInteract( targetObject.Id );
            }
        }
    }

   private void UpdateRaycast()
    {
        var cam = Scene.Camera;
        if ( cam == null ) return;

        var tr = Scene.Trace.Ray( cam.WorldPosition, cam.WorldPosition + cam.WorldRotation.Forward * InteractDistance )
            .IgnoreGameObjectHierarchy( GameObject ) // Don't hit yourself
            .Run();

        if ( tr.Hit )
        {
            CurrentInteractable = tr.GameObject.Components.Get<IInteractable>();
        }
        else
        {
            CurrentInteractable = null;
        }
    }

    // FIX 2: Replaced [Rpc.Cmd] with [Broadcast]
    // This sends the function call to everyone (including the Server)
    [Rpc.Broadcast] 
    private void CmdTryInteract( Guid targetId )
    {
        // FIX 3: Server Check
        // We only want the SERVER (Host) to actually run the interaction logic.
        // If we don't check this, every client tries to interact, which causes desync/errors.
        if ( !Networking.IsHost ) return;

        // Find the object on the server by its ID
        var targetObj = Scene.Directory.FindByGuid( targetId );
        if ( targetObj == null ) return;

        // Get the interface
        var interactable = targetObj.Components.Get<IInteractable>();
        
        // Double check distance on server (anti-cheat/validation)
        if ( targetObj.WorldPosition.Distance( WorldPosition ) > InteractDistance * 2f ) 
            return;

        // Run the logic!
        interactable?.OnInteract( GameObject );
    }
}