using Sandbox;
using System;

public sealed class ColorChanger : Component, IInteractable
{
    // The UI will show "[E] Change Color"
    public string ActionName => "Change Color";

    [Rpc.Broadcast] // Broadcast ensures everyone sees the color change
    public void OnInteract( GameObject player )
    {
        var renderer = Components.Get<ModelRenderer>();
        if ( renderer != null )
        {
            renderer.Tint = Color.Random;
        }
        
        // Optional: Play a sound
        Sound.Play( "ui.button.press", WorldPosition );
    }
}