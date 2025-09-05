using Godot;

public partial class OptionsMenuPopupFix : OptionButton
{
  [Export]
  public Vector2I offset;
  public override void _Ready()
  {
      // -y up. In Godot 4, PopupMenu is a Window with Vector2I Size/Position
      offset = new Vector2I(0, GetPopup().Size.Y);
  }
  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    // Position in screen coordinates
    GetPopup().Position = (Vector2I)GlobalPosition + offset;
    // Keep on top in the OS window
    GetPopup().AlwaysOnTop = true;
  }
}
