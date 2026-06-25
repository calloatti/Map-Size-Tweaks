namespace Calloatti.MapSizeTweaks
{
  public static class MapHeightOverrideStates
  {
    public static bool SaveHasCustomHeights = false;
    public static bool UseOverride = false;
    public static bool IsLoadingGame = false; // Prevents the validator from clearing user entries
    public static int TerrainHeight = 22;
    public static int AboveHeight = 10;

    public static void ResetToVanilla()
    {
      SaveHasCustomHeights = false;
      UseOverride = false;
      TerrainHeight = 22;
      AboveHeight = 10;
    }
  }
}