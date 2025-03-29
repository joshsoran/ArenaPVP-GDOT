using Godot;
using System;

public partial class NetworkAnimations : Node
{
    [Export]
    private CharacterBody3D Player;

    [Export]
    private AnimationTree PlayerAnimationTree;

    Vector2 CurrentVelocity = Vector2.Zero;
    float StrafeAcceleration = 4.0f;
    float TargetSpeed;
    int LocalNetworkId;

    //we should probably subscribe to the onconnect event and do all of this _ready stuff there.
    public override void _Ready()
    {
        LocalNetworkId = Multiplayer.GetUniqueId();
        GD.Print($"network animdation id: {LocalNetworkId}");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        int CurrentPeer = Multiplayer.GetUniqueId();
        if(LocalNetworkId != CurrentPeer)
        {
            //rpc to other clients and THENNNNN return
            RpcId(CurrentPeer, MethodName.AnimatePlayerMovement);
            return;
        }

        AnimatePlayerMovement();

    }

    //We should keep all of the core funcitonality in discrete functions so we can do these calculations on the server
    //This lets us go from client authoritative to server authoritative easier
    //this isn't really that important to be server led anyway
    //still good to practice good standards so our code doesn't turn into a rats nest
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void AnimatePlayerMovement()
    {
        //we might be already doing some of this in ourt movement controller. It's worth looking into consolidating this somwhow
        if(Player.IsOnFloor())
        {
            PlayerAnimationTree.Set("parameters/Transition/transition_request", "Grounded");
            Vector3 StrafeDirection3 = (Vector3)Player.Get("_targetVelocity");
            Vector2 StrafeDirection2 = new Vector2(StrafeDirection3.X, StrafeDirection3.Z);
            PlayerAnimationTree.Set("parameters/Locomotion/blend_position", -StrafeDirection2);

            if(Input.IsActionJustPressed("jump"))
            {
                PlayerAnimationTree.Set("parameters/OneShot/request", 1);
            }
        }
        else
        {
            PlayerAnimationTree.Set("parameters/Transition/transition_request", "Fall");
        }
    }
}
