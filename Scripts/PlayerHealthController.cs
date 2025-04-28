using Godot;
using System;

public partial class PlayerHealthController : Node3D
{
	[Export]
	private short maxHealth = 100;

    [Export]
    private ProgressBar playerHealthBar;

	private short currentHealth;

    public override void _Ready()
	{
		// Init current hp as max hp
		currentHealth = maxHealth;
        playerHealthBar.MaxValue = maxHealth;
        playerHealthBar.Value = maxHealth;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void TakeDamage(short damageAmount)
	{
		// prevent negative HP
		if(currentHealth < damageAmount){
			damageAmount = currentHealth;
		}

		// subtract damage from current hp
		currentHealth -= damageAmount;

		// VISUAL - Subtract damage from HP bar
		playerHealthBar.Value -= damageAmount;
	}

}
