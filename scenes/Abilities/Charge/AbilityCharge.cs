using Godot;
using System;

public partial class AbilityCharge : AbilityBase
{
    // Exports
    [Export]
    private GpuParticles3D chargeVFX;

    // Publics
    public override bool bHasCooldown { get; set; } = true;
    public override double cooldownTime { get; set; } = 5.0;
    public override bool bHasActiveTime { get; set; } = true;
    public override double activeTime { get; set; } = 2.0;

    // Privates
    private float chargeSpeed = 10.0f;
    private double chargeDamage = 10.0;
    private bool isCharging = false;


    public override void _Ready()
    {
        // Charge inherits from AbilityBase: run the _Ready() function from AbilitBase in addition to this _Ready() function.
        base._Ready(); 

        // Launch the ability
        ExecuteAbility += ChargeForward; 

        // Stop charge if timer runs out
        activeTimer.Timeout += ChargeStop;      
    }

    private void OnAreaDetectedSomething(Node body)
    {
        // If collision with enemy or collision with object TAGS 
        if((body.IsInGroup("Enemy") || body.IsInGroup("Object")) && isCharging)
        {
            // End timer pre-maturely
            //activeTimer.Stop();
            GD.Print($"Player colliding with {body.Name} and isCharging {isCharging}");
            RpcId(owningPlayer.NetworkId, AbilityCharge.MethodName.ChargeStop);
            ChargeStop();
            //activeTimer.Timeout -= ChargeStop;  

            // Deal damage to dummy
            body.Rpc(TargetDummy.MethodName.TakeDamage, chargeDamage);
        }
    }

    private void ChargeForward() // Function that starts the charge
    {
        // Grab collider body - ONLY IF SERVER - SERVER-SIDE DETECTION ONLY!!!
        // Doing it here instead of _Ready so that it has enough time to init.
        if(Multiplayer.IsServer())
        {
            // Only detect collision if player is already charging
            isCharging = true;
            owningPlayer.BodyEnteredExternal += OnAreaDetectedSomething;
        }

        // Lock player input
        owningPlayer.bInputsLocked = true;
        
        // Move forward
        var forward = owningPlayer.Transform.Basis.Z.Normalized(); // grab player forward
        owningPlayer.forcedDirection = forward; // set player forward input for movement processing
        owningPlayer.forcedSpeed = chargeSpeed; // set charge speed
        owningPlayer.forceMove = true; // enable forced movement parameter for movement processing

        // Cancel any additional movement
        owningPlayer.GetPlayerInputController().InputDirection = Vector2.Zero;
        owningPlayer.GetPlayerInputController().bJustLeftClicked = false;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void ChargeStop()
    {
        // Reset all player input
        isCharging = false;
        owningPlayer.bInputsLocked = false; //unlock character
        owningPlayer.forceMove = false;
        owningPlayer.GetPlayerInputController().InputDirection = Vector2.Zero;
        owningPlayer.GetPlayerInputController().bJustLeftClicked = false;

        // interrupt timer
        if(activeTimer.TimeLeft > 0)
        {
            activeTimer.Stop();
        }


        if(Multiplayer.IsServer())
            GD.Print("SERVER: stopped charging...");
        else
            GD.Print("LOCAL: stopped charging...");
    }
}
