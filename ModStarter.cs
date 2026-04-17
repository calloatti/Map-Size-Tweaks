using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace Calloatti.MapSizeTweaks
{
  public class ModStarter : IModStarter
  {

    public void StartMod(IModEnvironment modEnvironment)
    {

      new Harmony("Calloatti.MapSizeTweaks").PatchAll();

      Debug.Log("[MapSizeTweaks] Harmony Patches Applied.");
    }
  }
}