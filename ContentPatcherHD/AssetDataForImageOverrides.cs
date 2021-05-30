using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.Reflection;

namespace Su226.ContentPatcherHD {
  class AssetDataForImageOverrides {
    public static void PatchAll(HarmonyInstance harmony) {
      harmony.Patch(
        AccessTools.Method("StardewModdingAPI.Framework.Content.AssetDataForImage:PatchImage"),
        new HarmonyMethod(typeof(AssetDataForImageOverrides), "PatchImage")
      );
    }

    public static bool PatchImage(IAssetDataForImage __instance, Texture2D source, Rectangle? sourceArea, Rectangle? targetArea, PatchMode patchMode) {
      if (__instance.Data is Texture2DWrapper wrapper) {
        if (sourceArea.HasValue) {
          source = Texture2DWrapper.Crop(source, sourceArea.Value);
        }
        source = Texture2DWrapper.ScaleUp(source, wrapper.Scale);
        if (targetArea.HasValue) {
          targetArea = Texture2DWrapper.MultiplyRect(targetArea.Value, wrapper.Scale);
        }
        PropertyInfo Data = __instance.GetType().GetProperty("Data");
        Data.SetValue(__instance, wrapper.Wrapped);
        __instance.PatchImage(source, null, targetArea, patchMode);
        Data.SetValue(__instance, wrapper);
        return false;
      }
      return true;
    }
  }
}
