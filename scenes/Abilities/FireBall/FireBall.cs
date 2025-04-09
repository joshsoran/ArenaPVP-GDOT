using Godot;
using System;

public partial class FireBall : RigidBody3D
{
    [Export]
    private GpuParticles3D outerSphere;

    private NetworkedPlayer owningPlayer;
    private Vector3 desiredDirection;
    private float desiredVelocity;

    public void InitializeFireball(NetworkedPlayer _owningPlayer, float _velocity, Vector3 _direction)
    {
        //I need an idea of my owner so I dont magnetize toward them or deal damage to them
        owningPlayer = _owningPlayer;
        //I also need an idea of waht direction and speed to go
        desiredDirection = _direction;
        desiredVelocity = _velocity;
        GlobalPosition = _owningPlayer.GlobalPosition + new Vector3(0, 1.0f, 0);
    }

    public override void _Ready()
    {
        base._Ready();
        //Vector3 motion = 
        //MoveAndCollide();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        MoveAndCollide(desiredDirection * desiredVelocity * (float)delta);
    }

}
