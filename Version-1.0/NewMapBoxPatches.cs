using System.Reflection;
using HarmonyLib;
using Timberborn.CoreUI;
using Timberborn.MapRepositorySystemUI;
using Timberborn.MapStateSystem;

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(NewMapBox), "TryParseSize")]
  public static class Patch_NewMapBox_TryParseSize
  {
    public static bool Prefix(NewMapBox __instance, string text, out int size, ref bool __result, MapSizeSpec ____mapSizeSpec)
    {
      if (int.TryParse(text, out size) && size >= ____mapSizeSpec.MinMapSize)
      {
        __result = size <= 65536;
        return false;
      }

      size = 0;
      __result = false;
      return false;
    }
  }

  [HarmonyPatch(typeof(NewMapBox), "StartNewMap")]
  public static class Patch_NewMapBox_StartNewMap
  {
    public static bool Prefix(NewMapBox __instance,
                              UnityEngine.UIElements.TextField ____sizeXField,
                              UnityEngine.UIElements.TextField ____sizeYField,
                              DialogBoxShower ____dialogBoxShower)
    {
      MethodInfo tryParseMethod = typeof(NewMapBox).GetMethod("TryParseSize", BindingFlags.NonPublic | BindingFlags.Instance);

      object[] xParams = new object[] { ____sizeXField.text, 0 };
      bool xValid = (bool)tryParseMethod.Invoke(__instance, xParams);
      int sizeX = (int)xParams[1];

      object[] yParams = new object[] { ____sizeYField.text, 0 };
      bool yValid = (bool)tryParseMethod.Invoke(__instance, yParams);
      int sizeY = (int)yParams[1];

      if (xValid && yValid)
      {
        if (sizeX * sizeY <= 65536)
        {
          return true;
        }
        else
        {
          ____dialogBoxShower.Create().SetMessage("The total map area (Width x Height) cannot exceed 65,536 tiles.").Show();
          return false;
        }
      }

      return true;
    }
  }
}