using Godot;

public partial class TileSetAnimation : TileMap
{
  [Export]
  public TileSet tileSet;

  //3, 19, 20, 21
  //Arrays of custom types cannot be edited in the inspector currently so doing the next best thing, arrays of the data

  
  public const int maxNumAnimatedTiles = 10;
  [Export] 
  public int numAnimatedTiles = 1;

  //Starting state of the region
  public Rect2[] origionalRegion = new Rect2[maxNumAnimatedTiles];

  //Specific tile inside of the tileset
  [Export]
  public int[] tiles = new int[maxNumAnimatedTiles];
  
  //Max frames of animation
  [Export]
  public int[] maxFrame = new int[maxNumAnimatedTiles]{4,4,4,4,4,4,4,4,4,4};
  //internal current frame
  public int[] currentFrame = new int[maxNumAnimatedTiles];
  
  //Framerate of the animation
  [Export]
  public int[] animFramerate = new int[maxNumAnimatedTiles]{8,8,8,8,8,8,8,8,8,8};

  //internal calc to get the seconds per frame for the timer
  public float[] secondsPerFrame = new float[maxNumAnimatedTiles];

  //internal counter of time passed per frame
  public float[] currentTimePassed = new float[maxNumAnimatedTiles];


  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    // Godot 4: TileSet/TileMap API changed; runtime region animation via TileSet is no longer supported this way.
    // Keep arrays initialized and compute seconds per frame for potential future use.
    if (tileSet == null)
    {
      tileSet = this.TileSet;
    }
    for (int i = 0; i < numAnimatedTiles; i++)
    {
      // Store a default rect as placeholder; actual tile region access removed in Godot 4.
      origionalRegion[i] = new Rect2(0, 0, 0, 0);
      secondsPerFrame[i] = animFramerate[i] / 60.0f;
    }
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    // No-op animation stub to preserve behavior without using removed TileSet API.
    for (int i = 0; i < numAnimatedTiles; i++)
    {
      currentTimePassed[i] += (float)delta;
      if (currentTimePassed[i] > secondsPerFrame[i])
      {
        currentFrame[i] = (currentFrame[i] + 1) % maxFrame[i];
        currentTimePassed[i] = 0f;
      }
    }
  }
}

