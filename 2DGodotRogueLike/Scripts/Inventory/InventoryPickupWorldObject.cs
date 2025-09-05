using Godot;
using Parts;
using Material = Materials.Material;

//can be either a unique item or a material
public partial class InventoryPickupWorldObject : Node2D
{
  [Export]
  public string inventoryObjectName;

  [Export]
  public bool isMaterial = true;
  [Export]
  public Material material;
  [Export]
  public int numMaterials;

  public ConstructedWeapon weapon;

  float vaccumRadius = 50.0f;
  float minRadius = 10.0f;
  Vector2 velocity;
  public AnimatedSprite2D animatedSprite;
  Area2D area2D;
  CollisionShape2D collisionShape2D;
  CpuParticles2D cpuParticles2D;
  PlayerManager playerManager;

  //be alive for 1 second before enabling collision
  float timeToCollide = 0.5f;

  [Export]
  public bool DrawDebugInfo;

  public override void _Ready() 
  {
    animatedSprite = GetNode("AnimatedSprite") as AnimatedSprite2D;
    area2D = GetNode("Area2D") as Area2D;
    collisionShape2D = GetNode("Area2D/CollisionShape2D") as CollisionShape2D;
    cpuParticles2D = GetNode("CPUParticles2D") as CpuParticles2D;
    //To get nodes in main scene from instanced scene need to use this instead of root.getnode
    playerManager = GetNode<PlayerManager>("/root/PlayerManagerSingletonNode");
    collisionShape2D.Disabled = true;
    animatedSprite.Frame = (int)material;
  }

  public override void _Draw()
  {
    if(DrawDebugInfo)
    {
      DrawCircle(Vector2.Zero, vaccumRadius, new Color(1,0,0,0.5f));
    }
  }

  public override void _Process(double delta)
  {
    timeToCollide -= (float)delta;
    if(timeToCollide < 0)
    {
      collisionShape2D.Disabled = false;
    }
    if(timeToCollide < 0 && GlobalPosition.DistanceSquaredTo(playerManager.topDownPlayer.GlobalPosition) < vaccumRadius*vaccumRadius
    && GlobalPosition.DistanceSquaredTo(playerManager.topDownPlayer.GlobalPosition) > minRadius * minRadius)
    {
      //Have the lerp speed up the closer to the player the objects are
      GlobalPosition = GlobalPosition.Lerp(playerManager.topDownPlayer.GlobalPosition, 0.02f);
    }
  }
}
