using Godot;
using Godot.NativeInterop;
using System;

public partial class NetworkedMovement : CharacterBody3D
{

    [Export]
    public float Speed = 5.0f;

    [Export]
    public float JumpVelocity = 4.5f;

    [Export]
    public float MouseSensitivity = 0.1f;
    
    [Export]
    public Camera3D LocalCamera;
    [Export]
    public Node3D LocalCameraMount;
    public int NetworkId;

    private float Gravity = 9.81f;



    [Export]
    private MultiplayerSynchronizer MultiplayerInput;

    public Vector3 _targetVelocity = Vector3.Zero;

    public override void _Ready()
    {
        if(Multiplayer.IsServer())
        {
            return;
        }

        GD.Print($"Setting Network ID for movement to {NetworkId}");
        MultiplayerInput.SetMultiplayerAuthority(NetworkId);
        this.SetMultiplayerAuthority(NetworkId);
        MultiplayerInput.Call("postIntialization");
        if(NetworkId == Multiplayer.GetUniqueId())
        {
            GD.Print($"Local Camera Set");
            LocalCamera.Current = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseMotion eventMouseMotion)
        {
            float vertical = eventMouseMotion.Relative.X;
            RotateY(Mathf.DegToRad(-vertical * MouseSensitivity));
            float horizontal = eventMouseMotion.Relative.Y;
            LocalCameraMount.RotateX(Mathf.DegToRad(-horizontal * MouseSensitivity));
            
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        //gravity
        if(!IsOnFloor())
        {
            _targetVelocity.Y -= Gravity * (float)delta;
        }

        //jump
        if(MultiplayerInput.Get("jumping").AsBool() && IsOnFloor())
        {
            _targetVelocity.Y = JumpVelocity;
        }

        MultiplayerInput.Set("jumping", false);

        Vector2 InputDirection = MultiplayerInput.Get("direction").AsVector2();

        Vector3 Direction = (Transform.Basis * new Vector3(InputDirection.X, 0, InputDirection.Y)).Normalized();

        if(Direction.IsZeroApprox())
        {
            _targetVelocity.X = Direction.X * Speed;
            _targetVelocity.Z = Direction.Z * Speed;
        }
        else
        {
            _targetVelocity.X = Mathf.Lerp(Direction.X, 0, Speed);
            _targetVelocity.Z = Mathf.Lerp(Direction.Z, 0, Speed);
        }

        Velocity = _targetVelocity;
        MoveAndSlide();
    }
}
