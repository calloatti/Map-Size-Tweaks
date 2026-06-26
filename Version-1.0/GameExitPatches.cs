using System;
using HarmonyLib;
using Timberborn.GameExitSystem;

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(GoodbyeBoxFactory), "GetController")]
  public static class Patch_GoodbyeBoxFactory_GetController
  {
    public static void Prefix(ref Action action)
    {
      // Capture the original exit action (Quit to Desktop or SaveAndOpenMainMenu)
      Action originalAction = action;

      // Replace it with a new action that executes the vanilla exit/save FIRST, 
      // then resets our mod so it doesn't bleed into the next session.
      action = () =>
      {
        originalAction?.Invoke();
        MapHeightOverrideStates.ResetToVanilla();
      };
    }
  }
}