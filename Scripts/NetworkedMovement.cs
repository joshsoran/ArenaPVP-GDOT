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

    public Vector3 _targetVelocity = Vector3.Zero;
    
    public override void _Ready()
    {
        if(Multiplayer.IsServer())
        {
            return;
        }

        //Input.MouseMode = Input.MouseModeEnum.Captured;

        if(NetworkId == Multiplayer.GetUniqueId())
        {
            GD.Print("Local Camera Set");
            LocalCamera.Current = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if(NetworkId != Multiplayer.GetUniqueId())
        {
            return;
        }

        if(@event is InputEventMouseMotion eventMouseMotion)
        {
            float VerticalMouseMovement = eventMouseMotion.Relative.X;
            RotateY(Mathf.DegToRad(-VerticalMouseMovement * MouseSensitivity));
            float HorizontalMouseMovement = eventMouseMotion.Relative.Y;
            LocalCameraMount.RotateX(Mathf.DegToRad(-HorizontalMouseMovement * MouseSensitivity));
            
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(NetworkId != Multiplayer.GetUniqueId())
        {
            return;
        }
        //gravity
        if(!IsOnFloor())
        {
            _targetVelocity.Y -= Gravity * (float)delta;
        }

        if(Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            _targetVelocity.Y = JumpVelocity;
        }

        Vector2 InputDirection = Input.GetVector("move_right", "move_left", "move_down", "move_up");

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

        foreach (var Peer in Multiplayer.GetPeers())
        {
            if(Peer == NetworkId)
            {
                continue;
            }
            if(Peer == 1)
            {
                continue;
            }
            RpcId(Peer, MethodName.ReplicatePosition, Transform);
        }


    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ReplicatePosition(Transform3D NetworkTransform)
    {
        Transform = NetworkTransform;
    }

}
