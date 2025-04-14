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
	[Export]
	public HUDController playerHUD;

	private Godot.Collections.Array<AbilityBase> loadedAbilities = new Godot.Collections.Array<AbilityBase>();
	public ref Godot.Collections.Array<AbilityBase> GetLoadedAbilities() { return ref loadedAbilities; }
	
	public Godot.Collections.Dictionary<AbilityBase, TextureRect> abilityQueue = new Godot.Collections.Dictionary<AbilityBase, TextureRect>();
	
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
			playerHUD.abilityIconContainer.AddChild(abilityInstance.abilityTextureRect);
		}
	}

    public override void _Process(double delta)
    {
        base._Process(delta);
		//we shouldn't do this everyframe. instead we should make events and subscribe to them
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

		AbilityBase ability = abilityQueue.First().Key;
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
			//remove this from the queue
			abilityQueue.Remove(ability);
		}
    }

	private void CancelAbilitiesQueued()
	{
		foreach (System.Collections.Generic.KeyValuePair<AbilityBase, TextureRect> ability in abilityQueue)
		{
			if (ability.Key.bHasCastingTime)
			{
				ability.Key.castingTimer.Timeout -= ability.Key.StartAbility;
				ability.Key.castingTimer.Stop();
				if(ability.Key.owningPlayer.NetworkId == Multiplayer.GetUniqueId() && !Multiplayer.IsServer())
				{
					ability.Value.Hide();
				}
			}
		}
		abilityQueue.Clear();
	}
}
