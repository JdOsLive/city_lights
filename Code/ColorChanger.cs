using Sandbox;

public sealed class ColorChanger : Component, IInteractable
{
    public string ActionName => "Change Color";

    [Rpc.Broadcast] // <--- This MUST be Rpc.Broadcast, not just Broadcast
    public void OnInteract( GameObject player )
    {
        var renderer = Components.Get<ModelRenderer>();
        if ( renderer != null )
        {
            renderer.Tint = Color.Random;
        }
        Sound.Play( "ui.button.press", WorldPosition );
    }
}