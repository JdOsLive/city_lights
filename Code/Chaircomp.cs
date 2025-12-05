using Sandbox;

public sealed class InteractiveChair : BaseChair
{
	/// <summary>
	/// The maximum distance from which the player can interact with the chair
	/// </summary>
	[Property] public float InteractionDistance { get; set; } = 100f;

	/// <summary>
	/// The text displayed when the player can sit down
	/// </summary>
	[Property] public string SitPrompt { get; set; } = "Press E to Sit";

	/// <summary>
	/// The text displayed when the player can stand up
	/// </summary>
	[Property] public string LeavePrompt { get; set; } = "Press E to Stand";

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// Only execute on client
		if (!IsProxy)
		{
			CheckForPlayerInteraction();
		}
	}

	private void CheckForPlayerInteraction()
	{
		// Get the local player controller
		var playerController = Scene.GetAllComponents<PlayerController>()
			.FirstOrDefault(x => x.Network.IsOwner);

		if (playerController == null)
			return;

		var playerPos = playerController.WorldPosition;
		var chairPos = WorldPosition;
		var distance = Vector3.DistanceBetween(playerPos, chairPos);

		// Check if player is within range
		if (distance <= InteractionDistance)
		{
			// Show interaction prompt
			if (IsOccupied && GetOccupant() == playerController)
			{
				// Player is already sitting - show "Stand" prompt
				Gizmo.Draw.ScreenText(LeavePrompt, new Vector2(Screen.Width / 2, Screen.Height - 100));
				
				// Check for E key press to exit
				if (Input.Pressed("Use") || Input.Pressed("e"))
				{
					AskToLeave(playerController);
				}
			}
			else if (!IsOccupied)
			{
				// Chair is free - show "Sit" prompt
				Gizmo.Draw.ScreenText(SitPrompt, new Vector2(Screen.Width / 2, Screen.Height - 100));
				
				// Check for E key press to sit
				if (Input.Pressed("Use") || Input.Pressed("e"))
				{
					if (CanEnter(playerController))
					{
						Sit(playerController);
					}
				}
			}
		}
	}

	public override bool CanEnter(PlayerController player)
	{
		// Player can enter if chair is free
		return !IsOccupied;
	}

	public override bool CanLeave(PlayerController player)
	{
		// Player can always leave
		return true;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		// Draw interaction radius in editor
		if (!Gizmo.IsSelected)
			return;

		Gizmo.Draw.Color = IsOccupied ? Color.Red : Color.Green;
		Gizmo.Draw.LineSphere(WorldPosition, InteractionDistance);
		
		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.Arrow(WorldPosition, WorldPosition + WorldRotation.Forward * 50f, 5f);
	}
}