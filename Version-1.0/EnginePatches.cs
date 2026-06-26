using HarmonyLib;
using System;
using System.Reflection;
using Timberborn.MapStateSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(MapSize), "Save")]
  public static class Patch_MapSize_Save
  {
    public static void Postfix(ISingletonSaver singletonSaver)
    {
      // Only write to the save file if the map actually uses custom heights
      if (MapHeightOverrideStates.UseOverride)
      {
        IObjectSaver saver = singletonSaver.GetSingleton(new SingletonKey("MapSize"));
        saver.Set(new PropertyKey<int>("CustomTerrainHeight"), MapHeightOverrideStates.TerrainHeight);
        saver.Set(new PropertyKey<int>("CustomAboveHeight"), MapHeightOverrideStates.AboveHeight);

        Debug.Log($"[MapSizeTweaks] Saved custom heights to map data: Terrain={MapHeightOverrideStates.TerrainHeight}, Above={MapHeightOverrideStates.AboveHeight}");
      }
    }
  }

  [HarmonyPatch(typeof(MapSize), "Load")]
  public static class Patch_MapSize_Load
  {
    public static void Postfix(MapSize __instance, ref MapSizeSpec ____mapSizeSpec, ISingletonLoader ____singletonLoader)
    {
      // 1. Check the true save file data (bypassing the UI metadata loop entirely)
      IObjectLoader loader = ____singletonLoader.GetSingleton(new SingletonKey("MapSize"));
      PropertyKey<int> terrainKey = new PropertyKey<int>("CustomTerrainHeight");
      PropertyKey<int> aboveKey = new PropertyKey<int>("CustomAboveHeight");

      if (loader != null && loader.Has(terrainKey) && loader.Has(aboveKey))
      {
        // We are loading a custom save file
        MapHeightOverrideStates.TerrainHeight = loader.Get(terrainKey);
        MapHeightOverrideStates.AboveHeight = loader.Get(aboveKey);
        MapHeightOverrideStates.UseOverride = true;
        Debug.Log("[MapSizeTweaks] Custom heights loaded from Save Data.");
      }
      else if (!MapHeightOverrideStates.IsLoadingGame)
      {
        // We are loading a vanilla save, AND the user didn't use the UI button
        MapHeightOverrideStates.ResetToVanilla();
        Debug.Log("[MapSizeTweaks] Vanilla map detected. Resetting heights.");
      }
      else
      {
        // We are generating a New Game using the UI Button override
        Debug.Log("[MapSizeTweaks] UI Override applied to New Map generation.");
      }

      // 2. Apply the chosen heights to the engine spec
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

        // Force the map to recalculate its 3D boundaries with our new heights
        MethodInfo initMethod = typeof(MapSize).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
        initMethod?.Invoke(__instance, new object[] { __instance.TerrainSize2D });

        Debug.Log($"[MapSizeTweaks] MapSize initialized: Terrain {oldTerrain} -> {____mapSizeSpec.MaxGameTerrainHeight}, Above {oldAbove} -> {____mapSizeSpec.MaxHeightAboveTerrain}");
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