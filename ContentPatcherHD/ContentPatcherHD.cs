﻿using Harmony;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace Su226.ContentPatcherHD {
  class M {
    public static IMonitor Monitor;
    public static IModHelper Helper;
  }

  class Data {
    public string[] ScaleRequests = null;
  }

  class ContentPatcherHD : Mod, IAssetEditor, IAssetLoader {
    private ICollection<string> Requests = new HashSet<string>();
    private IDictionary<string, Texture2DWrapper> ScaledAssets = new Dictionary<string, Texture2DWrapper>();

    public override void Entry(IModHelper helper) {
      M.Monitor = Monitor;
      M.Helper = Helper;
      HarmonyInstance harmony = HarmonyInstance.Create(ModManifest.UniqueID);
      SpriteBatchOverrides.PatchAll(harmony);
      AssetDataForImageOverrides.PatchAll(harmony);
      IModInfo info = Helper.ModRegistry.Get("Pathoschild.ContentPatcher");
      Mod cp = (Mod)info.GetType().GetProperty("Mod").GetValue(info);
      foreach (IContentPack pack in cp.Helper.ContentPacks.GetOwned()) {
        Data data = pack.ReadJsonFile<Data>("content.json");
        if (data.ScaleRequests != null) {
          foreach (string res in data.ScaleRequests) {
            Requests.Add(Normalize(res));
          }
        }
      }
    }

    public bool CanEdit<T>(IAssetInfo asset) {
      return Requests.Contains(Normalize(asset.AssetName));
    }

    public void Edit<T>(IAssetData asset) {
      IAssetDataForImage image = asset.AsImage();
      string name = Normalize(asset.AssetName) + ".4x";
      if (!ScaledAssets.ContainsKey(name)) {
        ScaledAssets[name] = new Texture2DWrapper(image.Data, 4, name);
      }
      Texture2DWrapper wrapper = ScaledAssets[name];
      if (wrapper.Locale != asset.Locale) {
        Monitor.Log($"Upscaling {asset.AssetName} (Locale: {asset.Locale})");
        wrapper.Wrapped = Texture2DWrapper.ScaleUp(image.Data, wrapper.Scale);
        wrapper.Locale = asset.Locale;
      }
      image.ReplaceWith(wrapper);
      Helper.Content.Load<Texture2D>(wrapper.Path, ContentSource.GameContent);
    }

    public bool CanLoad<T>(IAssetInfo asset) {
      return ScaledAssets.ContainsKey(Normalize(asset.AssetName));
    }

    public T Load<T>(IAssetInfo asset) {
      Texture2DWrapper wrapper = ScaledAssets[Normalize(asset.AssetName)];
      return (T)(object)wrapper.Wrapped;
    }

    private string Normalize(string original) {
      return PathUtilities.NormalizePath(original).ToLower();
    }
  }
}
