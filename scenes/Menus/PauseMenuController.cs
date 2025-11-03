using Godot;
using System;

public partial class PauseMenuController : Node2D
{
    [Export]
    private Control UltimateMenu;

    [Export]
    private Control pauseMenu;

    [Export]
    private Control settingsMenu;

    [Export]
    private Control abilityMenu;

    [Export]
    private Control settingsButton;
    
    [Export]
    private Control abilitiesButton;
        
    [Export]
    private Control mainBackButton;

    [Export]
    private Control settingsBackButton;

    [Export]
    private Control abilityBackButton;

    public NetworkedPlayer owningPlayer;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey KeyPressed)
        {
            if(KeyPressed.IsActionPressed("open_pause_menu"))
            {
                UltimateMenu.Visible = !UltimateMenu.Visible;
                if(owningPlayer != null)
                {
                    owningPlayer.bInputsLocked = UltimateMenu.Visible;
                }
            }
        }
    }
}
