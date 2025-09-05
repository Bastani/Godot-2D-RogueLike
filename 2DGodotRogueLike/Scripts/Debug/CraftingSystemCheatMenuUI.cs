using System;
using Godot;
using Material = Materials.Material;

public partial class CraftingSystemCheatMenuUI : Control
{
  PlayerManager playerManager;
  InputManager inputManager;
  OptionButton oreSpawnerSelectionOptionsButton;

  public void GivePlayerOre()
  {
    playerManager.playerInventory.AddMaterial((Material)oreSpawnerSelectionOptionsButton.Selected, 10);
  }
  
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    playerManager = GetNode<PlayerManager>("/root/PlayerManagerSingletonNode");
    inputManager = GetNode<InputManager>("/root/InputManagerSingletonNode");

    oreSpawnerSelectionOptionsButton = GetNode("VBoxContainer/HSplitContainer/OptionButton") as OptionButton;

    //Generate the options menu from the dict keys to make sure they are good with 0 still being no overlays
    foreach (Material item in Enum.GetValues(typeof(Material)))
    {
      if(item == Materials.Material.Bronze)
        break;

      oreSpawnerSelectionOptionsButton.AddItem(item.ToString());
    }
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if(inputManager.IsKeyPressed(Key.N))
    {
      Visible = !Visible;
    }
  }
}
