using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Su226.GreatEnchanter {
  class M {
    public static IModHelper Helper;
    public static IMonitor Monitor;
    public static Config Config;
  }
  class GreatEnchanter : Mod {
    public override void Entry(IModHelper helper) {
      M.Helper = Helper;
      M.Monitor = Monitor;
      M.Config = helper.ReadConfig<Config>();
      helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object o, ButtonPressedEventArgs e) {
      if (e.Button != M.Config.key) {
        return;
      }
      if (!Game1.player.CanMove) {
        Monitor.Log("Can't save: Can't move");
        return;
      }
      if (Game1.activeClickableMenu != null) {
        Monitor.Log("Can't save: Menu open");
        return;
      }
      if (Game1.currentMinigame != null) {
        Monitor.Log("Can't save: Minigame open");
        return;
      }
      Game1.playSound("bigSelect");
      Game1.activeClickableMenu = new EnchantMenu();
    }
  }
}