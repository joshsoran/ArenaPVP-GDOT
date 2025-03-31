using Godot;
using System;
using System.Linq;

public partial class Network : Node
{

    [Export]
    public int Port = 7000;

    [Export]
    public int MaxClients = 20;

    [Export]
    public String DefaultServerIP = "94.174.205.107";

    private int _playersLoaded = 0;

    private PackedScene Player;

    
    private Godot.Collections.Dictionary<int, CharacterBody3D> ConnectedPlayers = new Godot.Collections.Dictionary<int, CharacterBody3D>();

    [Export]
    private PackedScene NetworkedPlayer;

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

        RpcId(1, MethodName.AddPlayerToServerList, peerId);

        GD.Print($"Client ID:{peerId} Connected");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void AddPlayerToServerList(int Id)
    {
        if(Id == 1)
        {
            return;
        }

//CHECK WHO IS ALREADY CONNECTED AND SPAWN THEN AND THEN MOVE ON TO SPAWNING YOURSELF FOR EVERYONE
        foreach (var ConnectedPlayer in ConnectedPlayers)
        {
            RpcId(Id, MethodName.AddPlayer, ConnectedPlayer.Key);
            //AddPlayer(ConnectedPlayerId, SpawnOffset);
        }

        ConnectedPlayers[Id] = new CharacterBody3D();
        GD.Print($"Client ID:{Id} added to server list");

        Rpc(MethodName.AddPlayer, Id);

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void AddPlayer(int Id)
    {

        if(Id == 1)
        {
            return;
        }

        //ConnectedPlayerIds.Add(Id);
        CharacterBody3D NetworkedPlayerInstance = (CharacterBody3D)NetworkedPlayer.Instantiate();
        NetworkedPlayerInstance.Set("NetworkId", Id);
        //NetworkedPlayerInstance.Call("SetNetworkID", Id);
        GD.Print($"Spawning Player with ID: {Id}");
        //we spawn on the node we are adding this child to (in this case it is the "network" node)
        AddChild(NetworkedPlayerInstance);
        ConnectedPlayers[Id] = NetworkedPlayerInstance;
        //we adjust the position so there isn't fuckery with spawning in the EXACT same spot at the same time
        NetworkedPlayerInstance.Position += new Vector3(ConnectedPlayers.Count*2, 0, 0);
    }

}
