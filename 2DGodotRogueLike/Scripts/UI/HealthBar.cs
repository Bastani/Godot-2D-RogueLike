using Godot;

public partial class HealthBar : Control
{
  TextureProgressBar healthBarUIElement;

  public override void _Ready()
  {
	  healthBarUIElement = GetNode("HealthBarProgress") as TextureProgressBar;
  }
  
  //Sets the current health of the UI element
  public void SetHealth(float health)
  {
	  healthBarUIElement.Value = health;
  }

  //Set max and current health in the UI element
  public void SetMaxHealth(int maxHealth)
  {
	  healthBarUIElement.Value = maxHealth;
	  healthBarUIElement.MaxValue = maxHealth;
  }
}
