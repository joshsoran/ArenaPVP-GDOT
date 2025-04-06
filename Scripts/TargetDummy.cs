using Godot;
using System;

public partial class TargetDummy : CharacterBody3D
{
	// Exports
	[Export]
	public double maxHealth = 100;
	[Export]
	public double currentHealth;

	// Publics

	// Privates
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Init current hp as max hp
		currentHealth = maxHealth;
	}

	// Take damage function
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void TakeDamage(double damage)
	{
		// prevent negative HP
		if(currentHealth < damage){
			damage = currentHealth;
		}
		// subtract damage from current hp
		currentHealth -= damage;

		// VISUAL - Subtract damage from HP bar
		GetNode<ProgressBar>("SubViewport/hp_bar").Value -= damage;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
