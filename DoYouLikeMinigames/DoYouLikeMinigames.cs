using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Su226.DoYouLikeMinigames {
  class M {
    public static Config Config;
    public static IModHelper Helper;
    public static IMonitor Monitor;
  }

  class DoYouLikeMinigames : Mod {
    public override void Entry(IModHelper helper) {
      M.Config = Helper.ReadConfig<Config>();
      M.Helper = Helper;
      M.Monitor = Monitor;
      helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object o, ButtonPressedEventArgs e) {
      if (e.Button != M.Config.key) {
        return;
      }
      Game1.activeClickableMenu = new GameSelectMenu();
    }
  }
}