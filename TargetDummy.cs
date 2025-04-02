using Godot;
using System;

public partial class TargetDummy : CharacterBody3D
{
	// Exports
	[Export]
	public double td_max_health = 100;
	[Export]
	public double td_current_health;

	// Publics

	// Privates
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Init current hp as max hp
		td_current_health = td_max_health;
	}

	// Take damage function
	public void td_takeDamage(double damage)
	{
		// prevent negative HP
		if(td_current_health < damage){
			damage = td_current_health;
		}
		// subtract damage from current hp
		td_current_health -= damage;

		// VISUAL - Subtract damage from HP bar
		GetNode<ProgressBar>("SubViewport/hp_bar").Value -= damage;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
