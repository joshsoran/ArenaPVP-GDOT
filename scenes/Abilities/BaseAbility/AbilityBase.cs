using Godot;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

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
    public virtual bool bCanCharge { get; set; } = false;
    public Timer ChargeTimer = new Timer();
    public virtual double ChargeTime { get; set; } = 0.5;
    public virtual double maxChargeCount { get; set; } = 3;
    public int currentChargeCount = 0;

    [Export]
    public virtual Texture2D abilityIcon { get; set; }
    public TextureRect abilityTextureRect = new TextureRect();

    public bool bAbilityInputPressed = false;
    public bool bAbilityInputReleased = false;

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

        //The ability can not have both a casting time and a charge time
        if(bHasCastingTime && bCanCharge)
        {
            GD.PrintErr($"The ability {this} can not have both a casting time and charge time");
        }
        else if(bHasCastingTime)
        {
            castingTimer.WaitTime = castingTime;
            castingTimer.OneShot = true;
            AddChild(castingTimer);
        } 
        else if(bCanCharge)
        {
            ChargeTimer.WaitTime = ChargeTime;
            ChargeTimer.OneShot = false;
            AddChild(ChargeTimer);
            ChargeTimer.Timeout += AdjustCharge;
        }

    }

    public override void _Process(double delta)
    {
        
        if(localAbilityController == null)
        {
            GD.PrintErr($"localAbilityController is null in {System.Reflection.MethodBase.GetCurrentMethod().Name}");
            return;
        }

        if (bHasCooldown && cooldownTimer.TimeLeft != 0.0)
        {
            return;
        }

        //we need to move this to a signal system so I don't have to do this on tick boolean bullshit
        if (bAbilityInputPressed)
        {
            if(bCanCharge)
            {
                //If we haven't already started the timer we will start it here.
                if(ChargeTimer.IsStopped())
                {
                    GD.Print("charge Timer started");
                    ChargeTimer.Start();
                    //I don't want to make a funciton for this so you get this abomination of an in-line declared delegate
                }
            }
        }
        
        if (bAbilityInputReleased)
        {
            if(localAbilityController.abilityQueue.Keys.Contains(this) || localAbilityController.abilityQueue.Count >= 2)
            {
                return;
            }

            //GD.Print($"Added ability {this}");
            localAbilityController.abilityQueue.Add(this, abilityTextureRect);
            
            //add the ability icon to the hud for the local player (don't do this if it's got no casting time)
			if(owningPlayer.NetworkId == Multiplayer.GetUniqueId() && !Multiplayer.IsServer() && bHasCastingTime)
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
        //wait until the input is dropped and then emit a signal and then we can use that charged amount in the child class
        //GD.Print("ExecuteAbility singal Emitted");
        EmitSignal(SignalName.ExecuteAbility);
        bAbilityInputPressed = false;
        //GD.Print($"AbilityInputReleased Set to false");
        bAbilityInputReleased = false;
        var foundIndex = localAbilityController.GetLoadedAbilities().IndexOf(this);
        // gaming.Where<AbilityBase>(gaming => gaming.Index);
        if(foundIndex == -1)
        {
            GD.PrintErr("Didn't find ability in loaded abilities.");
        }
        else
        {
            owningPlayer.GetPlayerInputController().SetAbilityReleasedInput(bAbilityInputReleased, foundIndex);
        }

        localAbilityController.abilityQueue.Remove(this);
        localAbilityController.ProcessAbilityQueue();
        if(bHasCastingTime)
		{
            castingTimer.Timeout -= StartAbility;
        }

        if(bCanCharge)
        {
            ChargeTimer.Stop();
            currentChargeCount = 0;
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

    public void AdjustCharge()
    {
        if (currentChargeCount < maxChargeCount) 
        {
            currentChargeCount++; 
            GD.Print("charge added");
        }
    }
}
