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

        StartCharge += InitFireBall;

    }

    private void InitFireBall()
    {
        //this is not a good way fo doing this. We will improve on this hacky solution if we add a real fireball to the game
        //probably use a different charge vfx/animation entirely
        fireBallInstance = fireBall.Instantiate<FireBall>();
        fireBallInstance.owningPlayer = owningPlayer;
		owningPlayer.AddChild(fireBallInstance);
        //fireBallInstance.Position += owningPlayer.GlobalTransform.Basis.Y;

        ChargeTimer.Timeout += IncreaseFireBallCharge;
    }

    private void IncreaseFireBallCharge()
    {
        Vector3 newFireBallScale = fireBallScale + (fireBallScale * (0.25f * (float)currentChargeCount));
        fireBallInstance.SetFireBallScale(newFireBallScale);
    }

    private void LaunchFireBall()
    {
        fireBallInstance.Reparent(GetTree().CurrentScene);
        //change the fireball speed based on charge amount
        float newFireBallSpeed = fireBallSpeed + (fireBallSpeed * (0.25f * (float)currentChargeCount));
        fireBallInstance.ShootFireball(newFireBallSpeed, -owningPlayer.GlobalTransform.Basis.Z);
        
        GD.Print($"Fireball Launched {Multiplayer.GetUniqueId()}");
    }

    private void RemoveFireBall()
    {
        GetTree().CurrentScene.RemoveChild(fireBallInstance);
        GD.Print($"Fireball Removed {Multiplayer.GetUniqueId()}");
    }
}
