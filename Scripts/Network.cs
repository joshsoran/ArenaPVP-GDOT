using Godot;
using System;
using System.Linq;

public partial class Network : Node3D
{

    [Export]
    public int Port = 7000;

    [Export]
    public int MaxClients = 20;

    [Export]
    public String DefaultServerIP = "94.174.205.107"; //94.174.205.107

    private int _playersLoaded = 0;

    private PackedScene Player;

    [Signal]
    public delegate void RetrieveLocalPlayerPositionEventHandler();
    
    private Godot.Collections.Dictionary<int, NetworkedPlayer> ConnectedPlayers = new Godot.Collections.Dictionary<int, NetworkedPlayer>();

    [Export]
    private PackedScene NetworkedPlayerScene;

    [Export]
    private Path3D PlayerSpawnPath;

    public override void _Ready()
    {
        base._Ready();

        if(OS.HasFeature("server"))
        {
            GD.Print("Creating Server...");
            CreateServer("BabiesFirstServer");
        }
        else
        {
            GD.Print("Creating Client...");
            CreateClientAndJoinServer();
        }

        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.PeerDisconnected += OnDisconnect;
        
    }

    public override void _Process(double delta)
    {
        if(Multiplayer.IsServer())
        {
            //FetchPlayerPositions();
        }
    }

    public void OnDisconnect(long DisconnectedPeerId)
    {
        RemoveChild(ConnectedPlayers[(int)DisconnectedPeerId]);
        ConnectedPlayers.Remove((int)DisconnectedPeerId);
        Multiplayer.MultiplayerPeer = null;
    }

    public void CreateServer(string ServerName)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(Port, MaxClients);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"Server Created on Port {Port} with a max of {MaxClients} connections");
    }

    public Error CreateClientAndJoinServer(string Address = "")
    {
        if (string.IsNullOrEmpty(Address))
        {
            Address = DefaultServerIP;
        }

        var peer = new ENetMultiplayerPeer();
        Error ClientCreationError = peer.CreateClient(Address, Port);

        if(ClientCreationError != Error.Ok)
        {
            GD.Print("ClientCreationError");
            return ClientCreationError;
        }

        Multiplayer.MultiplayerPeer = peer;

        GD.Print("Client Created");
        return Error.Ok;
    }

    private void OnConnectOk()
    {
        int peerId = Multiplayer.GetUniqueId();

        //Rpc(MethodName.AddPlayerToServerList, peerId);
        RpcId(1, MethodName.AddPlayerToServerList, peerId);
        //AddPlayerToServerList(peerId);
        
        GD.Print($"Client ID:{peerId} Connected");


    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void AddPlayerToServerList(int Id)
    {
        NetworkedPlayer NetworkedPlayerInstance = (NetworkedPlayer)NetworkedPlayerScene.Instantiate();
        NetworkedPlayerInstance.Set("NetworkId", Id);
        GD.Print($"Spawning Player with ID: {Id}");
        AddChild(NetworkedPlayerInstance);
        //need a new way to choose spawns bc this is mega stinky
        //the spawn point isn't even network synced. DISGUSTING
        //the location of the player is tho so it kinda gets around the spawn point syncing
        Random rnd = new Random();
        float offset = rnd.Next(-3, 3);
        NetworkedPlayerInstance.Position += new Vector3(offset, 0, 0);

        ConnectedPlayers[Id] = NetworkedPlayerInstance;

        foreach (var ConnectedPlayer in ConnectedPlayers)
        {
            RpcId(Id, MethodName.SpawnPlayersLocally, ConnectedPlayer.Key, NetworkedPlayerInstance.Position);
            if(ConnectedPlayer.Key == Id)
            {
                continue;
            }
            RpcId(ConnectedPlayer.Key, MethodName.SpawnPlayersLocally, Id, NetworkedPlayerInstance.Position);
        }

        GD.Print($"Client ID:{Id} added to server list");
        

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void SpawnPlayersLocally(int Id, Vector3 SpawnLocation)
    {
        NetworkedPlayer NetworkedPlayerInstance = (NetworkedPlayer)NetworkedPlayerScene.Instantiate();
        NetworkedPlayerInstance.Set("NetworkId", Id);
        AddChild(NetworkedPlayerInstance);
        NetworkedPlayerInstance.Position = SpawnLocation;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void ValidateClientPostionAgainstServer(int AskingClientId, Vector3 AskingClientPositionLocal)
    {
        //Ideally this tolerance would increase depending on the speed that the player is travelling at
        //rn it has to be 1.5 so we can jump and not get it eaten by this server validation method
        float Tolerance = 1.5f;
        Vector3 AskingClientPositionServer = ConnectedPlayers[AskingClientId].Position;
        //GD.Print($"Peer {AskingClientId} is at {AskingClientPositionServer} On the server");
        //GD.Print($"Peer {AskingClientId} is at {AskingClientPositionLocal} On their machine");
        float XDiff = Math.Abs(AskingClientPositionServer.X - AskingClientPositionLocal.X);
        float YDiff = Math.Abs(AskingClientPositionServer.Y - AskingClientPositionLocal.Y);
        float ZDiff = Math.Abs(AskingClientPositionServer.Z - AskingClientPositionLocal.Z);
        if(XDiff > Tolerance || YDiff > Tolerance || ZDiff > Tolerance)
        {
            GD.Print($"Peer {AskingClientId} is off by {XDiff}, {YDiff}, {ZDiff}");
            //ConnectedPlayers[AskingClientId].RpcId(AskingClientId, NetworkedPlayer.MethodName.UpdateOutOfSyncClientPosition, AskingClientId, AskingClientPositionServer);
            ConnectedPlayers[AskingClientId].Rpc(NetworkedPlayer.MethodName.UpdateOutOfSyncClientPosition, AskingClientId, AskingClientPositionServer);
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RecievePlayerInit(int CallingPlayerId, bool bIsInitialized)
    {
        ConnectedPlayers[CallingPlayerId].bIsInitialized = bIsInitialized;
        GD.Print($"Initialization {bIsInitialized} for peer {CallingPlayerId} Recieved");
    }
}
