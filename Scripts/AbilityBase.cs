using Godot;
using System;

public partial class AbilityBase : Node3D
{
    public virtual bool bHasCooldown { get; set; } = true;
    private Timer cooldownTimer = new Timer();
    public virtual double cooldownTime { get; set; } = 1.0;

    public virtual bool bHasActiveTime { get; set; } = false;
    public Timer activeTimer = new Timer();
    public virtual double activeTime { get; set; } = 1.0;

    public bool bAbilityInputPressed = false;

    private AbilityController localAbilityController;

    [Signal]
    public delegate void ExecuteAbilityEventHandler();

    [Signal]
    public delegate void AbilityActiveEndedEventHandler();

    public override void _Ready()
    {
        localAbilityController = GetParent<AbilityController>();
        if(localAbilityController == null)
        {
            GD.PrintErr($"localAbilityController is null in {System.Reflection.MethodBase.GetCurrentMethod().Name}");
        }

        if (bHasCooldown)
        {
            cooldownTimer.WaitTime = cooldownTime;
            cooldownTimer.OneShot = true;
            AddChild(cooldownTimer);
            ExecuteAbility += StartCooldown;
        }

        if (bHasActiveTime)
        {
            activeTimer.WaitTime = activeTime;
            cooldownTimer.OneShot = true;
            AddChild(activeTimer);
            ExecuteAbility += StartActiveCooldown;
        }
        

    }

    public override void _Process(double delta)
    {
        
        if(localAbilityController != null)
        {
            if (localAbilityController.globalCooldownTimer.TimeLeft != 0.0)
            {
                return;
            }
        }

        if (!bAbilityInputPressed)
        {
            return;
        }

        if (bHasCooldown && cooldownTimer.TimeLeft != 0.0)
        {
            return;
        }

        StartAbility();
    }

    private void StartAbility()
    {
        EmitSignal(SignalName.ExecuteAbility);
        //if ability goes twice it's because this is true for more than one _process tick
        bAbilityInputPressed = false;
        localAbilityController.globalCooldownTimer.Start();
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
