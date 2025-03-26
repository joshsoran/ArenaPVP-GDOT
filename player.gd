extends CharacterBody3D

@onready var camera_mount = $camera_mount
@onready var animation_player = $visuals/knight/AnimationPlayer

@onready var visuals = $visuals

const SPEED = 5.0
const JUMP_VELOCITY = 4.5

# Declaring them here to access from other scripts
var input_dir 
var direction

# Jumping shtuff
var temp_vel_x = 0
var temp_vel_z = 0
var is_falling = false

@export var mouse_sens = 0.1
var smoothed_direction : Vector3 
# Capture and hide mouse pointer in game
func _ready():
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

# Camera rotate section	with mouse
func _input(event):
	if event is InputEventMouseMotion:
		rotate_y(deg_to_rad(-event.relative.x * mouse_sens))
		camera_mount.rotate_x(deg_to_rad(-event.relative.y * mouse_sens))

func _physics_process(delta):
	if is_on_floor():
		is_falling = false
	# Add the gravity.
	if not is_on_floor():
		is_falling = true
		velocity += get_gravity() * delta

	# Handle jump.
	if Input.is_action_just_pressed("ui_accept") and is_on_floor() and not direction:
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	input_dir = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	# Half the movement speed when walking backwards
	var current_speed = SPEED
	if input_dir.y > 0:
		current_speed *= 0.5
	
	# If pressing movement keys, then move
	if direction and is_falling == false:			
		velocity.x = direction.x * current_speed
		velocity.z = direction.z * current_speed
		if Input.is_action_just_pressed("ui_accept") and is_on_floor():
			velocity.y = JUMP_VELOCITY
	elif !direction and is_falling == false:			
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)
	
	temp_vel_x = velocity.x
	temp_vel_z = velocity.z
	if is_falling:			
		velocity.x = temp_vel_x
		velocity.z = temp_vel_z
		
	move_and_slide()
