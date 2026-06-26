using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using Timberborn.CoreUI;
using Timberborn.MainMenuPanels;
using UnityEngine.UIElements;

namespace Calloatti.MapSizeTweaks
{
  [HarmonyPatch(typeof(MainMenuPanel), "GetPanel")]
  public static class Patch_MainMenuPanel_GetPanel
  {
    public static void Postfix(MainMenuPanel __instance, VisualElement __result)
    {
      if (__result == null) return;

      // Prevent duplicate buttons
      if (__result.Q("MapHeightsButton") != null) return;

      // Find the LoadMapButton (Edit Map)
      VisualElement loadMapButton = __result.Q("LoadMapButton");
      if (loadMapButton == null) return;

      // Create the clone
      Button heightsButton = (Button)Activator.CreateInstance(loadMapButton.GetType());
      heightsButton.name = "MapHeightsButton";
      heightsButton.text = "Map Heights";

      // Copy Styles
      for (int i = 0; i < loadMapButton.styleSheets.count; i++)
      {
        heightsButton.styleSheets.Add(loadMapButton.styleSheets[i]);
      }

      foreach (var className in loadMapButton.GetClasses())
      {
        heightsButton.AddToClassList(className);
      }

      heightsButton.style.width = loadMapButton.style.width;
      heightsButton.style.height = loadMapButton.style.height;

      // REFLECTION MAGIC: 
      // MainMenuPanel doesn't have DialogBoxShower, but it has _loadGameBox.
      // _loadGameBox DOES have _dialogBoxShower. We reflect through it.
      FieldInfo loadGameBoxField = AccessTools.Field(typeof(MainMenuPanel), "_loadGameBox");
      object loadGameBoxInstance = loadGameBoxField?.GetValue(__instance);

      FieldInfo dialogShowerField = AccessTools.Field(loadGameBoxInstance?.GetType(), "_dialogBoxShower");
      DialogBoxShower dialogShower = (DialogBoxShower)dialogShowerField?.GetValue(loadGameBoxInstance);

      heightsButton.RegisterCallback<ClickEvent>(evt =>
      {
        if (dialogShower != null) ShowHeightsDialog(dialogShower);
      });

      VisualElement container = loadMapButton.parent;
      if (container != null)
      {
        // Insert right below the Edit Map button
        int insertIndex = container.IndexOf(loadMapButton) + 1;
        container.Insert(insertIndex, heightsButton);

        // --- SPACING ADJUSTMENT (From SyncMods) ---
        var menuItems = container.Children().ToList();
        foreach (var element in menuItems)
        {
          if (element is Button)
          {
            element.style.marginBottom = new Length(-1, LengthUnit.Pixel);
            element.style.marginTop = new Length(-1, LengthUnit.Pixel);
          }
        }
      }
    }

    private static void ShowHeightsDialog(DialogBoxShower dialogBoxShower)
    {
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

      dialogBoxShower.Create()
        .SetMessage("Set custom map heights. These will apply to the next game you load or create.")
        .AddContent(container)
        .SetCancelButton(() => { }, "Cancel")
        .SetConfirmButton(() =>
        {
          if (int.TryParse(terrainField.value, out int tHeight)) MapHeightOverrideStates.TerrainHeight = tHeight;
          if (int.TryParse(aboveField.value, out int aHeight)) MapHeightOverrideStates.AboveHeight = aHeight;

          MapHeightOverrideStates.UseOverride = true;
          MapHeightOverrideStates.IsLoadingGame = true; // Protect these values from SaveMetadata resets

        }, "Apply")
        .Show();
    }
  }
}