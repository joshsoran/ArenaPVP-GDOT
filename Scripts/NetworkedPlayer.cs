using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Net;
using System.Reflection;

public partial class NetworkedPlayer : CharacterBody3D
{
    // Exports
    [Export]
    public float defaultSpeed = 5.0f;
    [Export]
    public float JumpVelocity = 4.5f;

    [Export]
    public Camera3D LocalCamera;
    [Export]
    public Node3D LocalCameraMount;


    [Export]
    private PlayerHealthController playerHealthController;

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
    public bool bInputsLocked = false; // for locking player input
    public Vector3 forcedDirection = Vector3.Zero;
    public bool forceMove = false;
    public float forcedSpeed = 0f;
    public Vector3 normalInputDirection = Vector3.Zero;
    public Node LastEnteredBody { get; private set; }

    [Signal]
    public delegate void BodyEnteredExternalEventHandler(Node body);

    // Privates
    [Export]
    private PackedScene pauseMenu;
    private bool bCanDealDamage = false;
    private float Gravity = 9.81f;
    public Area3D _WeaponArea3D; // for weapon detection
    public Area3D _PlayerArea3D; // player body
    private Godot.Collections.Dictionary<int, Vector3> PeerPositionsToResync = new Godot.Collections.Dictionary<int, Vector3>();
    
    public void RespawnPlayer()
    {
        //in the future we will do other things here for now just change the position.
        //This works because this is only ever called on the server and then...
        //the client get's told it's out of position from our desync handling and get's moved
        Position = new Vector3(0, 0, 0);
        foreach (int connectedPlayerId in Multiplayer.GetPeers())
        {
            RpcId(connectedPlayerId, MethodName.UpdateOutOfSyncClientPosition, NetworkId, Position);
        }
        
        //UpdateOutOfSyncClientPosition(NetworkId, Position);
    }

    // weapon collision detection
    private void OnWeaponCollision(Node3D body)
    {
        // If not server
        if (!Multiplayer.IsServer()) 
        {
            return;
        }

        // if can't deal damage
        if(!bCanDealDamage) 
        {
            return;
        }
        // If not enemy
        //if(!body.IsInGroup("Enemy")) return;

        NetworkedPlayer HitPlayer = (NetworkedPlayer)body;
        //ensure what we hit was a playe
        if(HitPlayer == null)
        {
            return;
        }

        //and that the player hit isn't the player that's trying to hit something
        if(NetworkId == HitPlayer.NetworkId)
        {
            return;
        }

        // Deal Basic Melee Damage
        HitPlayer.playerHealthController.Rpc(PlayerHealthController.MethodName.TakeDamage, 10);
        bCanDealDamage = false; 

        //GD.Print($"Enemy HP: {body.Get("currentHealth")}");
    }

    public void OnPlayerCollision(Node3D body)
    {
        // Return conditions
        // Only emit signal if server
        if(!Multiplayer.IsServer()){return;}
        // Emit signal
        EmitSignal(SignalName.BodyEnteredExternal, body);
    }

    public override void _Ready()
    {
        playerInputController.Set("owningPlayer", this);
        if(Multiplayer.IsServer())
        {
            // weapon collision
            _WeaponArea3D = GetNode<Area3D>("knight/Node/Skeleton3D/BoneAttachment3D/Area3D"); // Adjust path if necessary
            _WeaponArea3D.BodyEntered += OnWeaponCollision;

            // player collision
            _PlayerArea3D = GetNode<Area3D>("CollisionShape3D/Area3D"); // Adjust path if necessary
            _PlayerArea3D.BodyEntered += OnPlayerCollision;
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
            //we can make the local palyer health bar invisible and then turn on visibility for a HUD health bar
            //playerHealthController.Visible = false;
            //load and add a pause menu to the local player
            //this method lets us carry the paause menu through to different maps and even modify it based on paramaters here
            //this causes errors and idk why so I'm done now
            //PauseMenuController puaseMenuInstance = pauseMenu.Instantiate<PauseMenuController>();
			//AddChild(puaseMenuInstance);
            //puaseMenuInstance.owningPlayer = this;
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
        // forced movement like ability usage, being feared away, being taunted, etc.
        if(forceMove)
        {
            ProcessMovement(forcedDirection, false, delta, forcedSpeed);
        }
        else
        {
            // Take player movement input, transform it into a vector 3, and normalize
            normalInputDirection = (Transform.Basis * new Vector3(((Vector2)playerInputController.Get("InputDirection")).X, 0, ((Vector2)playerInputController.Get("InputDirection")).Y)).Normalized();
            ProcessMovement(normalInputDirection, (bool)playerInputController.Get("bJustJumped"), delta, defaultSpeed);
        }
    }

    public void ProcessMovement(Vector3 _InputDirection, bool _bJustJumped, double delta, float speed)
    {
         // gravity
        if(!IsOnFloor())
        {
            _targetVelocity.Y -= Gravity * (float)delta;
        }
        
        // Handle jump
        if(_bJustJumped && IsOnFloor())
        {
            _targetVelocity.Y = JumpVelocity;
        }
  
        // Movement
        if(_InputDirection.IsZeroApprox())
        {
            _targetVelocity.X = _InputDirection.X * speed;
            _targetVelocity.Z = _InputDirection.Z * speed;
        }
        else
        {
            _targetVelocity.X = Mathf.Lerp(_InputDirection.X, 0, speed);
            _targetVelocity.Z = Mathf.Lerp(_InputDirection.Z, 0, speed);
        }

        // Not sure why renaming of this is needed?
        Velocity = _targetVelocity;
        //GD.Print($"Velocity {Velocity}");
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
