using Godot;
using System;

public partial class AbilityFireBall : AbilityBase
{
    public override bool bHasCooldown { get; set; } = true;
    public override double cooldownTime { get; set; } = 5.0;

    public override bool bHasActiveTime { get; set; } = true;
    public override double activeTime { get; set; } = 4.0;
    private FireBall fireBallInstance;
    private float fireBallSpeed = 10.0f;

    [Export]
    private PackedScene fireBall;

    public override void _Ready()
    {
        base._Ready();

        ExecuteAbility += LaunchFireBall;

        //delete the fireball if it's still alive after a long time
        //not really what the active timer is meant for...
        //we should have the fireball fizzle out before going away instead of blipping out of existance
        activeTimer.Timeout += RemoveFireBall;

    }

    private void LaunchFireBall()
    {
        
        fireBallInstance = fireBall.Instantiate<FireBall>();
        
		GetTree().CurrentScene.AddChild(fireBallInstance);
        fireBallInstance.InitializeFireball(owningPlayer, fireBallSpeed, -owningPlayer.GlobalTransform.Basis.Z);
        

        //change the shader paramaters based on strength
            //Main Texture Speed vec2
            //Main Texture Power float (voronoi intensity)
        GD.Print($"Fireball Launched {Multiplayer.GetUniqueId()}");
    }

    private void RemoveFireBall()
    {
        GetTree().CurrentScene.RemoveChild(fireBallInstance);
        GD.Print($"Fireball Removed {Multiplayer.GetUniqueId()}");
    }
}
