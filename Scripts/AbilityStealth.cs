using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public partial class AbilityStealth : AbilityBase
{
	public override bool bHasCooldown { get; set; } = true;
	public override double cooldownTime { get; set; } = 5.0;

	public override bool bHasActiveTime { get; set; } = true;
	public override double activeTime { get; set; } = 1.0;

    public override bool bHasCastingTime { get; set; } = true;
    public override double castingTime { get; set; } = 1.0;

	public override void _Ready()
	{
		base._Ready();

		ExecuteAbility += AddStealth;
		activeTimer.Timeout += RemoveStealth;

	}

	private void AddStealth()
	{
		//called on all clients
		//GD.Print($"Executed Stealth ability on {Multiplayer.GetUniqueId()}");
		//called from the server
		//GD.Print($"Stealth is RPCd from the server or peer {Multiplayer.GetRemoteSenderId()}");
		//with information about who called
		//GD.Print($"The original requester and caster is peer {fromPlayerId}");

		owningPlayer.Hide();
		
	}

	private void RemoveStealth()
	{
		owningPlayer.Show();
	}
}
