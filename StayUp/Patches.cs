using StardewModdingAPI;
using StardewValley;

namespace Su226.StayUp {
  class FarmerPatches {
    static IMonitor Monitor;

    public static void Init(IMonitor monitor) {
      Monitor = monitor;
    }

    public static bool PreDoEmote(Farmer __instance, int whichEmote) {
      if (whichEmote == 24 && !__instance.isInBed) {
        Monitor.Log("Tired emote supressed.");
        return false;
      }
      return true;
    }
  }
}