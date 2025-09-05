using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Materials;
using Parts;
using Array = Godot.Collections.Array;
using Material = Materials.Material;

public partial class CraftingMaterialSystem : Control
{
  //Signals

  public void SwitchToIronOnButtonPressed()
  {
    ingot.Modulate = MaterialTints.tints[Materials.Material.Iron];
  }

  public void SwitchToCopperOnButtonPressed()
  {
    ingot.Modulate = MaterialTints.tints[Materials.Material.Copper];
  }

  public void SwitchToTinOnButtonPressed()
  {
    ingot.Modulate = MaterialTints.tints[Materials.Material.Tin];
  }

  public void SwitchToAdamantiteOnButtonPressed()
  {
    ingot.Modulate = MaterialTints.tints[Materials.Material.Adamantite];
  }


  public void _on_SelectMaterialsButton_toggled(bool toggled)
  {
    if(toggled)
    {
      SetModeMaterialSelection();
    }
    else
    {
      SetModePartSelection();
    }
  }

#region Variables
  //Dict of material tints to lookup of pieces

  //Dict of type to list of pieces
  System.Collections.Generic.Dictionary<PartType, Array<PartBlueprint>> allPartsDict = new System.Collections.Generic.Dictionary<PartType, Array<PartBlueprint>>();

  [Obsolete]
  List<PartBlueprint> currentParts = new List<PartBlueprint>();

  WeaponBlueprintNode weaponRootNode = new WeaponBlueprintNode();

  List<AttachPoint> attachPoints = new List<AttachPoint>();

  CallbackTextureButton ingot;

  BaseBlueprint selectedBlueprint;
  AttachPoint selectedAttachPoint;

  public CallbackTextureButton selectedPart;
  public WeaponBlueprintNode selectedWeaponBPNode;
  public Material selectedInventoryMaterial = Materials.Material.Undefined;

  System.Collections.Generic.Dictionary<string,BaseBlueprint> blueprints = new System.Collections.Generic.Dictionary<string,BaseBlueprint>();

  //Load packed scenes
  PackedScene CallbackTextureButtonScene = (PackedScene)ResourceLoader.Load("res://Scenes/BlueprintSystem/CallbackTextureButtonScene.tscn");
  PackedScene CallbackTextureButtonWithTextScene = (PackedScene)ResourceLoader.Load("res://Scenes/BlueprintSystem/CallbackTextureButtonWithTextScene.tscn");

  RichTextLabel currentBlueprintText;

  public System.Collections.Generic.Dictionary<Material, HBoxContainer> stackableItemsUI = new System.Collections.Generic.Dictionary<Material, HBoxContainer>();
  public GridContainer inventoryOres;

  string fontBBcodePrefix = "[center][b]";

  Texture2D attachPointTex;
  Bitmap attachPointBitmask;
  const string attachPointAssetStr = "res://Assets/Art/My_Art/AttachPoint.png";

  Node partVisualizerContainer;
  Node currentBlueprintDetailContainer;
  Node blueprintContainer;
  Node newPartSelectionContainer;

  Vector2 partVisualizerScale = new Vector2(4,4);

  //Minimum box size for each sprite, also means the max sprite size is 30x30 with 1 pixel border
  Vector2 MinPartSelectionSize = new Vector2(32,32);

  float basePartVisualizerScale = 8.0f;

  public enum CraftingSystemMode
  {
    PartSelection,
    MaterialSelection
  }

  public CraftingSystemMode currentMode = CraftingSystemMode.PartSelection;

  bool playerCanCraftWeapon;

  //Todo replace this with something significantly better..
  //It will help to change the type from TextureButton to derived class like the other BP thing with callbacks
  int partNum;

  //Paths
  const string FullBPDir = "res://Data/Blueprints/";
  const string FullSpriteDir = "res://Assets/Art/My_Art/BlueprintIcons/";

  Vector2 maxWeaponUIExtents = Vector2.Zero;
  Vector2 minWeaponUIExtents = Vector2.Inf;

  System.Collections.Generic.Dictionary<Material, int> costOfWeapon;

  PartStats summationStats;

  protected PlayerManager playerManager;

  #endregion

  void SetModeMaterialSelection()
  {
    currentMode = CraftingSystemMode.MaterialSelection;
    ClearPartSelection();
    GeneratePartVisualizerUIFromCurrentParts();

    GetNode<RichTextLabel>("PartSelection/BlueprintStuff/PartInformationTitle").Visible = false;
    GetNode<ScrollContainer>("PartSelection/BlueprintStuff/PartSelectionScrollContainer").Visible = false;

    GetNode<RichTextLabel>("PartSelection/BlueprintStuff/OreInInventoryTitle").Visible = true;
    GetNode<ScrollContainer>("PartSelection/BlueprintStuff/OreInventorySelection").Visible = true;
  }

  void SetModePartSelection()
  {
    currentMode = CraftingSystemMode.PartSelection;
    ClearPartSelection();
    GeneratePartVisualizerUIFromCurrentParts();

    GetNode<RichTextLabel>("PartSelection/BlueprintStuff/PartInformationTitle").Visible = true;
    GetNode<ScrollContainer>("PartSelection/BlueprintStuff/PartSelectionScrollContainer").Visible = true;

    GetNode<RichTextLabel>("PartSelection/BlueprintStuff/OreInInventoryTitle").Visible = false;
    GetNode<ScrollContainer>("PartSelection/BlueprintStuff/OreInventorySelection").Visible = false;
  }

  //Load parts into parts dictionary
  void LoadAllParts()
  {
    //Generate texture and bitmask for the attachment nodes
    attachPointTex = (Texture2D)GD.Load(attachPointAssetStr);
    attachPointBitmask = new Bitmap();
    attachPointBitmask.CreateFromImageAlpha(attachPointTex.GetImage());

    //https://www.c-sharpcorner.com/article/loop-through-enum-values-in-c-sharp/
    //For each Piece type generate an array in the pieces dict
    foreach (PartType type in Enum.GetValues(typeof(PartType)))
    {
      if(type == PartType.Undefined)
        continue;
      allPartsDict[type] = new Array<PartBlueprint>();
    }

    Array<PartBlueprint> createdParts = new Array<PartBlueprint>();

    //Read json file into text
    FileAccess file = FileAccess.Open("res://Data/PartsList.json", FileAccess.ModeFlags.Read);
    string jsonText = file.GetAsText();
    file.Close();

    //Construct dict of stuff
    Array ParsedData = Json.ParseString(jsonText).Obj as Array;


    //slashDamage": -40,
    //"stabDamage": 20,
    //"attackWindUp": 10,
    //"attackWindDown": 10,
    //"length": 0,

    //Parse data based on Resource
    foreach (Dictionary data in ParsedData)
    {
      PartBlueprint partBP = new PartBlueprint();
      partBP.name = (string)data["partName"];
      partBP.texture = (Texture2D)GD.Load((string)data["partTextureDir"]);

      if(partBP.texture == null)
      {
        throw new Exception("Missing Texture :\"" + data["partTextureDir"] +"\"");
      }

      partBP.partType = PartTypeConversion.FromString((string)data["partType"]);

      //Don't ask
      partBP.materialCost = (int)(float)data["partCost"];
      //Ok, it's because json uses floats only so object -> float -> int


      Dictionary basicAttachPt = (Dictionary)data["baseAttachPoint"];
      partBP.baseAttachPoint = new Vector2((int)(float)basicAttachPt["x"],(int)(float)basicAttachPt["y"]);

      Dictionary partAttributes = (Dictionary)data["partAttributes"];
      partBP.stats.slashDamage =      (float)partAttributes["slashDamage"];
      partBP.stats.stabDamage =       (float)partAttributes["stabDamage"];
      partBP.stats.attackWindUp =     (float)partAttributes["attackWindUp"];
      partBP.stats.attackWindDown =   (float)partAttributes["attackWindDown"];
      partBP.stats.length =           (float)partAttributes["length"];
      partBP.stats.specialStat =      (string)partAttributes["specialStat"];

      foreach (Dictionary partAttachPoints in (Array)data["partAttachPoints"])
      {
        Array<PartType> types = new Array<PartType>();
        int x = (int)(float)partAttachPoints["x"];
        int y = (int)(float)partAttachPoints["y"];
        foreach (var item in (Array)partAttachPoints["types"])
        {
          types.Add(PartTypeConversion.FromString((string)item));
        }
        partBP.partAttachPoints.Add(new AttachPoint(new Vector2(x,y),types));
      }

      //Generate bitmap from texture data
      Bitmap newBMP = new Bitmap();

      newBMP.CreateFromImageAlpha(partBP.texture.GetImage());
      partBP.bitMask = newBMP;

      if(partBP.name == "TBD")
        continue;
      //Add to the GRAND parts dictionary
      allPartsDict[partBP.partType].Add(partBP);
    }
  }

  //Load the Blueprints from the json file into the blueprints dictionary
  void LoadBlueprints()
  {

    //Read json file into text
    var file = FileAccess.Open("res://Data/Blueprints.json", FileAccess.ModeFlags.Read);
    string jsonText = file.GetAsText();
    file.Close();

    //Construct dict of stuff
    Array ParsedData = Json.ParseString(jsonText).Obj as Array;

    //Parse data based on Resource
    foreach (Dictionary data in ParsedData)
    {
      BaseBlueprint partBP = new BaseBlueprint();
      partBP.name = (string)data["blueprintName"];
      partBP.texture = (Texture2D)GD.Load((string)data["blueprintIconSprite"]);

      foreach (Dictionary subData in (Array)data["blueprintRequiredPieces"])
      {
        partBP.requiredPieces.Add(PartTypeConversion.FromString((string)subData["partType"]));
      }
      blueprints.Add(partBP.name, partBP);
    }
  }

  //Generate a callback button from a weapon blueprint piece
  CallbackTextureButton CreateCallbackButtonFromBlueprint(PartBlueprint blueprint, BasicCallback callback, Vector2 size, bool useBitmask = false, bool useColors = true, bool setMinSize = false)
  {
    //Generate individual part buttons
    CallbackTextureButton BPPieceButton = CallbackTextureButtonScene.Instantiate() as CallbackTextureButton;
    //Generate a unique name for the part
    BPPieceButton.Name = blueprint.name + partNum++;

    //Set the size of the rect and need this stuff to get it to expand
    BPPieceButton.Size = blueprint.texture.GetSize();  //size of tex
    BPPieceButton.Scale = size;   //new scale
    //BPPieceButton.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
    //
    if(setMinSize)
      BPPieceButton.CustomMinimumSize = BPPieceButton.CustomMinimumSize * BPPieceButton.CustomMinimumSize;
    else
      BPPieceButton.CustomMinimumSize = BPPieceButton.CustomMinimumSize;

    //BPPieceButton.Position -= BPPieceButton.Size * BPPieceButton.Scale / 2.0f;

    //Set textures and bitmasks to the default part's texture and its bitmask
    BPPieceButton.TextureNormal = blueprint.texture;

    BPPieceButton.onButtonPressedCallback = callback;
    BPPieceButton.changeColors = useColors;
    BPPieceButton.Modulate = BPPieceButton.defaultColor;

    if(useBitmask)
      BPPieceButton.TextureClickMask = blueprint.bitMask;

    return BPPieceButton;
  }

  //Create the callback button from an attachment point on the weapon creation node
  CallbackTextureButton CreateCallbackButtonFromAttachmentPoint(AttachPoint attachPoint, WeaponBlueprintNode node, BasicCallback callback, Vector2 partRectSize, bool useColors = true, bool setMinSize = false)
  {
    //Generate individual part buttons
    CallbackTextureButton newAttachpoint = CallbackTextureButtonScene.Instantiate() as CallbackTextureButton;
    //Generate a unique name for the part

    //Set the size of the rect and need this stuff to get it to expand
    newAttachpoint.Size = attachPointTex.GetSize();  //size of tex
    newAttachpoint.Scale = partVisualizerScale;   //new scale
    //newAttachpoint.StretchMode = TextureButton.StretchModeEnum.KeepAspect;

    if(setMinSize)
      newAttachpoint.CustomMinimumSize = newAttachpoint.Size * newAttachpoint.Scale;
    else
      newAttachpoint.CustomMinimumSize = newAttachpoint.Size;

    //Set textures and bitmasks to the default part's texture and its bitmask
    newAttachpoint.TextureNormal = attachPointTex;

    newAttachpoint.Position = (node.currentOffset + attachPoint.pos - (attachPointTex.GetSize() / 2.0f)) * partVisualizerScale;

    //TODO need to fix the usage of currentOffset for the
    //if odd then move a bit
    //if(newAttachpoint.Size.x % 2 == 1 && (maxWeaponUIExtents.x-minWeaponUIExtents.x) % 2 == 0)
    //{
    //  newAttachpoint.Position += new Vector2(0.5f * partVisualizerScale.X,0);
    //}
    //if(newAttachpoint.Size.y % 2 == 1 && (maxWeaponUIExtents.y-minWeaponUIExtents.y) % 2 == 0)
    //{
    //  newAttachpoint.Position += new Vector2(0,0.5f * partVisualizerScale.y);
    //}

    //This fixed everything???
    newAttachpoint.Position += new Vector2(0.5f * partVisualizerScale.X,0.5f * partVisualizerScale.Y);


    newAttachpoint.onButtonPressedCallback = callback;
    newAttachpoint.changeColors = useColors;
    newAttachpoint.Modulate = new Color(0,0.8f,0);
    newAttachpoint.TextureClickMask = attachPointBitmask;

    return newAttachpoint;
  }

  //Clear the parts visualizer's children
  void ClearPartsVisualizer()
  {
    selectedPart = null;
    //Queue all current children to be deleted
    foreach (Node child in partVisualizerContainer.GetChildren())
    {
      partVisualizerContainer.RemoveChild(child);
      child.QueueFree();
    }
  }

  //Clear blueprint details UI
  void ClearCurrentBlueprintDetails()
  {
    foreach (Node child in currentBlueprintDetailContainer.GetChildren())
    {
      currentBlueprintDetailContainer.RemoveChild(child);
      child.QueueFree();
    }
  }

  //Clears the parts selection UI
  void ClearPartSelection()
  {
    //Queue all current children to be deleted
    foreach (Node child in newPartSelectionContainer.GetChildren())
    {
      if(child as HSeparator != null)
        continue;

      newPartSelectionContainer.RemoveChild(child);
      child.QueueFree();
    }
  }

  //Generate a new blueprint so the array can be moved into a FinishedBP list
  PartBlueprint CreatePartBlueprintFromType(PartType partType)
  {
    return new PartBlueprint(allPartsDict[partType][0]);
  }

  //Generate a blueprint button from
  [Obsolete]
  void GenerateBlueprintButton(BaseBlueprint loadedBP)
  {

    //Configure Button
    CallbackTextureButton newCallbackTextureButton = CallbackTextureButtonScene.Instantiate() as CallbackTextureButton;
    newCallbackTextureButton.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
    newCallbackTextureButton.TextureNormal = loadedBP.texture;
    newCallbackTextureButton.blueprint = loadedBP.name;
    newCallbackTextureButton.SetSize(new Vector2(5,5));
    newCallbackTextureButton.changeColors = true;

    //Setup on pressed callback func
    newCallbackTextureButton.onButtonPressedCallback = () =>
    {
      //if we are selecting the same BP that we already have selected then break early
      if(selectedBlueprint == blueprints[newCallbackTextureButton.blueprint])
        return;
      //Update selected blueprint and the selected BP stuff
      selectedBlueprint = blueprints[newCallbackTextureButton.blueprint];
      currentBlueprintText.Text = fontBBcodePrefix + selectedBlueprint.name;

      //Clear current parts
      currentParts.Clear();
      //Add new piece icons
      foreach (var part in selectedBlueprint.requiredPieces)
      {
        currentParts.Add(allPartsDict[part][0]);//CreatePartBlueprintFromType(part));
      }
      currentParts.Sort(PartTypeConversion.CompareParts);

      //Clear part selection as well
      ClearPartSelection();
      GeneratePartVisualizerUIFromCurrentParts();
    };

    //Load the icons
    blueprintContainer.AddChild(newCallbackTextureButton);
  }

  public void UpdateCurrentlySelectedPart(CallbackTextureButton newSelectedPart)
  {
    if(newSelectedPart != null)
    {
      newSelectedPart.Modulate = newSelectedPart.pressedColor;
      newSelectedPart.changeColors = false;
    }
    if(selectedPart != null)
    {
      selectedPart.Modulate = selectedPart.defaultColor;
      selectedPart.changeColors = true;
    }
    selectedPart = newSelectedPart;
  }

  void UpdateCurrentlySelectedAttachPoint(AttachPoint attachPoint, CallbackTextureButton newAttachPointButton)
  {
    if(attachPoint != null && newAttachPointButton != null)
    {
      newAttachPointButton.Modulate = new Color(0,1,0);
      newAttachPointButton.changeColors = false;
    }
    if(selectedAttachPoint != null && newAttachPointButton != null)
    {
      newAttachPointButton.Modulate = new Color(1,1,1);
      newAttachPointButton.changeColors = true;
    }
    selectedAttachPoint = attachPoint;
  }

  void SetSelectedAttachPoint(AttachPoint attachPoint, CallbackTextureButton newAttachPointButton, WeaponBlueprintNode node)
  {
    //Reset selected part
    UpdateCurrentlySelectedPart(null);
    selectedWeaponBPNode = null;
    UpdateCurrentlySelectedAttachPoint(attachPoint, newAttachPointButton);
    selectedAttachPoint = attachPoint;
    ClearPartSelection();
    if(currentMode == CraftingSystemMode.PartSelection)
    {
      LoadPartSelectionAttachPoint(attachPoint, node, newAttachPointButton);
    }
    //Material Selection
  }

  void GenerateAttachPointsUIFromPart(WeaponBlueprintNode node, Vector2 partRectSize)
  {
    foreach (var attachPoint in node.part.partAttachPoints)
    {
      //Only generate a new button if there is an open attachment slot
      if(!attachPoint.attachedPart)
      {
        //Generate green attach point
        CallbackTextureButton newAttachPointButton = default;
        newAttachPointButton = CreateCallbackButtonFromAttachmentPoint(attachPoint, node,() => {SetSelectedAttachPoint(attachPoint, newAttachPointButton, node);}, partRectSize);

        partVisualizerContainer.AddChild(newAttachPointButton);
        //Set callback to SetSelectedAttachPoint
      }
    }
  }

  void GeneratePartsFromWeaponBPNode(WeaponBlueprintNode node, Vector2 baseOffset)
  {
    //Loads the part visualizer with callbacks to load the part selections
    //cannot pass BPPieceButton to the functor so need to initialize it to an object.
    CallbackTextureButton BPPieceButton = default;
    BPPieceButton = CreateCallbackButtonFromBlueprint(node.part, () =>
    {
      if(BPPieceButton != selectedPart)
      {
        UpdateCurrentlySelectedPart(BPPieceButton);
        selectedWeaponBPNode = node;
        UpdateCurrentlySelectedAttachPoint(null, null);
        ClearPartSelection();
        if(currentMode == CraftingSystemMode.PartSelection)
        {
          LoadPartSelection(node);
        }
        else
        {
          //Set material to Undefined
          selectedInventoryMaterial = Materials.Material.Undefined;
          //Tell player to Select Material
          //Material Selection
        }
      }
    }, partVisualizerScale, true);


    //places the location - the attach point because attachpoint is -vector to move image so its 0,0 is the attach pt + the base offset
    node.currentOffset = -node.part.baseAttachPoint + baseOffset;
    BPPieceButton.Position = node.currentOffset * partVisualizerScale;

    //if odd then move a bit
    //Weirdly this works perfectly with the attachment points but makes the problem worse here
    //if(BPPieceButton.Size.x % 2 == 1)
    //{
    //  BPPieceButton.Position += new Vector2(0.5f * partVisualizerScale.X,0);
    //}
    //if(BPPieceButton.Size.y % 2 == 1)
    //{
    //  BPPieceButton.Position += new Vector2(0,0.5f * partVisualizerScale.y);
    //}

    WeaponBlueprintNode parentNode = node.parent;

    //If not undefined than don't change the color
    if(currentMode == CraftingSystemMode.PartSelection)
    {
      BPPieceButton.Modulate = MaterialTints.tints[node.part.currentMaterial];
    }

    //If not undefined than don't change
    if(currentMode == CraftingSystemMode.MaterialSelection)
    {
      BPPieceButton.Modulate = MaterialTints.tints[node.part.currentMaterial];
    }

    partVisualizerContainer.AddChild(BPPieceButton);


    //Don't place attachment points in the material selection
    if(currentMode == CraftingSystemMode.PartSelection)
    {
      GenerateAttachPointsUIFromPart(node, BPPieceButton.Size * BPPieceButton.Scale / 2.0f);
    }

    foreach (var item in node.children)
    {
      //- attach pt as its inverted
      GeneratePartsFromWeaponBPNode(item.Value, -node.part.baseAttachPoint + item.Key.pos + baseOffset);
    }
  }

  //recursively get the largest UI extents
  void GetLargestUIExtents(WeaponBlueprintNode node)
  {
    //Set the bounding box of the weapon so we can rescale the UI
    maxWeaponUIExtents = new Vector2(Mathf.Max(node.currentOffset.X + node.part.texture.GetSize().X, maxWeaponUIExtents.X), Mathf.Max(node.currentOffset.Y + node.part.texture.GetSize().Y, maxWeaponUIExtents.Y));
    minWeaponUIExtents = new Vector2(Mathf.Min(node.currentOffset.X - node.part.texture.GetSize().X, minWeaponUIExtents.X), Mathf.Min(node.currentOffset.Y - node.part.texture.GetSize().Y, minWeaponUIExtents.Y));
    foreach (var item in node.children)
    {
      GetLargestUIExtents(item.Value);
    }
  }

  void AccumulateStats(WeaponBlueprintNode node, ref PartStats summationStats)
  {
    summationStats = PartStats.GetCombinationOfStats(summationStats, node.part.stats, node.part.currentMaterial);
    foreach (var item in node.children)
    {
      AccumulateStats(item.Value, ref summationStats);
    }
  }

  public void GeneratePartVisualizerUIFromCurrentParts()
  {
    maxWeaponUIExtents = Vector2.Zero;
    minWeaponUIExtents = Vector2.Inf;

    //We need to update the currentOffset before we get the extents
    GeneratePartsFromWeaponBPNode(weaponRootNode, -weaponRootNode.part.texture.GetSize() / 2.0f);
    GetLargestUIExtents(weaponRootNode);

    ClearPartsVisualizer();
    ClearCurrentBlueprintDetails();

    summationStats = new PartStats();

    Console.WriteLine("Max Weapon UI Extents Part Visualizer Scale is " + maxWeaponUIExtents);
    Console.WriteLine("Min Weapon UI Extents Part Visualizer Scale is " + minWeaponUIExtents);

    //max - min = dist
    Vector2 weaponUIExtents = maxWeaponUIExtents - minWeaponUIExtents;
    Console.WriteLine("Weapon UI Extents Part Visualizer Scale is " + weaponUIExtents);

    //get the larget axis, MaxAxis returns the largest axis not the number of it
    float weaponUIMaxScale = Mathf.Max(weaponUIExtents.X, weaponUIExtents.Y);

    Console.WriteLine("Weapon UI Extents Scale is " + weaponUIMaxScale);
    //hardcoded expected 32 to be the largest size so divide the max by the current to get the multiplier * the scale at 32 length gives us the new scalar (instead of 4)
    //And round it so that we don't have any float shenannagins
    float newScale = Mathf.Round((32.0f/weaponUIMaxScale) * basePartVisualizerScale * 100.0f)/100.0f;

    partVisualizerScale = new Vector2(newScale,newScale);

    Console.WriteLine("New Part Visualizer Scale is " + partVisualizerScale.X.ToString());
    GeneratePartsFromWeaponBPNode(weaponRootNode, -weaponRootNode.part.texture.GetSize() / 2.0f);
    //GeneratePartsFromWeaponBPNode(weaponRootNode, -weaponRootNode.part.texture.GetSize() / 2.0f + (weaponUIExtents / 2.0f) / partVisualizerScale);

    AccumulateStats(weaponRootNode, ref summationStats);

    RichTextLabel bpDetails = new RichTextLabel();
    currentBlueprintDetailContainer.AddChild(bpDetails);
    bpDetails.BbcodeEnabled = true;

    //Only write text if we have parts
    //if(currentParts.Count >= 1)
    bpDetails.Text = summationStats.GenerateStatText(null, 0, false);

    bpDetails.CustomMinimumSize = new Vector2(32,50);
    bpDetails.ClipContents = false;
  }

  public override void _Draw()
  {

    Vector2 center = ((Control)partVisualizerContainer).GlobalPosition;// + (partVisualizerContainer as Control).Size * 0.5f;
    //Vector2 center = (partVisualizerContainer as Control).GlobalPosition + (partVisualizerContainer as Control).Size * 0.5f;

    //Debug lines
    //DrawLine(center + minWeaponUIExtents, center + minWeaponUIExtents + new Vector2(0,100),new Color("fc0303"),2);   //Pos Y
    //DrawLine(center + maxWeaponUIExtents, center + maxWeaponUIExtents + new Vector2(0,-100),new Color("fcdb03"),2);  //Neg Y
    //DrawLine(center + minWeaponUIExtents, center + minWeaponUIExtents + new Vector2(100,0),new Color("0345fc"),2);   //Pos X
    //DrawLine(center + maxWeaponUIExtents, center + maxWeaponUIExtents + new Vector2(-100,0),new Color("fc03ce"),2);  //Neg X
  }


  public override void _Ready()
  {
    //TODO: Hardcode path so no search
    partVisualizerContainer = GetNode("PartsVisualizerContainer");
    currentBlueprintDetailContainer = GetNode("PartDetailContainer");
    blueprintContainer = GetNode("GridBlueprints") as HBoxContainer;
    newPartSelectionContainer = GetNode("NewPartSelectionContainer");
    currentBlueprintText = GetNode("CurrentBPTitle") as RichTextLabel;
    inventoryOres = GetNode("OreInventoryGridContainer") as GridContainer;

    playerManager = GetNode<PlayerManager>("/root/PlayerManagerSingletonNode");

    LoadAllParts();
    //Start the current part as a empty handle
    //currentParts.Add(allPartsDict[Parts.PartType.Handle][0]);
    weaponRootNode.part = allPartsDict[PartType.Handle][0];

    Color ironBaseColor = new Color("e8e8e8");
    //Materials.MaterialTints.tints = data from file
    //Pieces = data from file
    //genuinely using hex str to int

    //For each BP in BP folder, load them

    //Load sprites for bp's
    if(DirAccess.Open(FullSpriteDir) == null)
    {
      throw(new Exception("Broke loading BP sprite Icons"));
    }

    //Blueprints are deprecated
    //Load blueprint resources
    //foreach (var blueprint in blueprints)
    //{
    //  GenerateBlueprintButton(blueprint.Value);
    //}

    //Set initial state to MaterialSelection
    SetModePartSelection();
  }


  public override void _Process(double delta)
  {
    //Call update which calls _Draw
    QueueRedraw();
  }

  //Updates dict with material cost
  public void GetWeaponMaterialCost(Inventory playerInventory)
  {
    costOfWeapon = new System.Collections.Generic.Dictionary<Material, int>();
    List<string> partsMissingMaterials = new List<string>();
    if(IsWeaponsMaterialsSelected(weaponRootNode, ref partsMissingMaterials))
    {
      costOfWeapon = GetCostOfWeaponNode(weaponRootNode);
    }
    else
    {
      string partsMissingMaterialsString = string.Join(string.Empty, partsMissingMaterials);
      GetNode<RichTextLabel>("ParchmentBackground/CurrentWeaponInfo").Text = "Select Materials for Parts: " + partsMissingMaterialsString;
      costOfWeapon = null;
    }

    string missingMaterials = string.Empty;

    if(costOfWeapon != null)
    {
      playerCanCraftWeapon = true;
      //Check playerInventory against items
      foreach (var item in costOfWeapon)
      {
        if(!playerInventory.HasMaterial(item.Key, item.Value))
        {
          missingMaterials += "Missing " + (item.Value - playerInventory.GetMaterialCount(item.Key)) + " pieces of " + item.Key + "\n";
          playerCanCraftWeapon = false;
        }
      }
      GetNode<RichTextLabel>("ParchmentBackground/CurrentWeaponInfo").Text = missingMaterials;
    }

    GetNode<Button>("CraftButton").Disabled = !playerCanCraftWeapon;
  }

  //Simply recursively check if the weapon has its material selected
  public bool IsWeaponsMaterialsSelected(WeaponBlueprintNode node, ref List<string> partsMissingMaterials)
  {
    bool materialsSelected = true;

    if(node.part.currentMaterial == Materials.Material.Undefined)
    {
      materialsSelected = false;
      partsMissingMaterials.Add(node.part.name);
    }

    foreach(var child in node.children)
    {
      if(!IsWeaponsMaterialsSelected(child.Value, ref partsMissingMaterials))
        materialsSelected = false;
    }
    return materialsSelected;
  }

  public System.Collections.Generic.Dictionary<Material, int> GetCostOfWeaponNode(WeaponBlueprintNode node)
  {
    System.Collections.Generic.Dictionary<Material, int> currentCost = new System.Collections.Generic.Dictionary<Material, int>();

    currentCost.Add(node.part.currentMaterial, node.part.materialCost);

    foreach(var child in node.children)
    {
      System.Collections.Generic.Dictionary<Material, int> childCost = GetCostOfWeaponNode(child.Value);

      //Combine dicts
      foreach (var childVal in childCost)
      {
        int currVal = 0;
        if(currentCost.TryGetValue(childVal.Key, out currVal))
        {
          currentCost[childVal.Key] += childVal.Value;
        }
        else
        {
          currentCost.Add(childVal.Key, childVal.Value);
        }
      }
    }
    return currentCost;
  }

  public static int GetMinYSizeFromRichTextLabel(RichTextLabel label)
  {
    //min size is num lines * font size + spacings
    return (1 + label.Text.Count("\n")) * ((label.Theme.DefaultFontSize) + (label.Theme.DefaultFont).GetSpacing(TextServer.SpacingType.Bottom) + (label.Theme.DefaultFont).GetSpacing(TextServer.SpacingType.Top));
  }

  //callback from create weapon button, only available when the player can craft a weapon
  public void CreateWeapon()
  {
    string weaponName = "New Created Weapon";
    //For now just set a basic sprite as the image
    ConstructedWeapon newWeapon = new ConstructedWeapon(weaponName, summationStats, ResourceLoader.Load<Texture2D>("res://Assets/Art/My_Art/BlueprintIcons/Medium_Sword.png"), summationStats.GenerateStatText(null, 0, false));
    playerManager.playerInventory.AddUniqueItem(weaponName, newWeapon);

    System.Collections.Generic.Dictionary<Material, int> weaponCost =  GetCostOfWeaponNode(weaponRootNode);

    //When crafting then weapon charge the player
    foreach (var cost in weaponCost)
    {
      playerManager.playerInventory.RemoveMaterial(cost.Key, cost.Value);
    }

    //Reset the weaponRootNode
    ResetWeapon();
    //Close the inventory??
  }

  public void ResetWeapon()
  {
    weaponRootNode = new WeaponBlueprintNode();
    weaponRootNode.part = allPartsDict[PartType.Handle][0];
    GetNode<RichTextLabel>("ParchmentBackground/CurrentWeaponInfo").Text = string.Empty;

    //Reset attach points
    foreach (var attachPt in weaponRootNode.part.partAttachPoints)
    {
      attachPt.attachedPart = false;
    }
    SetModePartSelection();
  }

  class SpriteInformation
  {
    public Vector2 size = Vector2.Zero;
    public Vector2 bottomLeft = Vector2.Zero;
  }

  //TODO
  Image CreateWeaponSprite()
  {
    return GetWeaponSpriteInfo(weaponRootNode);
  }


  //Need data + size + offset
  Image GetWeaponSpriteInfo(WeaponBlueprintNode node)
  {
    Image weaponSprite = new Image();
        //TODO construct weapon sprite stuff here, the pain of finding the bounds of the weapon and placing the pixels correctly

    return weaponSprite;
  }


  //Loads the list of all possible parts of the passed part blueprint
  public void LoadPartSelection(WeaponBlueprintNode currentNode)
  {
    //Load all parts of this type
    foreach (var part in allPartsDict[currentNode.part.partType])
    {
      //Load part as clickable button with callback to set the current piece of the current blueprint as this piece
      CallbackTextureButton partSelectionButton = CreateCallbackButtonFromBlueprint(part, () =>
      {
        ClearPartSelection();
        currentNode.IterateNode(currNode =>
        {
          foreach (var attachPt in currNode.part.partAttachPoints)
          {
              attachPt.attachedPart = false;
          }
        });

        currentNode.ClearNodeChildren();
        currentNode.part = part;
        GeneratePartVisualizerUIFromCurrentParts();
      }, new Vector2(1,1), false, true, true);

      partSelectionButton.Modulate = partSelectionButton.defaultColor;

      /////////////////////////////////////////////////////////////////////////////////////////////////
      //Generate Detail Sprites
      HBoxContainer hBox = CallbackTextureButtonWithTextScene.Instantiate() as HBoxContainer;

      Node partIconParentNode = hBox.GetNode("VBoxContainer/HSplitContainer");
      Node node = partIconParentNode.GetNode("PartIcon");
      partIconParentNode.RemoveChild(partIconParentNode.GetNode("PartIcon"));     //Remove current selection button
      node.QueueFree();                       //Free node
      partIconParentNode.AddChild(partSelectionButton);     //add constructed obj
      partIconParentNode.MoveChild(partSelectionButton,0);  //move to pos 0

      partSelectionButton.CustomMinimumSize = MinPartSelectionSize;

      RichTextLabel detailText = hBox.GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/PartData");
      detailText.Text = part.stats.GenerateStatText(currentNode.part);
      detailText.BbcodeEnabled = true;
      detailText.CustomMinimumSize = new Vector2(detailText.CustomMinimumSize.X,GetMinYSizeFromRichTextLabel(detailText));
      //Dont change colors with the callbacks
      newPartSelectionContainer.AddChild(hBox);
    }
  }

  //Loads the list of all possible parts of the passed attachment point
  public void LoadPartSelectionAttachPoint(AttachPoint attachPoint, WeaponBlueprintNode parentNode, CallbackTextureButton newAttachPointButton)
  {
    foreach (var partType in attachPoint.partTypes)
    {
      //Load all parts of this type
      foreach (var part in allPartsDict[partType])
      {
        //Load part as clickable button with callback to set the current piece of the current blueprint as this piece
        CallbackTextureButton partSelectionButton = CreateCallbackButtonFromBlueprint(part, () =>
        {
          ClearPartSelection();
          attachPoint.attachedPart = true;
          partVisualizerContainer.RemoveChild(newAttachPointButton);
          //Set the x/y pos of the attach point to the actual node that represents the part
          WeaponBlueprintNode newNode = new WeaponBlueprintNode(part, parentNode);
          parentNode.children.Add(attachPoint, newNode);
          GeneratePartVisualizerUIFromCurrentParts();
        }, new Vector2(1,1));

        partSelectionButton.Modulate = partSelectionButton.defaultColor;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        //Generate Detail Sprites
        HBoxContainer hBox = CallbackTextureButtonWithTextScene.Instantiate() as HBoxContainer;

        Node partIconParentNode = hBox.GetNode("VBoxContainer/HSplitContainer");
        Node node = partIconParentNode.GetNode("PartIcon");
        partIconParentNode.RemoveChild(partIconParentNode.GetNode("PartIcon"));     //Remove current selection button
        node.QueueFree();                       //Free node
        partIconParentNode.AddChild(partSelectionButton);     //add constructed obj
        partIconParentNode.MoveChild(partSelectionButton,0);  //move to pos 0

        partSelectionButton.CustomMinimumSize = MinPartSelectionSize;

        RichTextLabel detailText = hBox.GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/PartData");
        detailText.Text = part.stats.GenerateStatText();
        detailText.BbcodeEnabled = true;
        detailText.CustomMinimumSize = new Vector2(detailText.CustomMinimumSize.X, GetMinYSizeFromRichTextLabel(detailText));
        //Dont change colors with the callbacks
        newPartSelectionContainer.AddChild(hBox);
      }
    }
  }
}
