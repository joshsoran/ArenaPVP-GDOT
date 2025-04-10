using Godot;
using System;
using System.Linq;

public partial class NetworkedInput : Node3D
{
    private Vector2 InputDirection = Vector2.Zero;
    private NetworkedPlayer owningPlayer;
    [Export]
    public float MouseSensitivity = 0.1f;
    private bool bJustJumped = false;
    private bool bJustLeftClicked = false;
    private bool bJustCancelledCast = false;
    //This currently assumes a max of 10 abilities
    private Godot.Collections.Array<bool> bAbilityInputs = new Godot.Collections.Array<bool>{false, false, false, false, false, false, false, false, false, false};
    public override void _Input(InputEvent @event)
    {
        if (owningPlayer.NetworkId != Multiplayer.GetUniqueId() || Multiplayer.IsServer())
        {
            return;
        }

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            float VerticalMouseMovement = eventMouseMotion.Relative.X;
            owningPlayer.RotateY(Mathf.DegToRad(-VerticalMouseMovement * MouseSensitivity));
            float HorizontalMouseMovement = eventMouseMotion.Relative.Y; 
            owningPlayer.LocalCameraMount.RotateX(Mathf.DegToRad(-HorizontalMouseMovement * MouseSensitivity));
        }

        if(@event is InputEventMouseButton MouseButtonPressed)
        {
            bJustLeftClicked = MouseButtonPressed.IsActionPressed("left_click");
        }

        if (@event is InputEventKey KeyPressed)
        {
            InputDirection = Input.GetVector("move_right", "move_left", "move_down", "move_up");
            bJustJumped = KeyPressed.IsActionPressed("jump");
            bJustCancelledCast = KeyPressed.IsActionPressed("cancel_cast");
            bAbilityInputs[0] = KeyPressed.IsActionPressed("ability_one");
            bAbilityInputs[1] = KeyPressed.IsActionPressed("ability_two");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {                
            return;
        }

        if (owningPlayer.NetworkId != Multiplayer.GetUniqueId())
        {
            return;
        }
        
        //we should be doing all this getting on the physics process this can move into _ready at some point
        AbilityController abilityController = owningPlayer != null ? owningPlayer.GetPlayerAbilityController() : null;
        Godot.Collections.Array<AbilityBase> loadedAbilities = abilityController != null ? abilityController.GetLoadedAbilities() : null;
        if (loadedAbilities != null)
        {
            int i = 0;
            foreach (AbilityBase ability in loadedAbilities)
            {
                if (ability == null)
                {
                    continue;
                }
                ability.bAbilityInputPressed = bAbilityInputs[i];
                i++;
            }
        }

        foreach (var Peer in Multiplayer.GetPeers())
        {

            if(Multiplayer.IsServer())
            {                
                continue;
            }
            
            if(Peer == owningPlayer.NetworkId)
            {
                continue;
            }
            
            RpcId(Peer, MethodName.ReplicateInput, InputDirection, bJustJumped, bJustLeftClicked, bJustCancelledCast, bAbilityInputs);
            RpcId(Peer, MethodName.ReplicateLook, owningPlayer.Rotation);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ReplicateInput(Vector2 _InputDirection, bool _bJustJumped, bool _bJustLeftClicked, bool _bJustCancelledCast, Godot.Collections.Array<bool> _bAbilityInputs)
    {
        AbilityController abilityController = owningPlayer != null ? owningPlayer.GetPlayerAbilityController() : null;
        Godot.Collections.Array<AbilityBase> loadedAbilities = abilityController != null ? abilityController.GetLoadedAbilities() : null;
        if(loadedAbilities == null)
        {
            GD.PrintErr($"loadedAbilities is null in {System.Reflection.MethodBase.GetCurrentMethod().Name}");
            return;
        }
        int i = 0;
        foreach (AbilityBase ability in loadedAbilities)
        {
            if (ability == null)
            {
                continue;
            }
            ability.bAbilityInputPressed = _bAbilityInputs[i];
            i++;
        }
        bJustLeftClicked =  _bJustLeftClicked;
        InputDirection = _InputDirection;
        bJustJumped = _bJustJumped;
        bJustCancelledCast = _bJustCancelledCast;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ReplicateLook(Vector3 LookRotation)
    {
        owningPlayer.Rotation = LookRotation;
    }
}
