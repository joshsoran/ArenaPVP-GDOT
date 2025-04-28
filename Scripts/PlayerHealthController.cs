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

		// subtract damage from current hp
		currentHealth -= damageAmount;

        if(currentHealth <= 0)
        {
            //I don't like this parental call
            //maybe a an export reference would be better maybe it doesn't matter for now
            if (Multiplayer.IsServer())
            {
                GetParent<NetworkedPlayer>().RespawnPlayer();
            }
            //I think this should be handled in respawn player or maybe in it's own reset health method
            //for now we don't have too much complexity so this is fine
        
            currentHealth = maxHealth;
            playerHealthBar.Value = maxHealth;
        }
        else
        {
            // VISUAL - Subtract damage from HP bar
            playerHealthBar.Value -= damageAmount;
        }



	}

}
