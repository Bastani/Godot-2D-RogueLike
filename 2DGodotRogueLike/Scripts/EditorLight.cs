using Godot;

public partial class EditorLight : Node2D
{
    public Sprite2D spriteToLight;
    public Node2D parentNode;

    [Export]
    public double rotateSpeed = 1.0f;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if(spriteToLight == null)
        {
            spriteToLight = GetParent().GetParent() as Sprite2D;
        }
        parentNode = GetParent() as Node2D;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        parentNode.Rotate((float)(rotateSpeed * delta));
        Rotate((float)(-rotateSpeed * delta));

        //if(spriteToLight != null)
        //{
        //    Vector2 lightDir = GlobalPosition - spriteToLight.GlobalPosition;
        //    (spriteToLight.Material as ShaderMaterial)?.SetShaderParameter("basicLightDir", new Vector3(lightDir.X,-lightDir.Y,5));
        //}
    }
}

