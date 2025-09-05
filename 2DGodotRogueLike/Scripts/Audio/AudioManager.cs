using System.Collections.Generic;
using Godot;

public partial class AudioManager : Node
{
  //https://inglo-games.github.io/2020/04/22/audio-busses.html
  //Godot has an AudioServer singleton

  enum AudioBus
  {
    SFX_Player,
    SFX_Enemy,
    Music_Action,
    Music_BG,
    SFX_BG,
    OtherBus1,
    OtherBus2
  }

  Dictionary<AudioBus, int> AudioBusToID = new Dictionary<AudioBus, int>();
  int busCount;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {

  }

  public void SwitchToCombatMusic()
  {
    
  }

  public void SwitchToBackgroundMusic()
  {

  }

  //To test passing audio to a bus, create an AudioStreamPlayer node, load an AudioStream and select a target bus for playback:
  
  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    
  }
}
