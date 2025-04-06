using Godot;
using System;
using System.Threading.Tasks;



public partial class AbilityController : Node3D
{
	[Export]
	private Godot.Collections.Array<PackedScene> abilities;

	//public Godot.Collections.Array<AbilityBase> loadedAbilities;
	
	public override void _Ready()
	{
		base._Ready();
		foreach (PackedScene ability in abilities)
		{
			AbilityBase abilityInstance = ability.Instantiate<AbilityBase>();
			AddChild(abilityInstance);
			//GD.Print($"Loaded Ability {abilityInstance}");
		}
	}
}
