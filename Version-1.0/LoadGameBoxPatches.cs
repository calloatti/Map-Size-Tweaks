using HarmonyLib;
using Timberborn.CoreUI;
using Timberborn.GameSaveRepositorySystem;
using Timberborn.GameSaveRepositorySystemUI;
using UnityEngine.UIElements;

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(LoadGameBox), "LoadGame")]
  public static class Patch_LoadGameBox_LoadGame
  {
    public static bool Prefix(
        LoadGameBox __instance,
        SaveList ____saveList,
        GameSaveRepository ____gameSaveRepository,
        ValidatingGameLoader ____validatingGameLoader,
        DialogBoxShower ____dialogBoxShower)
    {
      if (!____saveList.TryGetSelectedSave(out var selectedSave) || !____gameSaveRepository.SaveExists(selectedSave.SaveReference))
      {
        return true;
      }

      // Reset the loading flag while the user views the dialog popup box
      MapHeightOverrideStates.IsLoadingGame = false;

      VisualElement container = new VisualElement();
      container.style.marginTop = 15;
      container.style.alignItems = Align.Center;

      Label terrainLabel = new Label("Max Terrain Height: (Default: 22)");
      terrainLabel.AddToClassList("text--default");
      terrainLabel.style.marginBottom = 5;

      NineSliceTextField terrainField = new NineSliceTextField();
      terrainField.AddToClassList("text-field");
      terrainField.AddToClassList("box__input");
      terrainField.value = MapHeightOverrideStates.TerrainHeight.ToString();
      terrainField.style.width = 250;
      terrainField.style.marginBottom = 15;

      Label aboveLabel = new Label("Max Building Height: (Default: 10)");
      aboveLabel.AddToClassList("text--default");
      aboveLabel.style.marginBottom = 5;

      NineSliceTextField aboveField = new NineSliceTextField();
      aboveField.AddToClassList("text-field");
      aboveField.AddToClassList("box__input");
      aboveField.value = MapHeightOverrideStates.AboveHeight.ToString();
      aboveField.style.width = 250;

      container.Add(terrainLabel);
      container.Add(terrainField);
      container.Add(aboveLabel);
      container.Add(aboveField);

      ____dialogBoxShower.Create()
        .SetMessage("Current map heights. You can change the values here.")
        .AddContent(container)
        .SetCancelButton(() => { }, "Cancel")
        .SetConfirmButton(() =>
        {
          if (int.TryParse(terrainField.value, out int tHeight)) MapHeightOverrideStates.TerrainHeight = tHeight;
          if (int.TryParse(aboveField.value, out int aHeight)) MapHeightOverrideStates.AboveHeight = aHeight;

          MapHeightOverrideStates.UseOverride = true;
          MapHeightOverrideStates.IsLoadingGame = true; // Protect the variables from metadata resets

          ____validatingGameLoader.LoadGame(selectedSave.SaveReference);

        }, "Load Game")
        .Show();

      return false;
    }
  }
}