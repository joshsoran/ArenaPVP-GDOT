extends MultiplayerSynchronizer

# Set via RPC to simulate is_action_just_pressed.
@export var jumping := false

# Synchronized property.
@export var direction := Vector2()

func postIntialization():
	# Only process for the local player.
	var IsLocalPlayer = get_multiplayer_authority() == multiplayer.get_unique_id();
	set_process(IsLocalPlayer)
	print(get_multiplayer_authority());
	print(multiplayer.get_unique_id());
	print(IsLocalPlayer);

#@rpc("call_local")
func jump():
	jumping = true


func _process(_delta):
	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	direction = Input.get_vector("move_right", "move_left", "move_down", "move_up")
	if Input.is_action_just_pressed("jump"):
		jump()
		#jump.rpc()