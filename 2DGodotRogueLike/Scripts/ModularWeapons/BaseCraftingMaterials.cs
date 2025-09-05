using System.Collections.Generic;
using Godot;
using Materials;
using Parts;

public partial class BaseCraftingMaterials : Resource
{
  [Export]
  public string name { get; set; } = "BaseCraftingMaterial";

  [Export]
  public int ingotCost { get; set; } = 5;

  [Export]
  public MaterialType materialType { get; set; } = MaterialType.Undefined;

  [Export]
  public Color tint { get; set; } = new Color(0,0,0,0);

  // This represents the texture used to render the material in UI; TextureRect is a Control node, not a resource to store here.
  public Texture2D sprite;

  // Godot 4 does not support exporting System.Collections.Generic.Dictionary directly for editing; keep runtime dictionary non-exported.
  public Dictionary<PartType,List<MaterialStatData>> materialProperties;

  public BaseCraftingMaterials()
  {
    materialProperties = new Dictionary<PartType,List<MaterialStatData>>();
  }

  public BaseCraftingMaterials(string _name, int _ingotCost, MaterialType _materialType, Color _tint)
  {
    materialProperties = new Dictionary<PartType,List<MaterialStatData>>();

    name = _name;
    ingotCost = _ingotCost;
    materialType = materialType;
    tint = _tint;
  }
}

namespace Materials
{
  //Each material is linked to a tint
  public enum Material
  {
    Iron,       //Ore Chunks 0
    Copper,     //Ore Chunks 1
    Silver,     //Ore Chunks 2
    Gold,       //Ore Chunks 3
    Mithril,    //Ore Chunks 4
    Cobalt,     //Ore Chunks 6
    Bronze,     //Currently do everything before bronze
    Coal,       //Ore Chunks 5
    Tin,
    Steel,
    Platinum,
    Adamantite,
    Darksteel,
    Titanium,
    Undefined,
  }

  public enum MaterialType
  {
    Undefined,
    Metal,
    Wood,
    String,
  }

  public enum MaterialStatType
  {
    Undefined,
    Damage,
    CritChange,
    CritDamage,
    AttackSpeed,
    Health,
  }

  //Static class of colors tints
  public static class MaterialTints
  {
    public static Dictionary<Material, Color> tints = new Dictionary<Material, Color>();

    static MaterialTints()
    {

      tints[Material.Iron] =        new Color("e8e8e8");
      tints[Material.Silver] =      new Color("e6f2ff");  //TODO update silver so its not just platinum
      tints[Material.Coal] =        new Color("404040");
      tints[Material.Copper] =      new Color("e8a25d");
      tints[Material.Tin] =         new Color("faf4dc");
      tints[Material.Bronze] =      new Color("e8c774");
      tints[Material.Steel] =       new Color("a2e8b7");
      tints[Material.Gold] =        new Color("e8dc5d");
      tints[Material.Platinum] =    new Color("e6f2ff");
      tints[Material.Adamantite] =  new Color("e86868");
      tints[Material.Mithril] =     new Color("a2e8b7");
      tints[Material.Cobalt] =      new Color("a2aee8");
      tints[Material.Darksteel] =   new Color("696969");
      tints[Material.Titanium] =    new Color("ffffff");
      tints[Material.Undefined] =   new Color(1,1,1,0.5f); //transparent 50%, also ffffff7e doesnt work??

    }
  }


  public class Stats
  {
    public float damageMult {get;private set;} = 1;
    public float windMult {get;private set;}  = 1;
    public int tier = 1;
    public Stats(int _tier, float _damage, float _wind)
    {
      tier = _tier;
      damageMult = _damage;
      windMult = _wind;
    }

  }
  public static class MaterialStats
  {

    public static Dictionary<Material, Stats> stats = new Dictionary<Material, Stats>();

    static MaterialStats()
    {
                                              //tier, damage mult, windup/down mult
      stats[Material.Copper] =      new Stats(1, 1,     1);
      stats[Material.Silver] =      new Stats(2, 1.5f,  1);
      stats[Material.Iron] =        new Stats(2, 2,     1.333f);
      stats[Material.Gold] =        new Stats(3, 3,     1);
      stats[Material.Mithril] =     new Stats(3, 4f,    1.333f);
      stats[Material.Cobalt] =      new Stats(3, 2,     0.666f);

      //TODO other ores later
      stats[Material.Tin] =         new Stats(1, 1, 1);
      stats[Material.Bronze] =      new Stats(1, 1, 1);
      stats[Material.Steel] =       new Stats(1, 1, 1);
      stats[Material.Platinum] =    new Stats(1, 1, 1);
      stats[Material.Adamantite] =  new Stats(1, 1, 1);
      stats[Material.Darksteel] =   new Stats(1, 1, 1);
      stats[Material.Titanium] =    new Stats(1, 1, 1);
      stats[Material.Coal] =        new Stats(1, 0, 0);
      stats[Material.Undefined] =   new Stats(1, 1, 1);

    }
  }

  //Material Stat Data Piece
  public partial class MaterialStatData : Resource
  {
    public MaterialStatData(MaterialStatType _statType, int _statData){statType = _statType;statData = _statData;}

    [Export]
    public MaterialStatType statType = MaterialStatType.Undefined;

    [Export]
    public int statData;
  }
}

//TODO Move this to a PiecesEnum.cs and generate that code from a list of .png files inside of the My_Art/Parts folder
namespace Parts
{
  using System;

  public static class PartTypeConversion
  {
    public static PartType FromString(string input)
    {

      foreach (PartType type in Enum.GetValues(typeof(PartType)))
      {
        if(input == type.ToString())
          return type;
      }
      throw(new Exception("Enum " + input + " Does not exist in Parts.PartType."));

      //redundent return statement
      //return PartType.Undefined;
    }

    public static int CompareParts(PartBlueprint x, PartBlueprint y)
    {
      //Sort parts according to the
      return x.partType - y.partType;
    }
  }



  //Going to use enums to simplify the number of classes + adding more types of pieces can be data read into the piece data type instead of some RTTI/CTTI
  public enum PartType
  {
    Undefined,
    Blade,     //Blades, mace heads, tool heads, etc
    Guard,
    Handle,
    Pommel,
    
  //  HammerHead,   //Art Todo
  //  BattleaxeHead,  //Art Todo
  //  LargeGuard,   //Art Todo
  //  MediumHandle,   //Art Todo
  //  Medium_Mace,
  }
  //Pickaxe is {LargeHandle, ToolBinding, PickaxeHead}
  //Axe is {LargeHandle, ToolBinding, AxeHead}
  //Dagger is {Pommel, Small Handle, Small Guard, Small Blade}
  //Sword is a {Pommel, Small Handle, Medium Guard, Medium Blade}

}


