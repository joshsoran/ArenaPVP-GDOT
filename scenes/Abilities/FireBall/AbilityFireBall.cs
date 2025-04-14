using Godot;
using System;

public partial class AbilityFireBall : AbilityBase
{
    public override bool bHasCooldown { get; set; } = true;
    public override double cooldownTime { get; set; } = 5.0;

    public override bool bHasActiveTime { get; set; } = true;
    public override double activeTime { get; set; } = 4.0;

    public override bool bCanCharge { get; set; } = true;
    public override double ChargeTime { get; set; } = 0.5;
    public override double maxChargeCount { get; set; } = 3;
    private FireBall fireBallInstance;
    private float fireBallSpeed = 10.0f;
    private Vector3 fireBallScale = new Vector3(1, 1, 1);

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
        //change the fireball size and speed based on charge amount
        float newFireBallSpeed = fireBallSpeed + (fireBallSpeed * (0.25f * (float)currentChargeCount));
        Vector3 newFireBallScale = fireBallScale + (fireBallScale * (0.25f * (float)currentChargeCount));

        fireBallInstance.InitializeFireball(owningPlayer, newFireBallSpeed, -owningPlayer.GlobalTransform.Basis.Z, newFireBallScale);
        
        GD.Print($"Fireball Launched {Multiplayer.GetUniqueId()}");
    }

    private void RemoveFireBall()
    {
        GetTree().CurrentScene.RemoveChild(fireBallInstance);
        GD.Print($"Fireball Removed {Multiplayer.GetUniqueId()}");
    }
}
