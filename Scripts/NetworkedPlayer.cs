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
    public Camera3D LocalCamera;
    [Export]
    public Node3D LocalCameraMount;

    [Export]
    private AbilityController playerAbilityController;
    public ref AbilityController GetPlayerAbilityController() { return ref playerAbilityController; }

    [Export]
    private NetworkedInput playerInputController;
    public ref NetworkedInput GetPlayerInputController() { return ref playerInputController; }

    // Publics
    public int NetworkId;
    public Vector3 _targetVelocity = Vector3.Zero;

    public bool bIsInitialized = false;   
    public Network NetworkNode; 
    public bool _canDealDamage = false; // prevent weapon from over-dealing damage

    // Privates
    private float Gravity = 9.81f;
    private Area3D _area3D; // for weapon detection

    private Godot.Collections.Dictionary<int, Vector3> PeerPositionsToResync = new Godot.Collections.Dictionary<int, Vector3>();
    

    // weapon collision detection
    private void OnBodyEntered(Node3D body)
    {
        // Return conditions
        // If not server
        if(!Multiplayer.IsServer()) return;
        // if can't deal damage
        if(!_canDealDamage) return;
        // If not enemy
        if(!body.IsInGroup("Enemy")) return;

        // Deal damage
        body.Rpc(TargetDummy.MethodName.TakeDamage, 10);
        _canDealDamage = false; 

        //GD.Print($"Enemy HP: {body.Get("currentHealth")}");
    }

    public override void _Ready()
    {
        playerInputController.Set("owningPlayer", this);
        if(Multiplayer.IsServer())
        {
            _area3D = GetNode<Area3D>("knight/Node/Skeleton3D/BoneAttachment3D/Area3D"); // Adjust path if necessary
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

        //playerAbilityController.PostPlayerLoad();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void SetNetworkNode(Network _NetworkNode)
    {
        NetworkNode = _NetworkNode;
    }



    public override void _PhysicsProcess(double delta)
    {
        ProcessMovement((Vector2)playerInputController.Get("InputDirection"), (bool)playerInputController.Get("bJustJumped"), delta);
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
        //GD.Print("replicated client behind server");
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
