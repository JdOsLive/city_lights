using Sandbox;
using System.Linq;

/// <summary>
/// Simple networked text chat manager. Attach this to a world object along with a <see cref="ChatPanel"/>
/// to give players a shared chat feed.
/// </summary>
public sealed class ChatSystem : Component
{
    [Property] public ChatPanel Panel { get; set; }

    [Property, Group("Chat")] public int MaxMessages { get; set; } = 50;

    /// <summary>
    /// Input action name used to open the chat box.
    /// </summary>
    [Property, Group("Chat")] public string ChatAction { get; set; } = "Chat";

    protected override void OnStart()
    {
        // Try to find a panel in this object or elsewhere in the scene if one isn't wired up.
        if ( Panel == null )
        {
            Panel = GameObject.Components.Get<ChatPanel>( FindMode.EverythingInSelfAndChildren )
                 ?? Scene.GetAllComponents<ChatPanel>().FirstOrDefault();
        }
    }

    protected override void OnUpdate()
    {
        if ( Panel == null )
            return;

        if ( Input.Pressed( ChatAction ) )
        {
            Panel.BeginChat();
        }

        if ( Panel.IsTyping && Input.Pressed( "Escape" ) )
        {
            Panel.CancelChat();
        }
    }

    /// <summary>
    /// Called by the chat panel when a user presses Enter.
    /// </summary>
    public void SubmitMessage( string text )
    {
        var trimmed = text?.Trim();
        if ( string.IsNullOrEmpty( trimmed ) )
            return;

        var sender = Connection.Local?.DisplayName ?? "Player";
        RpcBroadcastMessage( sender, trimmed );
    }

    [Rpc.Broadcast]
    private void RpcBroadcastMessage( string sender, string message )
    {
        Panel ??= GameObject.Components.Get<ChatPanel>( FindMode.EverythingInSelfAndChildren );
        Panel?.AddMessage( sender, message, MaxMessages );
    }
}
