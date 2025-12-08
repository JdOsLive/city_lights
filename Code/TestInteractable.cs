using Sandbox;

namespace CityLights;

public sealed class TestInteractable : Component, IInteractable
{
    [Property] public string ActionName { get; set; } = "Inspect";

    protected override void OnStart()
    {
        // Add a visible model to this GameObject
        var renderer = Components.GetOrCreate<ModelRenderer>();
        renderer.Model = Model.Load("models/citizen_props/crate01.vmdl");
    }

    // Match the interface signature in your project
    public void OnInteract(GameObject interactor)
    {
        Log.Info($"{interactor} interacted with the test object!");
    }
}
