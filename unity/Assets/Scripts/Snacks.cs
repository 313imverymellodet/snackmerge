using UnityEngine;

public class SnackDef
{
    public string id, name, model;
    public float r;            // physics radius (world units)
    public Vector3 euler;      // how the model faces the camera
    public Color color;        // juice colour for merge bursts
}

// The merge ladder: two of a kind become the next one up. Two watermelons pop for a big bonus.
public static class Snacks
{
    public static readonly SnackDef[] All =
    {
        new SnackDef { id = "cherries",   name = "CHERRY",     model = "Food/cherries",        r = 0.27f, euler = new Vector3(0, 30, 0),   color = Kit.Hex("#E8344A") },
        new SnackDef { id = "strawberry", name = "STRAWBERRY", model = "Food/strawberry",      r = 0.36f, euler = new Vector3(-15, 20, 0), color = Kit.Hex("#FF4D5E") },
        new SnackDef { id = "lemon",      name = "LEMON",      model = "Food/lemon",           r = 0.46f, euler = new Vector3(0, 70, 0),   color = Kit.Hex("#FFE14D") },
        new SnackDef { id = "apple",      name = "APPLE",      model = "Food/apple",           r = 0.57f, euler = new Vector3(-10, 30, 0), color = Kit.Hex("#7ED957") },
        new SnackDef { id = "orange",     name = "ORANGE",     model = "Food/orange",          r = 0.69f, euler = new Vector3(0, 20, 0),   color = Kit.Hex("#FF9A2E") },
        new SnackDef { id = "donut",      name = "DONUT",      model = "Food/donut-sprinkles", r = 0.83f, euler = new Vector3(-80, 0, 0),  color = Kit.Hex("#FF8FC8") },
        new SnackDef { id = "coconut",    name = "COCONUT",    model = "Food/coconut",         r = 0.98f, euler = new Vector3(-20, 30, 0), color = Kit.Hex("#B58A64") },
        new SnackDef { id = "pizza",      name = "PIZZA",      model = "Food/pizza",           r = 1.15f, euler = new Vector3(-80, 0, 0),  color = Kit.Hex("#FFC14D") },
        new SnackDef { id = "cake",       name = "CAKE",       model = "Food/cake-birthday",   r = 1.34f, euler = new Vector3(-15, 25, 0), color = Kit.Hex("#FFB3D9") },
        new SnackDef { id = "pumpkin",    name = "PUMPKIN",    model = "Food/pumpkin",         r = 1.55f, euler = new Vector3(-8, 20, 0),  color = Kit.Hex("#FF7A1A") },
        new SnackDef { id = "watermelon", name = "WATERMELON", model = "Food/watermelon",      r = 1.80f, euler = new Vector3(-10, 40, 0), color = Kit.Hex("#4FD36A") },
    };
    public const int DropTiers = 5;          // only the five smallest are ever dropped

    // Special drops (tier codes -1 and -2 in the drop queue):
    //   HOT PEPPER blows up shortly after it lands, popping small snacks and shoving the rest.
    //   SPRINKLE CUPCAKE merges with whatever it touches first, bumping that snack up a tier.
    public const int PepperCode = -1, SprinkleCode = -2;
    public static readonly SnackDef Pepper = new SnackDef { id = "pepper", name = "HOT PEPPER", model = "Food/pepper", r = 0.45f, euler = new Vector3(-10, 30, 15), color = Kit.Hex("#E8343A") };
    public static readonly SnackDef Sprinkle = new SnackDef { id = "cupcake", name = "SPRINKLE CUPCAKE", model = "Food/cupcake", r = 0.46f, euler = new Vector3(-12, 25, 0), color = Kit.Hex("#FF8FC8") };
    public static SnackDef Def(int code) => code == PepperCode ? Pepper : code == SprinkleCode ? Sprinkle : All[code];
    public static int Points(int tier) => (tier + 1) * (tier + 2);   // made by merging into `tier`
}
