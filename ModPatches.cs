using System.Reflection;
using HarmonyLib;
using Timberborn.CoreUI;
using Timberborn.MapRepositorySystemUI;
using Timberborn.MapStateSystem;

namespace Calloatti.MapSizeTweaks
{
  // 1. Override the individual dimension parsing so the text fields accept sizes up to 65536
  [HarmonyPatch(typeof(NewMapBox), "TryParseSize")]
  public static class Patch_NewMapBox_TryParseSize
  {
    public static bool Prefix(NewMapBox __instance, string text, out int size, ref bool __result, MapSizeSpec ____mapSizeSpec)
    {
      // Try to parse the input to an integer
      if (int.TryParse(text, out size) && size >= ____mapSizeSpec.MinMapSize)
      {
        // We allow any dimension up to 65536, bypassing the vanilla MaxMapSize
        __result = size <= 65536;
        return false; // Skip the original method
      }

      size = 0;
      __result = false;
      return false; // Skip the original method
    }
  }

  // 2. Validate the total surface area when the "Start" button is clicked
  [HarmonyPatch(typeof(NewMapBox), "StartNewMap")]
  public static class Patch_NewMapBox_StartNewMap
  {
    public static bool Prefix(NewMapBox __instance,
                              // These are the private fields from NewMapBox we need access to
                              UnityEngine.UIElements.TextField ____sizeXField,
                              UnityEngine.UIElements.TextField ____sizeYField,
                              DialogBoxShower ____dialogBoxShower)
    {
      // We use reflection to call the private TryParseSize method to get the X and Y values
      MethodInfo tryParseMethod = typeof(NewMapBox).GetMethod("TryParseSize", BindingFlags.NonPublic | BindingFlags.Instance);

      object[] xParams = new object[] { ____sizeXField.text, 0 };
      bool xValid = (bool)tryParseMethod.Invoke(__instance, xParams);
      int sizeX = (int)xParams[1];

      object[] yParams = new object[] { ____sizeYField.text, 0 };
      bool yValid = (bool)tryParseMethod.Invoke(__instance, yParams);
      int sizeY = (int)yParams[1];

      // If both inputs are valid numbers
      if (xValid && yValid)
      {
        // Enforce the 65,536 total surface area rule
        if (sizeX * sizeY <= 65536)
        {
          return true; // The area is valid, allow the original method to run and create the map
        }
        else
        {
          // The area is too big. Show an error dialog and block creation.
          ____dialogBoxShower.Create().SetMessage("The total map area (Width x Height) cannot exceed 65,536 tiles.").Show();
          return false; // Skip the original method to prevent map creation
        }
      }

      // Let the original method handle invalid/non-numeric inputs
      return true;
    }
  }
}