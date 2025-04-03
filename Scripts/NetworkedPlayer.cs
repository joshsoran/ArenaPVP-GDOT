using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Reflection;

public partial class NetworkedPlayer : CharacterBody3D
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
    public bool bIsInitialized = false;   
    public Network NetworkNode; 
    // Privates
    private float Gravity = 9.81f;
    private Area3D _area3D; // for weapon detection
    private Godot.Collections.Dictionary<int, Vector3> PeerPositionsToResync = new Godot.Collections.Dictionary<int, Vector3>();

    // weapon collision detection
    private void OnBodyEntered(Node3D body)
    {
        // If not server
        if(!Multiplayer.IsServer())
        {
            return;
        }

        // If enemy
        if(body.IsInGroup("Enemy"))
        {
            // body.Call("td_takeDamage", 10);
            // GD.Print($"Enemy HP: {body.Get("td_current_health")}");
            body.Rpc(TargetDummy.MethodName.TakeDamage, 10);
            GD.Print($"Enemy HP: {body.Get("currentHealth")}");
        }
    }

    public override void _Ready()
    {
        if(Multiplayer.IsServer())
        {
            _area3D = GetNode<Area3D>("knight/Node/Area3D"); // Adjust path if necessary
            _area3D.BodyEntered += OnBodyEntered;
            return;
        }

        //Input.MouseMode = Input.MouseModeEnum.Captured;

        //This is bad bc it relies on the network node being a parent of all of our networkedplayers
        //We will prolly find a way to do this better later. Maybe an rpc form the network can do this
        NetworkNode = GetParent<Network>();

        if(NetworkId == Multiplayer.GetUniqueId())
        {
            LocalCamera.Current = true;
            bIsInitialized = true;
            InitializeOnServer();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void SetNetworkNode(Network _NetworkNode)
    {
        NetworkNode = _NetworkNode;
    }

    public override void _Input(InputEvent @event)
    {
        if(NetworkId != Multiplayer.GetUniqueId() || Multiplayer.IsServer())
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
        ProcessMovement(InputDirection, bJustJumped, delta);

        if(Multiplayer.IsServer())
        {                
            return;
        }

        if(NetworkId != Multiplayer.GetUniqueId())
        {
            return;
        }

        InputDirection = Input.GetVector("move_right", "move_left", "move_down", "move_up");
        bJustJumped = Input.IsActionJustPressed("jump");
        foreach (var Peer in Multiplayer.GetPeers())
        {

            if(Multiplayer.IsServer())
            {                
                continue;
            }
            
            if(Peer == NetworkId)
            {
                continue;
            }

            RpcId(Peer, MethodName.ReplicateInput, InputDirection, bJustJumped);
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
        MoveAndSlide();

        if(Multiplayer.IsServer())
        {
            return;
        }

        if(!GetRealVelocity().IsZeroApprox() || !_InputDirection.IsZeroApprox() || _bJustJumped)
        {
            NetworkNode.RpcId(1, Network.MethodName.ValidateClientPostionAgainstServer, NetworkId, Position);
        }

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void UpdateOutOfSyncClientPosition(int OutOfSyncClientId, Vector3 ServerPosition)
    {
        GD.Print("replicated client behind server");
        //PeerPositionsToResync[OutOfSyncClientId] = ServerPosition;
        if(NetworkId == OutOfSyncClientId)
        {
            Position = ServerPosition;
        }
        

    }

    private void InitializeOnServer()
    {  
        if(NetworkNode == null)
        {
            GD.PrintErr($"NetworkNode is null in InitializeOnServer() When called on peer {NetworkId}");
            return;
        }
        //let the server know we are ready for gaming
        NetworkNode.RpcId(1, Network.MethodName.RecievePlayerInit, NetworkId, bIsInitialized);
        //Run an initial check for our server position because of spawning fuckery
        //deactivated for now but keeping it for future debugging
        //NetworkNode.RpcId(1, Network.MethodName.ValidateClientPostionAgainstServer, Multiplayer.GetUniqueId(), Position);
    }

}
