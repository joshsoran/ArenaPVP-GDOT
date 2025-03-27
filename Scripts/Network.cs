using Godot;
using System;

public partial class Network : Node
{

    [Export]
    public int Port = 7000;

    [Export]
    public int MaxClients = 20;

    [Export]
    public String DefaultServerIP = "127.0.0.1";

    private int _playersLoaded = 0;

    private PackedScene Player;

    [Export]
    private PackedScene NetworkedPlayer;
    
    //unused atm
    [Signal]
    public delegate void PlayerAddedEventHandler(int peerId);

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
        GD.Print($"Client ID:{peerId} Connected");
        if(!Multiplayer.IsServer())
        {
            //add ourselves
            AddPlayer(peerId);
            //load whoever is already connected (could be the local player)
            foreach (int NetworkId in Multiplayer.GetPeers())
            {
                if(NetworkId != peerId && NetworkId != 1)
                {
                    AddPlayer(NetworkId);
                }
            }
        }
    }

    private void AddPlayer(int Id)
    {
            Node NetworkedPlayerInstance = NetworkedPlayer.Instantiate();
            NetworkedPlayerInstance.Set("NetworkId", Id);
            NetworkedPlayerInstance.SetMultiplayerAuthority(Id);
            //NetworkedPlayerInstance.Call("SetNetworkID", Id);
            AddChild(NetworkedPlayerInstance);
    }

    //[TODO: @Cameron]We aren't doing anything on disconnect. Will need to check for this and client closing game to remove the peer

}
