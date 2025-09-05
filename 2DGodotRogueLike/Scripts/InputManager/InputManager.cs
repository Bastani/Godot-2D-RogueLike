using System;
using System.Collections.Generic;
using Godot;

//Get the input manager with 
//inputManager =  GetNode<InputManager>("/root/InputManagerSingletonNode");
public partial class InputManager : Node
{
  public enum KeyState
  {
    Pressed,  //pressed this frame
    Released, //pressed last frame
    Held,     //pressed this frame and last frame
    None      //no state
  }

  Dictionary<Key, KeyState> keys = new Dictionary<Key, KeyState>();

  public override void _Ready()
  {
    //For each keys initialize to none
    foreach (Key item in Enum.GetValues(typeof(Key)))
    {
      keys[item] = KeyState.None;
    }
  }

  public KeyState GetKeyState(Key key)
  {
    return keys[key];
  }



  //pressed this frame
  public bool IsKeyPressed(Key key)
  {
    return keys[key] == KeyState.Pressed;
  }

  //pressed this frame
  public bool IsKeyReleased(Key key)
  {
    return keys[key] == KeyState.Released;
  }
  
  //Down = pressed this frame or held down
  public bool IsKeyDown(Key key)
  {
    return keys[key] == KeyState.Pressed || keys[key] == KeyState.Held;
  }
  
  //If the key is held down
  public bool IsKeyHeld(Key key)
  {
    return keys[key] == KeyState.Held;
  }

  //Handle unhandled input as per Godot suggestion for keyboard input to the game and not UI (handled input)
  public override void _UnhandledInput(InputEvent @event)
  {
    if (@event is InputEventKey eventKey)
    {
      if(eventKey.Pressed)
      {
        if(keys[eventKey.PhysicalKeycode] == KeyState.None)
          keys[eventKey.PhysicalKeycode] = KeyState.Pressed;
      }
      else if(eventKey.Echo)
      {
        if(keys[eventKey.PhysicalKeycode] == KeyState.Pressed)
          keys[eventKey.PhysicalKeycode] = KeyState.Held;
      }
      else if(!eventKey.Pressed && !eventKey.Echo)  //key not pressed this frame
      {
        if(keys[eventKey.PhysicalKeycode] == KeyState.Pressed || keys[eventKey.PhysicalKeycode] == KeyState.Held)
          keys[eventKey.PhysicalKeycode] = KeyState.Released;

        //Reset the key
        if(keys[eventKey.PhysicalKeycode] == KeyState.Released)
          keys[eventKey.PhysicalKeycode] = KeyState.None;
      }
    }
  }

  public override void _PhysicsProcess(double delta)
  {

  }
}
