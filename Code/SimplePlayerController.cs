using Sandbox;
using Sandbox.Citizen;
using System.Linq; // Needed for cleanup logic

public sealed class SimplePlayerController : Component
{
    [Property, Group("Movement")] public float MoveSpeed { get; set; } = 140f;
    [Property, Group("Movement")] public float RunSpeed { get; set; } = 280f;
    [Property, Group("Movement")] public float CrouchSpeed { get; set; } = 80f; 
    [Property, Group("Movement")] public float JumpPower { get; set; } = 300f;
    
    [Property, Group("Smoothing")] public float RotationSpeed { get; set; } = 5.0f; 
    [Property, Group("Smoothing")] public float Acceleration { get; set; } = 10.0f; 
    [Property, Group("Smoothing")] public float SlideFriction { get; set; } = 2.0f; 

    [RequireComponent] public CharacterController CharacterController { get; set; }
    [Property] public SkinnedModelRenderer TargetRenderer { get; set; }

    // AUDIO SETTINGS
    [Property, Group("Audio")] public float FootstepVolume { get; set; } = 1.0f;
    [Property, Group("Audio")] public float StepDistance { get; set; } = 80.0f; 

    // SYNC VARIABLES
    [Sync] public bool IsGroundedSync { get; set; }
    [Sync] public float MoveSpeedSync { get; set; }
    [Sync] public Vector3 LookDirectionSync { get; set; }
    [Sync] public bool IsDuckingSync { get; set; }

    // --- NEW: CLOTHING SYNC ---
    // This string holds the JSON data of what the player is wearing
    [Sync] public string ClothingJson { get; set; } 
    private bool _clothingApplied = false;

    // Internal
    private float _distTraveled = 0f;

    protected override void OnStart()
    {
        // If we are the owner of this object (the player controlling it)
        if ( !IsProxy )
        {
            // 1. Get our avatar data from Steam/s&box
            var container = ClothingContainer.CreateFromLocalUser();
            
            // 2. Apply it to ourselves immediately so we see it
            container.Apply( TargetRenderer );

            // 3. Serialize it to the network variable so others can see it
            ClothingJson = container.Serialize();
            _clothingApplied = true;
        }
    }

    protected override void OnUpdate()
    {
        // --- NEW: CLOTHING SYNC LOGIC ---
        // If we are a Proxy (other player looking at this guy) and we haven't dressed him yet...
        if ( IsProxy && !_clothingApplied && !string.IsNullOrEmpty( ClothingJson ) )
        {
            // Load the clothes from the synced string
            var container = new ClothingContainer();
            container.Deserialize( ClothingJson );
            container.Apply( TargetRenderer );
            _clothingApplied = true;
        }

        // 1. Update Animations & Visibility
        UpdateAnimations();

        if ( IsProxy ) return;
        
        if ( CharacterController == null || Scene.Camera == null ) return;

        // --- RESPAWN ---
        if ( Input.Pressed( "Reload" ) ) 
        {
            var spawnPoint = Scene.GetAllComponents<SpawnPoint>().FirstOrDefault();
            if ( spawnPoint != null )
            {
                CharacterController.Velocity = Vector3.Zero;
                WorldPosition = spawnPoint.WorldPosition;
                WorldRotation = spawnPoint.WorldRotation;
            }
            return; 
        }

        // --- CROUCH LOGIC ---
        var isDucking = Input.Down( "Duck" );
        if ( isDucking ) CharacterController.Height = 32;
        else CharacterController.Height = 64;

        // --- MOVEMENT ---
        var wishSpeed = MoveSpeed;
        if ( isDucking ) wishSpeed = CrouchSpeed;
        else if ( Input.Down( "Run" ) ) wishSpeed = RunSpeed;

        var moveInput = Input.AnalogMove;
        var camRot = Scene.Camera.WorldRotation;
        var forward = camRot.Forward.WithZ( 0 ).Normal;
        var right = camRot.Right.WithZ( 0 ).Normal;
        var wishDir = (forward * moveInput.x - right * moveInput.y).Normal;

        if ( !CharacterController.IsOnGround )
            CharacterController.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
        else
            CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );

        if ( CharacterController.IsOnGround && Input.Pressed( "Jump" ) )
        {
            CharacterController.Punch( Vector3.Up * JumpPower );
            BroadcastJumpSound( WorldPosition );
        }

        // --- SLIDE LOGIC ---
        float currentAccel = Acceleration;
        var horizontalSpeed = CharacterController.Velocity.WithZ(0).Length;
        if ( isDucking && horizontalSpeed > CrouchSpeed + 10.0f )
        {
            currentAccel = SlideFriction;
        }

        if ( wishDir.Length > 0 )
        {
            var targetRot = Rotation.LookAt( wishDir, Vector3.Up );
            WorldRotation = Rotation.Slerp( WorldRotation, targetRot, Time.Delta * RotationSpeed );

            var currentZ = CharacterController.Velocity.z;
            var targetVelocity = wishDir * wishSpeed;
            var currentVelocity = CharacterController.Velocity.WithZ(0);
            var smoothVelocity = Vector3.Lerp( currentVelocity, targetVelocity, Time.Delta * currentAccel );
            CharacterController.Velocity = smoothVelocity.WithZ( currentZ );
        }
        else
        {
            var currentZ = CharacterController.Velocity.z;
            var velocityNoZ = CharacterController.Velocity.WithZ(0);
            CharacterController.Velocity = Vector3.Lerp( velocityNoZ, Vector3.Zero, Time.Delta * currentAccel ).WithZ( currentZ );
        }

        CharacterController.Move();

        // --- FOOTSTEPS ---
        UpdateFootsteps();

        // --- SYNC ---
        IsGroundedSync = CharacterController.IsOnGround;
        MoveSpeedSync = CharacterController.Velocity.WithZ(0).Length;
        if ( wishDir.Length > 0 ) LookDirectionSync = WorldPosition + wishDir * 100f;
        IsDuckingSync = isDucking;
    }

    void UpdateFootsteps()
    {
        float speed = 0f;
        bool isGrounded = false;

        if ( !IsProxy )
        {
            if ( CharacterController != null )
            {
                speed = CharacterController.Velocity.WithZ(0).Length;
                isGrounded = CharacterController.IsOnGround;
            }
        }
        else
        {
            speed = MoveSpeedSync;
            isGrounded = IsGroundedSync;
        }

        if ( isGrounded && speed > 5.0f )
        {
            _distTraveled += speed * Time.Delta;
            var currentStepDist = (speed > MoveSpeed + 10) ? StepDistance * 1.2f : StepDistance;
            if ( _distTraveled > currentStepDist )
            {
                _distTraveled = 0;
                DoFootstep();
            }
        }
    }

    void DoFootstep()
    {
        var tr = Scene.Trace.Ray( WorldPosition + Vector3.Up * 20, WorldPosition + Vector3.Down * 20 )
            .IgnoreGameObjectHierarchy( GameObject )
            .Run();

        if ( tr.Hit && tr.Surface != null )
        {
            // Simple check for surface type if desired, otherwise concrete
            Sound.Play( "footstep-concrete", tr.EndPosition ).Volume = FootstepVolume;
        }
        else
        {
            Sound.Play( "footstep-concrete", WorldPosition ).Volume = FootstepVolume;
        }
    }

    [Rpc.Broadcast]
    void BroadcastJumpSound( Vector3 pos )
    {
        Sound.Play( "footstep-concrete", pos ).Volume = FootstepVolume;
    }

    void UpdateAnimations()
    {
        if ( TargetRenderer == null ) return;

        // --- VISIBILITY FIX: Force body visible for other players ---
        if ( IsProxy )
        {
            var allRenderers = Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
            foreach ( var r in allRenderers )
            {
                if ( r.RenderType != ModelRenderer.ShadowRenderType.On )
                {
                    r.RenderType = ModelRenderer.ShadowRenderType.On;
                }
            }
        }

        var helper = new CitizenAnimationHelper();
        helper.Target = TargetRenderer;
        
        if ( !IsProxy )
        {
            helper.WithVelocity( CharacterController.Velocity );
            helper.WithWishVelocity( CharacterController.Velocity );
            helper.IsGrounded = CharacterController.IsOnGround;
            helper.DuckLevel = Input.Down( "Duck" ) ? 1.0f : 0.0f;
        }
        else
        {
            helper.IsGrounded = IsGroundedSync;
            var velocity = WorldRotation.Forward * MoveSpeedSync;
            helper.WithVelocity( velocity );
            helper.WithWishVelocity( velocity ); 
            helper.DuckLevel = IsDuckingSync ? 1.0f : 0.0f;
        }
        
        if ( !IsProxy ) 
        {
        }
        else if ( LookDirectionSync != Vector3.Zero )
        {
            helper.WithLook( LookDirectionSync );
        }
    }
}