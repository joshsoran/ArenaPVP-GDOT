using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;



public partial class AbilityController : Node3D
{
	[Export]
	private NetworkedInput networkedInput;
	[Export]
	private Godot.Collections.Array<PackedScene> abilities;

	private Godot.Collections.Array<AbilityBase> loadedAbilities = new Godot.Collections.Array<AbilityBase>();
	public ref Godot.Collections.Array<AbilityBase> GetLoadedAbilities() { return ref loadedAbilities; }
	
	public Godot.Collections.Array<AbilityBase> abilityQueue = new Godot.Collections.Array<AbilityBase>();
	
	public override void _Ready()
	{
		base._Ready();

		foreach (PackedScene ability in abilities)
		{
			
			AbilityBase abilityInstance = ability.Instantiate<AbilityBase>();
			AddChild(abilityInstance);
			//again. using relationships like this is not a good idea
			abilityInstance.owningPlayer = GetParent<NetworkedPlayer>();
			if (abilityInstance.owningPlayer == null)
			{
				GD.PrintErr($"Ability Owner is null in {System.Reflection.MethodBase.GetCurrentMethod().Name}");
			}
			loadedAbilities.Add(abilityInstance);
			//GD.Print($"Loaded Ability {abilityInstance.GetClass()}");
		}
	}

    public override void _Process(double delta)
    {
        base._Process(delta);
		if ((bool)networkedInput.Get("bJustCancelledCast"))
		{
			CancelAbilitiesQueued();
		}
    }


    public void ProcessAbilityQueue()
    {
		if(abilityQueue.Count == 0)
		{
			return;
		}
		AbilityBase ability = abilityQueue.First();
		if (ability.bHasCastingTime)
		{
			ability.castingTimer.Timeout += ability.StartAbility;
			//ability.castingTimer.Timeout += end casting animation;
			ability.castingTimer.Start();
			//here is where we would trigger a casting animation
		}
		else
		{
			//activate the abiltiy now becasue it has nho casting time
			ability.StartAbility();
			//we don't need to process the next in the queuw bc you
			//shouldn't be able to queue something after an instant speed ability
		}
    }

	private void CancelAbilitiesQueued()
	{
		foreach (AbilityBase ability in abilityQueue)
		{
			if (ability.bHasCastingTime)
			{
				ability.castingTimer.Timeout -= ability.StartAbility;
				ability.castingTimer.Stop();
			}
		}
		abilityQueue.Clear();
	}
}
