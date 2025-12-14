using Sandbox;
using System.Linq;
public sealed class ThirdPersonCamera : Component
{
	[Property, Title("Target Player")] 
	public GameObject Player { get; set; }

	[Property, Group("First Person")] 
	public Vector3 EyeOffset { get; set; } = new Vector3( 0, 0, 72 );

	
	// NEW: Height when crouching (usually half of standing)
	[Property, Group("First Person")] 
	public Vector3 CrouchOffset { get; set; } = new Vector3( 0, 0, 36 );
	
	[Property, Group("First Person")]
	public float FieldOfView { get; set; } = 90.0f;

	private Angles _lookAngles;
	private bool _hasInitializedAngles = false;
	
	// Track current Z height for smoothing
	private float _currentHeight = 72.0f;

	protected override void OnStart()
	{
		Player = null;
		_currentHeight = EyeOffset.z + EyeHeightBoost;
	}

	protected override void OnUpdate()
	{
		// 1. Find Player
		if ( Player == null )
		{
			var simpleController = Scene.GetAllComponents<SimplePlayerController>()
				.FirstOrDefault( x => !x.IsProxy );
				
			if ( simpleController != null ) 
			{
				Player = simpleController.GameObject;
			}
			else 
			{
				var cc = Scene.GetAllComponents<CharacterController>()
					.FirstOrDefault( x => !x.IsProxy );
				if (cc != null) Player = cc.GameObject;
			}
			
			if ( Player == null ) return;
		}

		// 2. Initialize Angle
		if ( !_hasInitializedAngles )
		{
			var playerForward = Player.WorldRotation.Forward;
			_lookAngles = Rotation.LookAt( playerForward ).Angles();
			_lookAngles.pitch = 0f; 
			_hasInitializedAngles = true;
		}

		// 3. Input
		_lookAngles += Input.AnalogLook;
		_lookAngles.pitch = MathX.Clamp( _lookAngles.pitch, -89f, 89f );

		// 4. Calculate Height (Smooth Duck)
		// Check for duck input
		bool isDucking = Input.Down( "Duck" );
		float baseHeight = isDucking ? CrouchOffset.z : EyeOffset.z;
		float targetZ = baseHeight + EyeHeightBoost;
		
		// Smoothly lerp the camera height so it doesn't snap instantly
		_currentHeight = MathX.Lerp( _currentHeight, targetZ, Time.Delta * 10.0f );

		// 5. Apply Transform
		WorldRotation = _lookAngles.ToRotation();
		
		// Combine player position with our smoothed height
		WorldPosition = Player.WorldPosition + new Vector3( EyeOffset.x, EyeOffset.y, _currentHeight );

		// 6. Set FOV
		var camera = Components.Get<CameraComponent>();
		if ( camera != null )
		{
			camera.FieldOfView = FieldOfView;
		}

		// 7. Hide Body
		UpdateBodyVisibility( false );
	}
	
	void UpdateBodyVisibility( bool visible )
	{
		var renderers = Player.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var renderer in renderers )
		{
			renderer.RenderType = visible ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.ShadowsOnly;
		}
	}
}
