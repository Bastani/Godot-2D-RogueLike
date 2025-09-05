public partial class DownwardLadder : Interactable
{


  public override void _Ready()
  {
    base._Ready();
  }

  public override void StartInteract()
	{
    base.StartInteract();

    //Descend into a deeper level 
    playerManager.topDownPlayer.currentlySelectedUI = PlayerTopDown.CurrentlySelectedUI.EndLevelUI;
    //Close the game on interact
    //Popup UI to be like "Do you want Descend??"
    //GetTree.Quit();
	}

	//Called when player ends interaction with the object
	public override void EndInteract()
	{
    base.EndInteract();
	}

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _PhysicsProcess(double delta)
  {
    base._PhysicsProcess(delta);
  }
}
