using Godot;
using System;

public partial class AbilityBase : Node
{
    //Cooldown Timer
    public virtual bool bHasCooldown { get; set; } = true;
    private Timer cooldownTimer = new Timer();
    public virtual double cooldownTime { get; set; } = 1.0;

    //Active Timer
    public virtual bool bHasActiveTime { get; set; } = false;
    public Timer activeTimer = new Timer();
    public virtual double activeTime { get; set; } = 1.0;

    //Casting Timer
    public virtual bool bHasCastingTime { get; set; } = false;
    public Timer castingTimer = new Timer();
    public virtual double castingTime { get; set; } = 0.5;

    //Charge Timer
    public virtual bool bHasChargeTime { get; set; } = false;
    public Timer ChargeTimer = new Timer();
    public virtual double ChargeTime { get; set; } = 0.5;

    [Export]
    public virtual Texture2D abilityIcon { get; set; }
    public TextureRect abilityTextureRect = new TextureRect();

    public bool bAbilityInputPressed = false;

    private AbilityController localAbilityController;

    public NetworkedPlayer owningPlayer;

    [Signal]
    public delegate void ExecuteAbilityEventHandler();

    [Signal]
    public delegate void AbilityActiveEndedEventHandler();

    public override void _Ready()
    {
        abilityTextureRect.Texture = abilityIcon;
        abilityTextureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        abilityTextureRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
        abilityTextureRect.Hide();

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

        if (bAbilityInputPressed)
        {
            if (bHasCooldown && cooldownTimer.TimeLeft != 0.0)
            {
                return;
            }

            if(localAbilityController.abilityQueue.Keys.Contains(this) || localAbilityController.abilityQueue.Count >= 2)
            {
                return;
            }
            

            localAbilityController.abilityQueue.Add(this, abilityTextureRect);
            
            //add the ability icon to the hud for the local player
			if(owningPlayer.NetworkId == Multiplayer.GetUniqueId() && !Multiplayer.IsServer())
			{
                //move the icon to the right so it's next in the queue on the HUD
				localAbilityController.playerHUD.abilityIconContainer.MoveChild(abilityTextureRect, abilityTextureRect.GetIndex() + 1);
				abilityTextureRect.Show();
				castingTimer.Timeout += abilityTextureRect.Hide;
				
			}

            //only start the queue processing if we added to it and it was empty before we did so
            if(localAbilityController.abilityQueue.Count == 1)
            {
                localAbilityController.ProcessAbilityQueue();
            }
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
