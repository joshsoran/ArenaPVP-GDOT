using Godot;
using System;

public partial class FireBall : RigidBody3D
{
    [Export]
    private GpuParticles3D outerSphere;
    
    [Export]
    private GpuParticles3D innerBillboard;
    

    [Export]
    private CollisionShape3D collisionShape;

    private NetworkedPlayer owningPlayer;
    private Vector3 desiredDirection;
    private float desiredVelocity;

    public void InitializeFireball(NetworkedPlayer _owningPlayer, float _velocity, Vector3 _direction, Vector3 _scale)
    {
        //I need an idea of my owner so I dont magnetize toward them or deal damage to them
        //we don't have magnetization or even collision set up yet
        owningPlayer = _owningPlayer;
        //I also need an idea of waht direction and speed to go
        desiredDirection = _direction;
        desiredVelocity = _velocity;
        GlobalPosition = _owningPlayer.GlobalPosition + new Vector3(0, 1.0f, 0);
        
        collisionShape.Scale = _scale;
        outerSphere.Scale = _scale;
        innerBillboard.Scale = _scale;
        //change the shader paramaters based on charge amount (not really sure how to do this so I'll leave it for polish)
            //Main Texture Speed vec2
            //Main Texture Power float (voronoi intensity)
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
        //might be better to do ConstantForce() in the init
        MoveAndCollide(desiredDirection * desiredVelocity * (float)delta);
    }

}
