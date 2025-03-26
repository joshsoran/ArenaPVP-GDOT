extends Node3D

@export var player : CharacterBody3D
@onready var animation_tree = $knight/AnimationTree

var currentspeed = Vector2.ZERO
var strafe_acceleration = 4
var targetspeed

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	
	if player == null:
		return
	
	targetspeed = Vector2(player.input_dir.x, player.input_dir.y).normalized()
	
	currentspeed = currentspeed.move_toward(-targetspeed, strafe_acceleration * delta)
	var strafe_input = Vector2(currentspeed.x, -currentspeed.y)
	
	animation_tree.set("parameters/Locomotion/blend_position", -strafe_input)
	pass
