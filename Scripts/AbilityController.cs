using Godot;
using System;
using System.Threading.Tasks;



public partial class AbilityController : Node3D
{
	[Export]
	private Godot.Collections.Array<PackedScene> abilities;

	//TODO[@cameron]: change this to a queue system down the line
	//Where if I actiavate an ability within the GCD then send it out as soon as the next one ends
	//basically async funciton learning
	public Timer globalCooldownTimer = new Timer();
    private double globalCooldownTime = 1.0;

	public Godot.Collections.Array<AbilityBase> loadedAbilities = new Godot.Collections.Array<AbilityBase>();
	
	public override void _Ready()
	{
		base._Ready();

		globalCooldownTimer.WaitTime = globalCooldownTime;
        globalCooldownTimer.OneShot = true;
        AddChild(globalCooldownTimer);

		foreach (PackedScene ability in abilities)
		{
			
			AbilityBase abilityInstance = ability.Instantiate<AbilityBase>();
			AddChild(abilityInstance);
			loadedAbilities.Add(abilityInstance);
			//GD.Print($"Loaded Ability {abilityInstance.GetClass()}");
		}
	}
}
