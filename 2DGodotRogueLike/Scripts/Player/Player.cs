using Godot;

public partial class Player : CharacterBody2D
{
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  Vector2 velocity;
  float gravity = 1300f;
  float horizontalMovementPower = 1000.0f;
  float jumpPower = -720.0f;
  
  float idleEpsilon = 10;

  bool grounded = true;


  //todo Doesnt quite work, need better way to detect if above fallable block
  bool OnTile;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    
  }


  public override void _Draw()
  {
      DrawLine(Position, Position + velocity, new Color(1, 0, 0, 1));
      DrawLine(Position, Position + new Vector2(0, 50), new Color(0, 1, 0, 1));

  }
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _PhysicsProcess(double delta)
  {

    AnimatedSprite2D animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    RayCast2D raycast2D = GetNode<RayCast2D>("RayCast2D");

    raycast2D.TargetPosition = new Vector2(0,50);

    //the raycast only collides with the second layer so only the floors
    grounded = IsOnFloor();
    OnTile = raycast2D.IsColliding();

    //update player movement
    if(Input.IsActionPressed("PlayerUp") && grounded)
    {
      velocity += new Vector2(0,jumpPower);
    }
    if(Input.IsActionPressed("PlayerDown"))
    {
      Position = new Vector2(Position.X, Position.Y + 1);
    }
    if(Input.IsActionPressed("PlayerRight"))
    {
      velocity += new Vector2(horizontalMovementPower,0) * (float)delta;
    }
    if(Input.IsActionPressed("PlayerLeft"))
    {
      velocity += new Vector2(-horizontalMovementPower,0) * (float)delta;
    }

    velocity.Y += gravity * (float)delta;

    
    // Use CharacterBody2D built-in Velocity with MoveAndSlide in Godot 4
    Velocity = velocity;
    MoveAndSlide();
    velocity = Velocity * 0.95f;
    if(grounded)
    {
      velocity.X *= 0.90f;
    }


  //if velocity x == 0 then dont change
    if(velocity.X > 0)
    {
      animatedSprite.FlipH = false;
    }
    else if(velocity.X < 0)
    {
      animatedSprite.FlipH = true;
    }
    
   
    //idle - if grounded and slow
    if(grounded)
    {
      if(velocity.X < idleEpsilon && velocity.X > -idleEpsilon)
      {
        animatedSprite.Play("Character Idle");
      }
      else
      {
        animatedSprite.Play("Character Run");
      }
    }
    //if not grounded
    else if(!OnTile)
    {
      if(velocity.Y < idleEpsilon && velocity.Y > -idleEpsilon)
      {
        animatedSprite.Play("Character Jump");
      }
      else 
      {
        animatedSprite.Play("Character Fall");
      }
    }
  }
 
}
