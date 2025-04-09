using Godot;
using System;

public partial class AbilityBase : Node
{
    public virtual bool bHasCooldown { get; set; } = true;
    private Timer cooldownTimer = new Timer();
    public virtual double cooldownTime { get; set; } = 1.0;

    public virtual bool bHasActiveTime { get; set; } = false;
    public Timer activeTimer = new Timer();
    public virtual double activeTime { get; set; } = 1.0;

    //The time it takes for the ability to be cast
    public virtual bool bHasCastingTime { get; set; } = false;
    public Timer castingTimer = new Timer();
    public virtual double castingTime { get; set; } = 0.5;

    public bool bAbilityInputPressed = false;

    private AbilityController localAbilityController;

    public NetworkedPlayer owningPlayer;

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
            activeTimer.OneShot = true;
            AddChild(activeTimer);
            ExecuteAbility += StartActiveCooldown;
        }

        if(bHasCastingTime)
        {
            castingTimer.WaitTime = castingTime;
            castingTimer.OneShot = true;
            AddChild(castingTimer);
        }

    }

    public override void _Process(double delta)
    {
        
        if(localAbilityController == null)
        {
            GD.PrintErr($"localAbilityController is null in {System.Reflection.MethodBase.GetCurrentMethod().Name}");
            return;
        }

        if (!bAbilityInputPressed)
        {
            return;
        }

        if (bHasCooldown && cooldownTimer.TimeLeft != 0.0)
        {
            return;
        }

        if(localAbilityController.abilityQueue.Contains(this) || localAbilityController.abilityQueue.Count >= 2)
        {
            return;
        }
        
        localAbilityController.abilityQueue.Add(this);

        //only start the queue processing if we added to it and it was empty before we did so
        if(localAbilityController.abilityQueue.Count == 1)
        {
            localAbilityController.ProcessAbilityQueue();
        }
        
    }

    public void StartAbility()
    {
        EmitSignal(SignalName.ExecuteAbility);
        bAbilityInputPressed = false;

        localAbilityController.abilityQueue.Remove(this);
        localAbilityController.ProcessAbilityQueue();
        if(bHasCastingTime)
		{
            castingTimer.Timeout -= StartAbility;
        }
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
