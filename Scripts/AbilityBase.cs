using Godot;
using System;

public partial class AbilityBase : Node3D
{
    public virtual String abilityInput { get; set; }

    public virtual bool bHasCooldown { get; set; } = true;
    private Timer cooldownTimer = new Timer();
    public virtual double cooldownTime { get; set; } = 1.0;

    public virtual bool bHasActiveTime { get; set; } = false;
    public Timer activeTimer = new Timer();
    public virtual double activeTime { get; set; } = 1.0;

    public bool bAbilityInputPressed = false;

    [Signal]
    public delegate void ExecuteAbilityEventHandler();

    [Signal]
    public delegate void AbilityActiveEndedEventHandler();

    public override void _Ready()
    {
        if(bHasCooldown)
        {
            cooldownTimer.WaitTime = cooldownTime;
            cooldownTimer.OneShot = true;
            AddChild(cooldownTimer);
            ExecuteAbility += StartCooldown;
        }

        if(bHasActiveTime)
        {
            activeTimer.WaitTime = activeTime;
            cooldownTimer.OneShot = true;
            AddChild(activeTimer);
            ExecuteAbility += StartActiveCooldown;
        }
        

    }

    public override void _Process(double delta)
    {
        if (bAbilityInputPressed && cooldownTimer.TimeLeft == 0.0)
        {
            StartAbility();
            bAbilityInputPressed = false;
        }
    }

    private void StartAbility()
    {
        EmitSignal(SignalName.ExecuteAbility);
    }

    public void StartCooldown()
    {
        cooldownTimer.Start();
    }

    public void StartActiveCooldown()
    {
        activeTimer.Start();
    }
}
