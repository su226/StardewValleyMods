using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Su226.StayUp {
  class LightTransition {
    Config config;

    private readonly int morningBegin;
    private readonly int morningDuration;

    private Color lightColor = Color.Transparent;
    private Color darkColor = new Color(237, 237, 0, 237);
    private Color rainLightColor = new Color(76, 60, 24, 76);
    private Color rainDarkColor = new Color(237, 186, 74, 237);
    private Color outdoorLight;

    public LightTransition(IModHelper helper, Config config) {
      this.config = config;
      this.morningBegin = config.morningLight / 100 * 60 + config.morningLight % 100;
      this.morningDuration = 360 - this.morningBegin;
      helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
      helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    public void UseLightColor() {
      this.outdoorLight = Game1.isRaining ? this.rainLightColor : this.lightColor;
    }

    public void UseDarkColor() {
      this.outdoorLight = Game1.isRaining ? this.rainDarkColor : this.darkColor;
    }

    private void OnTimeChanged(object o, TimeChangedEventArgs e) {
      if (e.NewTime > this.config.morningLight && e.NewTime <= 600) {
        double d = e.NewTime / 100 * 60 + e.NewTime % 100 - this.morningBegin;
        this.outdoorLight = this.GetAverageColor(
          Game1.isRaining ? this.rainLightColor : this.lightColor,
          Game1.isRaining ? this.rainDarkColor : this.darkColor,
          Math.Pow(d / this.morningDuration, this.config.morningLightPower)
        );
      }
    }

    private void OnUpdateTicked(object o, UpdateTickedEventArgs e) {
      if (Game1.timeOfDay <= 600) { // Keep correct light when rain
        Game1.outdoorLight = this.outdoorLight;
      }
    }

    private Color GetAverageColor(Color c1, Color c2, double ratio1) {
      double ratio2 = 1 - ratio1;
      return new Color(
        (byte)(c1.R * ratio1 + c2.R * ratio2),
        (byte)(c1.G * ratio1 + c2.G * ratio2),
        (byte)(c1.B * ratio1 + c2.B * ratio2),
        (byte)(c1.A * ratio1 + c2.A * ratio2)
      );
    }
  }
}