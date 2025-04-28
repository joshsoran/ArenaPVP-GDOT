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

    public NetworkedPlayer owningPlayer;
    private Vector3 desiredDirection;
    private float desiredVelocity;
    private bool bIsCharging = true;

    public void ShootFireball(float _velocity, Vector3 _direction)
    {
        //I also need an idea of waht direction and speed to go
        desiredDirection = _direction;
        desiredVelocity = _velocity;
        
        bIsCharging = false;
        //change the shader paramaters based on charge amount (not really sure how to do this so I'll leave it for polish)
            //Main Texture Speed vec2
            //Main Texture Power float (voronoi intensity)
    }

    public void SetFireBallScale(Vector3 _scale)
    {
        collisionShape.Scale = _scale;
        outerSphere.Scale = _scale;
        innerBillboard.Scale = _scale;
    }

    public override void _Ready()
    {
        base._Ready();
        bIsCharging = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        //might be better to do ConstantForce() in the init
        if(bIsCharging)
        {
            GlobalPosition = owningPlayer.GlobalPosition + new Vector3(0, 1.0f, 0) - owningPlayer.GlobalTransform.Basis.Z;
        }
        else
        {
            MoveAndCollide(desiredDirection * desiredVelocity * (float)delta);
        }
        
    }

}
