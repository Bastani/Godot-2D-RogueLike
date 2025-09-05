using Godot;

public partial class CameraMovement : Camera2D
{
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  [Export]
  public float zoomSensitivity = 1.0f;
  [Export]
  public float cameraMovementSens = 5.0f;
  
  [Export(PropertyHint.Range,"0,10")]
  public float cameraLerpWeight = 5f;

  [Export]
  public bool movementEnabled = true;

  [Export]
  public bool zoomEnabled = true;

  [Export]
  public bool followPlayer;
  
  InputManager inputManager;
  Vector2 cameraGoalPos = new Vector2(0,0);

  PlayerManager playerManager;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    inputManager = GetNode<InputManager>("/root/InputManagerSingletonNode");
    playerManager = GetNode<PlayerManager>("/root/PlayerManagerSingletonNode");
    cameraGoalPos = GlobalPosition;
  }

  public override void _Input(InputEvent inputEvent)
  {
    if(zoomEnabled)
    {
      if(inputEvent.IsActionPressed("MouseWheelDown"))
      {
        //make the zoom sensitivity a scale so 1 seems reasonable as a default
        Zoom = Zoom * (1.0f + 0.1f * zoomSensitivity);
      }
      if(inputEvent.IsActionPressed("MouseWheelUp"))
      {
        Zoom = Zoom * (1.0f - 0.1f * zoomSensitivity);
      }
      Scale = Zoom / 5.0f;
    }
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _PhysicsProcess(double delta)
  {
    if(followPlayer && playerManager.topDownPlayer != null)
    {
      cameraGoalPos = playerManager.topDownPlayer.GlobalPosition;
    }

    GlobalPosition = GlobalPosition.Lerp(cameraGoalPos, cameraLerpWeight);

    if(movementEnabled)
    {
      if(inputManager.IsKeyDown(Key.W))
      {
        cameraGoalPos += new Vector2(0,-1) * cameraMovementSens * (Scale.X);
      }
      if(inputManager.IsKeyDown(Key.S))
      {
        cameraGoalPos += new Vector2(0,1) * cameraMovementSens* (Scale.X);
      }
      if(inputManager.IsKeyDown(Key.D))
      {
        cameraGoalPos += new Vector2(1,0) * cameraMovementSens* (Scale.X);
      }
      if(inputManager.IsKeyDown(Key.A))
      {
        cameraGoalPos += new Vector2(-1,0) * cameraMovementSens* (Scale.X);
      }
    } 
  }
}
