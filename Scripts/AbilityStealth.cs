using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public partial class AbilityStealth : AbilityBase
{

    public override String abilityInput { get; set; } = "Ability1";

    public override bool bHasCooldown { get; set; } = true;
    public override double cooldownTime { get; set; } = 5.0;

    public override bool bHasActiveTime { get; set; } = true;
    public override double activeTime { get; set; } = 1.0;

    private NetworkedPlayer playerToAffect;

    public override void _Ready()
    {
        base._Ready();
        
        //get ability controller and then get player
        //this double get parent is DISGUSTING
        //TODO[@Cameron]: Give each ability an idea of their owner in the abiltiy controller when we load these abilities
        //we can store that info in ability base to be used
        playerToAffect = GetParent().GetParent<NetworkedPlayer>();

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

        playerToAffect.Hide();
        
    }

    private void RemoveStealth()
    {
        playerToAffect.Show();
    }
}
