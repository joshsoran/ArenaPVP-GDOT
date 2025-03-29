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

    public override void _Process(double delta)
    {
        base._Process(delta);

        LocalNetworkId = (int)Player.Get("NetworkId");
        if(Player == null || LocalNetworkId != Multiplayer.GetUniqueId())
        {
            return;
        }

        //we might be already doing some of this in ourt movement controller. It's worth looking into consolidating this somwhow
        if(Player.IsOnFloor())
        {
            PlayerAnimationTree.Set("parameters/Transition/transition_request", "Grounded");
            //there is def a cleaner way to do this conversion from vector3 to vector 2
            Vector3 StrafeDirection3 = (Vector3)Player.Get("_targetVelocity");
            Vector2 StrafeDirection2 = new Vector2(StrafeDirection3.X, StrafeDirection3.Z);
            PlayerAnimationTree.Set("parameters/Locomotion/blend_position", -StrafeDirection2);
            //TargetSpeed = Vector2(Player.Input)
            //NetworkedPlayerInstance.Set("NetworkId", Id);
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
