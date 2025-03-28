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

    //do _process to see if the player position has changed and then update it idek

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
            //Player = GD.Load<PackedScene>("res://scenes/player.tscn");
            //NetworkedPlayer = GD.Load<PackedScene>("res://scenes/networked_player.tscn");
            CreateClientAndJoinServer();
        }

        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.PeerDisconnected += OnDisconnect;
        
    }

    public void OnDisconnect(long DisconnectedPeerId)
    {
        RemoveChild(ConnectedPlayers[(int)DisconnectedPeerId]);
        ConnectedPlayers.Remove((int)DisconnectedPeerId);
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

        //create the player now that we have connected
        //we will need to do this locally and then rpc it on all systems but the server.

        //var PlayerInstance = Player.Instantiate();
        //AddChild(PlayerInstance);


        //we will also need to manage a disconnect through an rpc at some point
        //maybe there is an overidable diconnect

        //we will need to manage spawn lcoations at somepoint
        //for now we can rely on the position of the network node
        //mayhaps we can create a few spawn nodes and select them based on the player count

        GD.Print("Client Created");
        return Error.Ok;
    }

    private void OnConnectOk()
    {
        
        int peerId = Multiplayer.GetUniqueId();

        RpcId(1, MethodName.AddPlayerToServerList, peerId);

        GD.Print($"Client ID:{peerId} Connected");

        //having to many issues with connecting at the same time or something idk
        // this migth work better with a connect button
        
        //Rpc(MethodName.AddPlayer, peerId);

        //Rpc(MethodName.AddPlayer, peerId);
        
            //add ourselves
            //AddPlayer(peerId);
            //GD.Print($"Spawning Local Player with ID: {peerId}");
            //load whoever is already connected (could be the local player)
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
        AddChild(NetworkedPlayerInstance);
        ConnectedPlayers[Id] = NetworkedPlayerInstance;
        NetworkedPlayerInstance.Position += new Vector3(GD.RandRange(-3, 3), 0, GD.RandRange(-3, 3));
    }

    //[TODO: @Cameron]We aren't doing anything on disconnect. Will need to check for this and client closing game to remove the peer

}
