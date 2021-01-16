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
      Game1.activeClickableMenu = new EnchantMenu();
    }
  }
}