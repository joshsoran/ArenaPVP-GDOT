using Godot;
using System;

public partial class NetworkAnimations : Node
{
    [Export]
    private NetworkedPlayer Player;

    [Export]
    private AnimationTree PlayerAnimationTree;

    Vector2 CurrentVelocity = Vector2.Zero;
    float StrafeAcceleration = 6.5f; // Animation blend timer essentially
    private Vector2 TargetSpeed;
    private AnimationNodeOneShot AttackOneShot; // Track attack node

    int LocalNetworkId;

    //we should probably subscribe to the onconnect event and do all of this _ready stuff there.
    public override void _Ready()
    {
        LocalNetworkId = Multiplayer.GetUniqueId();
        GD.Print($"network animdation id: {LocalNetworkId}");

        // Animation tree info
        //PlayerAnimationTree = GetNode<AnimationTree>("AnimationTree");
        // Get the actual OneShot node from the AnimationTree
        AttackOneShot = (AnimationNodeOneShot)PlayerAnimationTree.Get("parameters/oneshot_attack");
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

        AnimatePlayerMovement(delta);

    }

    //We should keep all of the core funcitonality in discrete functions so we can do these calculations on the server
    //This lets us go from client authoritative to server authoritative easier
    //this isn't really that important to be server led anyway
    //still good to practice good standards so our code doesn't turn into a rats nest
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void AnimatePlayerMovement(double delta)
    {
        //we might be already doing some of this in ourt movement controller. It's worth looking into consolidating this somwhow
        if(Player.IsOnFloor())
        {
            PlayerAnimationTree.Set("parameters/Transition/transition_request", "Grounded");

            TargetSpeed = ((Vector2)Player.GetPlayerInputController().Get("InputDirection")).Normalized();
            CurrentVelocity = CurrentVelocity.MoveToward(-TargetSpeed, StrafeAcceleration * (float)delta);
            Vector2 strafeInput = new Vector2(CurrentVelocity.X, -CurrentVelocity.Y);
            PlayerAnimationTree.Set("parameters/Locomotion/blend_position", strafeInput);


            // Vector3 StrafeDirection3 = (Vector3)Player.Get("_targetVelocity");
            // Vector2 StrafeDirection2 = new Vector2(StrafeDirection3.X, StrafeDirection3.Y);
            // PlayerAnimationTree.Set("parameters/Locomotion/blend_position", -StrafeDirection2);

            if((bool)Player.GetPlayerInputController().Get("bJustJumped"))
            {
                PlayerAnimationTree.Set("parameters/OneShot/request", 1);
            }
        }
        else
        {
            PlayerAnimationTree.Set("parameters/Transition/transition_request", "Fall");
        }

        // Left click attack
        if((bool)Player.GetPlayerInputController().Get("bJustLeftClicked"))
        {
            bool isActive = (bool)PlayerAnimationTree.Get("parameters/oneshot_attack/active");
            if (!isActive)
            {
                PlayerAnimationTree.Set("parameters/oneshot_attack/request", 1);
                Player.Set("bCanDealDamage", true);
                //GD.Print($"Damage: {Player._canDealDamage}");
            }
        }
        else
        {
            bool isActive = (bool)PlayerAnimationTree.Get("parameters/oneshot_attack/active");
            if(!isActive) // if animation stopped playing, make sure player can't do anymore damage
            {
                Player.Set("bCanDealDamage", false);
                //GD.Print($"Damage: {Player._canDealDamage}");
            }   
        }
    }
}
