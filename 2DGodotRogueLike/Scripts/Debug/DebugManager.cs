using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public enum MouseOptions
{
  AStar_Pathfind,
  AStar_NodeInfo,
  DirectedGraph_ToParent,
  Character_Select,
}


public partial class DebugManager : Node2D
{

  public void MouseOptionsSelected_Callback(int index)
  {
    currentMouseOption = (MouseOptions)index;

    AStarInfoUI.Visible = false;
    CharacterInformationInfoUI.Visible = false;

    if(currentMouseOption == MouseOptions.AStar_NodeInfo)
    {
      AStarInfoUI.Visible = true;
    }
    else if(currentMouseOption == MouseOptions.AStar_Pathfind)
    {
      
    }
    else if(currentMouseOption == MouseOptions.Character_Select)
    {
      CharacterInformationInfoUI.Visible = true;
    }
    else if(currentMouseOption == MouseOptions.DirectedGraph_ToParent)
    {

    }
  }

#region Variables

  public MouseOptions currentMouseOption = MouseOptions.AStar_Pathfind;  
  public OptionButton ActiveOverlayOptions;
  public OptionButton MouseOptionsButton;

  public Camera2D debugCamera;
  public Camera2D playerCamera;

  public bool setAStarStart;
  public bool setAStarDest;
  public AStar.PathState AStarState = AStar.PathState.None;
  public Vector2 AStarStart;
  public Vector2 AStarDest;

  //AStar Debug Info
  public Label currentMouseSelection_AStar_NodeCoords;
  public Label currentMouseSelection_AStar_GivenCost;
  public Label currentMouseSelection_AStar_Heuristic;
  public Label currentMouseSelection_AStar_ParentNodeCoords;
  public Label currentMouseSelection_AStar_NodeState;

  //Character Debug Info
  public Label currentMouseSelection_Character_Name;
  public Label currentMouseSelection_Character_Location;
  public Label currentMouseSelection_Character_MaxHP;
  public Label currentMouseSelection_Character_CurrentHP;
  public Label currentMouseSelection_Character_MoveSpeed;

  //Containers for selection info
  public HSplitContainer AStarInfoUI;
  public HSplitContainer CharacterInformationInfoUI;

  //Data for selection info 
  public AStar.AStarNode currentMouseSelectionNode = new AStar.AStarNode();
  public CombatCharacter currentMouseSelectionCharacter;


  public MarginContainer DebugUI;

  public MarginContainer MouseInfoUI;

  public TestLevelGeneration testLevelGeneration;

  public bool playerMode;

  InputManager inputManager;
  PlayerManager playerManager;

  PhysicsDirectSpaceState2D spaceState;

#endregion

  public override void _Ready()
  {
    inputManager = GetNode<InputManager>("/root/InputManagerSingletonNode");
    playerManager = GetNode<PlayerManager>("/root/PlayerManagerSingletonNode");
  }

  public void PostLevelGenInit()
  {
    testLevelGeneration = GetNode<TestLevelGeneration>("/root/TestLevelGenNode");
    
    DebugUI = testLevelGeneration.GetNode("Camera2D/GUI") as MarginContainer;
    MouseInfoUI = testLevelGeneration.GetNode("Camera2D/MouseInfoUI") as MarginContainer; 
    debugCamera = testLevelGeneration.GetNode("Camera2D") as Camera2D;

    currentMouseSelection_AStar_NodeCoords =        testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/AStarInfo/VBoxContainer2/General7") as Label;
    currentMouseSelection_AStar_GivenCost =         testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/AStarInfo/VBoxContainer2/General8") as Label;
    currentMouseSelection_AStar_Heuristic =         testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/AStarInfo/VBoxContainer2/General9") as Label;
    currentMouseSelection_AStar_ParentNodeCoords =  testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/AStarInfo/VBoxContainer2/General10") as Label;
    currentMouseSelection_AStar_NodeState =         testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/AStarInfo/VBoxContainer2/General11") as Label;


    currentMouseSelection_Character_Name =      testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/CharacterInformation/Data/Name") as Label;
    currentMouseSelection_Character_Location =  testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/CharacterInformation/Data/Location") as Label;
    currentMouseSelection_Character_MaxHP =     testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/CharacterInformation/Data/MaxHP") as Label;
    currentMouseSelection_Character_CurrentHP = testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/CharacterInformation/Data/CurrentHP") as Label;
    currentMouseSelection_Character_MoveSpeed = testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/CharacterInformation/Data/MovementSpeed") as Label;

    AStarInfoUI = testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/AStarInfo") as HSplitContainer;
    CharacterInformationInfoUI = testLevelGeneration.GetNode("Camera2D/MouseInfoUI/VBoxContainer/CharacterInformation") as HSplitContainer;

    ActiveOverlayOptions = testLevelGeneration.GetNode("Camera2D/GUI/VBoxContainer/HSplitContainer5/SelectedOverlay") as OptionButton;

    MouseOptionsButton = testLevelGeneration.GetNode("Camera2D/GUI/VBoxContainer/HSplitContainer6/MouseOptions") as OptionButton;

    //Generate the options menu from the dict keys to make sure they are good with 0 still being no overlays
    foreach (var item in Enum.GetNames(typeof(MouseOptions)))
    {
      MouseOptionsButton.AddItem(item);
    }
    //Generate the options menu from the dict keys to make sure they are good with 0 still being no overlays
    foreach (var item in testLevelGeneration.VisualizationMaps)
    {
      ActiveOverlayOptions.AddItem(item.Key);
    }

    UpdateCharacterInfoUI();
  }

  public void PathfindToPlayerFromSelectedCharacter()
  {
    //Path the enemy to the player
    
    (currentMouseSelectionCharacter as Enemy).pather.InitPather(
      testLevelGeneration.ForegroundMap.LocalToMap(currentMouseSelectionCharacter.GlobalPosition),
      testLevelGeneration.ForegroundMap.LocalToMap(playerManager.topDownPlayer.GlobalPosition),
       new AStar.AStarMap(testLevelGeneration.terrainMap, testLevelGeneration.width, testLevelGeneration.height)); 
    if((currentMouseSelectionCharacter as Enemy).pather.GeneratePath() == AStar.PathState.Found)
    {
      List<Vector2> worldPosPath = new List<Vector2>();
      
      //Translate to world position
      foreach (var item in (currentMouseSelectionCharacter as Enemy).pather.path)
      {
        worldPosPath.Add(testLevelGeneration.ForegroundMap.MapToLocal((Vector2I)item));
      }
      (currentMouseSelectionCharacter as Enemy).movementPath = worldPosPath;
    }
  }

  public void UpdateMouseInfoUI()
  {
    //https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
    //5 points after decimal
    currentMouseSelection_AStar_NodeCoords.Text = currentMouseSelectionNode.pos.ToString("F5");
    currentMouseSelection_AStar_GivenCost.Text = currentMouseSelectionNode.givenCost.ToString("F5");
    currentMouseSelection_AStar_Heuristic.Text = currentMouseSelectionNode.heuristic.ToString("F5");
    if(currentMouseSelectionNode.parent != null)  
      currentMouseSelection_AStar_ParentNodeCoords.Text = currentMouseSelectionNode.parent.pos.ToString("F5");
    currentMouseSelection_AStar_NodeState.Text = currentMouseSelectionNode.state.ToString();
  }

  public void UpdateCharacterInfoUI()
  {
    if(currentMouseSelectionCharacter != null)
    {
      currentMouseSelection_Character_Name.Text       = currentMouseSelectionCharacter.Name.ToString();
      currentMouseSelection_Character_Location.Text   = currentMouseSelectionCharacter.GlobalPosition.ToString("F5");
      currentMouseSelection_Character_MaxHP.Text      = currentMouseSelectionCharacter.maxHealth.ToString();
      currentMouseSelection_Character_CurrentHP.Text  = currentMouseSelectionCharacter.currentHealth.ToString();
      currentMouseSelection_Character_MoveSpeed.Text  = currentMouseSelectionCharacter.movementSpeed.ToString();
    }
  }

  public void SetPlayerMode(bool _playerMode)
  {
    playerMode = _playerMode;
    if(playerMode)
    {
      (debugCamera as CameraMovement).movementEnabled = false;
      DebugUI.Visible = false;
      MouseInfoUI.Visible = false;
      if(playerCamera != null)
        playerCamera.MakeCurrent();
    }
    else
    {
      (debugCamera as CameraMovement).movementEnabled = true;
      debugCamera.MakeCurrent();
      DebugUI.Visible = true;
      MouseInfoUI.Visible = true;
    }
  }

  //Select with mouse position
  public override void _UnhandledInput(InputEvent inputEvent)
  {

    if(currentMouseOption == MouseOptions.DirectedGraph_ToParent)
    {
      if (inputEvent is InputEventMouseButton mouseClick && (mouseClick.Pressed && mouseClick.ButtonIndex == MouseButton.Left))
      {
        //World space -> Map space where coordinates are
        Vector2 clickedPos = testLevelGeneration.ForegroundMap.LocalToMap(testLevelGeneration.ForegroundMap.GetLocalMousePosition());
        ChokePointFinder.CPFNode foundNode;
        if(testLevelGeneration.FindNodeAtPos(clickedPos, testLevelGeneration.cpfRootNode, out foundNode))
        {
          //Reset the visuals
          testLevelGeneration.CPF.UpdateDirectedMapVis(testLevelGeneration.cpfRootNode);
          testLevelGeneration.DrawParentTree(foundNode);
          //Console.WriteLine("Found Node! at " + clickedPos.ToString() + " Where node exists at " + foundNode.pos.ToString());
        }
      }
    }
    else if (currentMouseOption == MouseOptions.AStar_Pathfind)
    {
      if(inputEvent is InputEventMouseButton mouseClick && mouseClick.Pressed)
      {
        if (mouseClick.ButtonIndex == MouseButton.Right)
        {
          AStarState = AStar.PathState.None;
          Vector2 newAStarDest = testLevelGeneration.ForegroundMap.LocalToMap(testLevelGeneration.ForegroundMap.GetLocalMousePosition());

          if(newAStarDest.X >= testLevelGeneration.width || newAStarDest.X <= 0)
            return;
          if(newAStarDest.Y >= testLevelGeneration.height || newAStarDest.Y <= 0)
            return;
            
          //Don't allow walls to be set as dest or start
          if(testLevelGeneration.terrainMap[(int)newAStarDest.X,(int)newAStarDest.Y] == 0)
          {
            //Remove old one
            testLevelGeneration.VisualizationMaps["AStar Overlay"].SetCell(0,new Vector2I((int)AStarDest.X,(int)AStarDest.Y), -1);

            AStarDest = newAStarDest;
            //Set dest
            setAStarDest = true;
            testLevelGeneration.VisualizationMaps["AStar Overlay"].SetCell(0,new Vector2I((int)AStarDest.X,(int)AStarDest.Y), 11);
          }
          else
          {
            AStarDest = Vector2.Zero;
            setAStarDest = false;
          }
        }

        if (mouseClick.ButtonIndex == MouseButton.Left)
        {
          AStarState = AStar.PathState.None;
          Vector2 newAStarStart = testLevelGeneration.ForegroundMap.LocalToMap(testLevelGeneration.ForegroundMap.GetLocalMousePosition());

          if(newAStarStart.X >= testLevelGeneration.width || newAStarStart.X <= 0)
            return;
          if(newAStarStart.Y >= testLevelGeneration.height || newAStarStart.Y <= 0)
            return;
            
          //Dont allow walls to be set as dest or start
          if(testLevelGeneration.terrainMap[(int)newAStarStart.X,(int)newAStarStart.Y] == 0)
          {
            testLevelGeneration.VisualizationMaps["AStar Overlay"].SetCell(0, (Vector2I)AStarStart);

            AStarStart = newAStarStart;
            //Set start
            setAStarStart = true;
            testLevelGeneration.VisualizationMaps["AStar Overlay"].SetCell(0, (Vector2I)AStarStart, 4);
          }
          else
          {
            AStarStart = Vector2.Zero;
            setAStarStart = false;
          }
        }
      }
      if(setAStarStart && setAStarDest && AStarState == AStar.PathState.None)
      {
        testLevelGeneration.AStarMap = new AStar.AStarMap(testLevelGeneration.terrainMap, testLevelGeneration.width, testLevelGeneration.height);
        //World space -> Map space where coordinates are
        testLevelGeneration.AStarPather.InitPather(AStarStart, AStarDest, testLevelGeneration.AStarMap);
        //if not realtime then just generate the path
        
        if(!testLevelGeneration.realtimeAStar)
        {
          AStarState = testLevelGeneration.AStarPather.GeneratePath(testLevelGeneration.realtimeAStar);
          testLevelGeneration.AStarPather.UpdateMapVisual();
        }
        else
        {
          AStarState = AStar.PathState.Searching;
        }
      }
    }
    else if (currentMouseOption == MouseOptions.AStar_NodeInfo)
    {
      if(inputEvent is InputEventMouseButton mouseClick && mouseClick.Pressed && mouseClick.ButtonIndex == MouseButton.Left)
      {
        if(testLevelGeneration.AStarPather.map != null)
        {
          currentMouseSelectionNode = testLevelGeneration.AStarPather.map.GetNodeAt(testLevelGeneration.ForegroundMap.LocalToMap(testLevelGeneration.ForegroundMap.GetLocalMousePosition()));
        }

        UpdateMouseInfoUI();
      }
    }
    else if(currentMouseOption == MouseOptions.Character_Select)
    {
      if(inputEvent is InputEventMouseButton mouseClick && mouseClick.Pressed && mouseClick.ButtonIndex == MouseButton.Left)
      {
        //Returns some JSON type stuff????
        var result = spaceState.IntersectPoint(new PhysicsPointQueryParameters2D{Position = GetGlobalMousePosition()});

        
        foreach (Dictionary item in result)
        {
          Console.WriteLine("Point intersected with: " + item + " Is of type " + item["collider"].GetType());
        }

        
        //currentMouseSelectionCharacter = 
        //UpdateCharacterInfoUI();
      }
    }

    if(inputManager.IsKeyPressed(Key.M))
    {
      SetPlayerMode(!playerMode);
    }
  }
  
  public override void _PhysicsProcess(double delta)
  {
    spaceState = GetWorld2D().DirectSpaceState;

  }
}
