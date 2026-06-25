using HarmonyLib;
using Timberborn.Persistence;
using Timberborn.SaveMetadataSystem;

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(SaveMetadataSerializer), "SaveMetadata")]
  public static class Patch_SaveMetadataSerializer_SaveMetadata
  {
    public static void Postfix(IObjectSaver objectSaver)
    {
      objectSaver.Set(new PropertyKey<int>("MaxGameTerrainHeight"), MapHeightOverrideStates.TerrainHeight);
      objectSaver.Set(new PropertyKey<int>("MaxHeightAboveTerrain"), MapHeightOverrideStates.AboveHeight);
    }
  }

  [HarmonyPatch(typeof(SaveMetadataSerializer), "LoadMetadata")]
  public static class Patch_SaveMetadataSerializer_LoadMetadata
  {
    public static void Postfix(IObjectLoader objectLoader)
    {
      // CRITICAL FIX: If we are actively loading a map, do not let file metadata clear the user input values!
      if (MapHeightOverrideStates.IsLoadingGame) return;

      PropertyKey<int> terrainKey = new PropertyKey<int>("MaxGameTerrainHeight");
      PropertyKey<int> aboveKey = new PropertyKey<int>("MaxHeightAboveTerrain");

      if (objectLoader.Has(terrainKey) && objectLoader.Has(aboveKey))
      {
        MapHeightOverrideStates.SaveHasCustomHeights = true;
        MapHeightOverrideStates.UseOverride = true;
        MapHeightOverrideStates.TerrainHeight = objectLoader.Get(terrainKey);
        MapHeightOverrideStates.AboveHeight = objectLoader.Get(aboveKey);
      }
      else
      {
        MapHeightOverrideStates.ResetToVanilla();
      }
    }
  }
}