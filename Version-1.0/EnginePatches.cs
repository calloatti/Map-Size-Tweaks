using System;
using System.Reflection;
using HarmonyLib;
using Timberborn.GameSaveRepositorySystemUI;
using Timberborn.MapStateSystem;
using Timberborn.TerrainSystem;
using UnityEngine; // Added for Debug.Log

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(ValidatingGameLoader), "LoadGame")]
  public static class Patch_ValidatingGameLoader_LoadGame
  {
    public static void Prefix()
    {
      // Log the state right when validation engine accepts the load request
      Debug.Log($"[MapSizeTweaks] ValidatingGameLoader.LoadGame Prefix: Terrain={MapHeightOverrideStates.TerrainHeight}, Above={MapHeightOverrideStates.AboveHeight}, UseOverride={MapHeightOverrideStates.UseOverride}");
      MapHeightOverrideStates.UseOverride = true;
    }
  }

  [HarmonyPatch(typeof(MapSize), "Load")]
  public static class Patch_MapSize_Load
  {
    public static void Postfix(MapSize __instance, ref MapSizeSpec ____mapSizeSpec)
    {
      Debug.Log($"[MapSizeTweaks] MapSize.Load Postfix entry: UseOverride={MapHeightOverrideStates.UseOverride}, Requested Terrain={MapHeightOverrideStates.TerrainHeight}");

      if (MapHeightOverrideStates.UseOverride)
      {
        int oldTerrain = ____mapSizeSpec.MaxGameTerrainHeight;
        int oldAbove = ____mapSizeSpec.MaxHeightAboveTerrain;

        ____mapSizeSpec = new MapSizeSpec
        {
          DefaultMapSize = ____mapSizeSpec.DefaultMapSize,
          MinMapSize = ____mapSizeSpec.MinMapSize,
          MaxMapSize = ____mapSizeSpec.MaxMapSize,
          MaxMapEditorTerrainHeight = ____mapSizeSpec.MaxMapEditorTerrainHeight,
          MaxGameTerrainHeight = MapHeightOverrideStates.TerrainHeight,
          MaxHeightAboveTerrain = MapHeightOverrideStates.AboveHeight
        };

        MethodInfo initMethod = typeof(MapSize).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
        initMethod?.Invoke(__instance, new object[] { __instance.TerrainSize2D });

        Debug.Log($"[MapSizeTweaks] MapSize.Load SPEC OVERRIDDEN: Terrain {oldTerrain} -> {____mapSizeSpec.MaxGameTerrainHeight}, Above {oldAbove} -> {____mapSizeSpec.MaxHeightAboveTerrain}");
      }
    }
  }

  [HarmonyPatch(typeof(TerrainMap), "GetTerrainData")]
  public static class Patch_TerrainMap_GetTerrainData
  {
    public static void Postfix(ref bool[] __result, MapSize ____mapSize)
    {
      int expectedLength = ____mapSize.TerrainSize.x * ____mapSize.TerrainSize.y * ____mapSize.TerrainSize.z;

      if (__result.Length < expectedLength)
      {
        Debug.Log($"[MapSizeTweaks] TerrainMap.GetTerrainData: Padding array size from {__result.Length} to {expectedLength}");
        bool[] paddedArray = new bool[expectedLength];
        Array.Copy(__result, paddedArray, __result.Length);
        __result = paddedArray;
      }
    }
  }
}