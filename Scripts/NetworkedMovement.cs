using Godot;
using Godot.NativeInterop;
using System;

public partial class NetworkedMovement : CharacterBody3D
{
    // Exports
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

    // Publics
    public int NetworkId;
    public Vector3 _targetVelocity = Vector3.Zero;
    public Vector2 InputDirection = Vector2.Zero;
    public bool bJustJumped = false;    
    // Privates
    private float Gravity = 9.81f;
    private Area3D _area3D; // for weapon detection


    // weapon collision detection
    private void OnBodyEntered(Node3D body)
    {
        // if enemy
        if(body.IsInGroup("Enemy"))
        {
            body.Call("td_takeDamage", 10);
            GD.Print($"Enemy HP: {body.Get("td_current_health")}");
        }
    }

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

        _area3D = GetNode<Area3D>("knight/Node/Area3D"); // Adjust path if necessary
        _area3D.BodyEntered += OnBodyEntered;
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
        if(NetworkId == 1)
        {
            return;
        }

        ProcessMovement(InputDirection, bJustJumped, delta);

        if(NetworkId != Multiplayer.GetUniqueId())
        {
            return;
        }

        InputDirection = Input.GetVector("move_right", "move_left", "move_down", "move_up");
        bJustJumped = Input.IsActionJustPressed("jump");


        //process movement using variables from the server if we are replicating and locally if we are not.
        //maybe we can always use network variables
        
        //rpc this to everyone and run locally

        foreach (var Peer in Multiplayer.GetPeers())
        {

            if(Peer == 1)
            {
                continue;
            }
            RpcId(Peer, MethodName.ReplicateInput, InputDirection, bJustJumped);

            if(Peer == NetworkId)
            {
                continue;
            }
            RpcId(Peer, MethodName.ReplicateLook, Rotation);
            
            
        }

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ReplicateInput(Vector2 _InputDirection, bool _bJustJumped)
    {
        InputDirection = _InputDirection;
        bJustJumped = _bJustJumped;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ReplicateLook(Vector3 LookRotation)
    {
        Rotation = LookRotation;
    }

    //We should probably implement a timer which checks the server side position of the palyer and updated it from network.cs

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ProcessMovement(Vector2 _InputDirection, bool _bJustJumped, double delta)
    {
                //gravity
        if(!IsOnFloor())
        {
            _targetVelocity.Y -= Gravity * (float)delta;
        }

        if(_bJustJumped && IsOnFloor())
        {
            _targetVelocity.Y = JumpVelocity;
        }

        Vector3 Direction = (Transform.Basis * new Vector3(_InputDirection.X, 0, _InputDirection.Y)).Normalized();

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
        //we should only replicated the direction and then run the velocity calc and moveandslide lcoally
        MoveAndSlide();
    }

}
