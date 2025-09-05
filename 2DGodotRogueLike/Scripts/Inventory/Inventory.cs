using System;
using System.Collections.Generic;
using Materials;
using Parts;

public class Inventory
{
  //How to deal with stack sizes?
  Dictionary<Material, int> stackableItems = new Dictionary<Material, int>();
  List<Tuple<string, ConstructedWeapon>> uniqueItems = new List<Tuple<string, ConstructedWeapon>>();

  protected PlayerManager playerManager;

  bool ranFirstTimeInit = false;

  public void AddMaterial(Material material, int count)
  {
    int currVal = 0;
    if(stackableItems.TryGetValue(material, out currVal))
    {
      stackableItems[material] = currVal + count;
    }
    else  //if material doesn't exist then make a new one and add it
    { 
      stackableItems.Add(material, count);
    }
    Console.WriteLine("Added " + count + " of Material " + material + " to Inventory");

    //Simply update the UI
    playerManager.topDownPlayer.playerInventoryUI.UpdateMaterialVisual(material, stackableItems[material]);
  }

  public void AddUniqueItem(string inventoryObjectName,	ConstructedWeapon weapon)
  {
    //TODO later selecting weapon brings up weapon menu to change stance/combo/special etc 
    uniqueItems.Add(new Tuple<string,	ConstructedWeapon>(inventoryObjectName, weapon));
    Console.WriteLine("Added Unique Item " + inventoryObjectName + " to Inventory");

    //Simply update the UI
    playerManager.topDownPlayer.playerInventoryUI.AddUniqueItemVisual(inventoryObjectName, weapon);
  }


  //Returns count of material
  public int GetMaterialCount(Material material)
  {
    int currVal = 0;

    //ignore return result, default is already 0
    stackableItems.TryGetValue(material, out currVal);

    return currVal;
  }

  //Returns whether the inventory has the material
  public bool HasMaterial(Material material, int count)
  {
    //deal with asking for 0 for some reason
    if(count == 0)
      return true;
  
    int currVal = 0;
    if(stackableItems.TryGetValue(material, out currVal))
    {
      return stackableItems[material] >= count;
    }
    //if we don't have the material then we can't have the material
    return false;
  }

  //Removes the material from the inventory and assumes the inventory has enough
  public void RemoveMaterial(Material material, int count)
  {
    //deal with removing 0 for some reason
    if(count == 0)
      return;

    int currVal = 0;
    if(stackableItems.TryGetValue(material, out currVal))
    {
      stackableItems[material] -= count;
    }
    //Simply update the UI
    playerManager.topDownPlayer.playerInventoryUI.UpdateMaterialVisual(material, stackableItems[material]);
  }


  //Ctor to construct base inv
  public Inventory(PlayerManager _playerManager)
  {
    playerManager = _playerManager;
    //iterate every material
    foreach(Material material in Enum.GetValues(typeof(Material)))
    {
      //For now just break when at bronze, only generate Iron -> Cobalt
      if(material == Material.Bronze)
      {
        break;
      }
      
      //Init to zero
      stackableItems.Add(material, 0);
    }
  }
}
