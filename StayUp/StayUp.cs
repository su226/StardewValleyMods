using Harmony;
using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Characters;

namespace Su226.StayUp {
  public class StayUp : Mod {
    private Config config;
    private int morningBegin;
    private int morningDuration;

    private bool canCallNewDay; // Prevent sleep repeatedly.
    private bool restoreData;

    private string map;
    private Vector2 pos;
    private int facing;
    private ISittable sitted;
    private float stamina;
    private int health;

    private Horse horse;
    private string horseMap;
    private Vector2 horsePos;
    private int horseFacing;

    private Color lightColor = Color.Transparent;
    private Color darkColor = new Color(237, 237, 0, 237);
    private Color rainLightColor = new Color(76, 60, 24, 76);
    private Color rainDarkColor = new Color(237, 186, 74, 237);
    private Color outdoorLight;

    public override void Entry(IModHelper helper) {
      this.Monitor.Log("Starting.");
      this.config = helper.ReadConfig<Config>();
      this.morningBegin = this.config.morningLight / 100 * 60 + this.config.morningLight % 100;
      this.morningDuration = 360 - this.morningBegin;

      helper.Events.GameLoop.DayStarted += this.OnDayStarted;
      helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
      helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;

      HarmonyInstance harmony = HarmonyInstance.Create(this.ModManifest.UniqueID);
      if (this.config.noTiredEmote) {
        this.Monitor.Log("Tired emote supression enabled.");
        FarmerPatches.Init(this.Monitor);
        harmony.Patch(
          AccessTools.Method(typeof(Farmer), "doEmote", new Type[] { typeof(int) }),
          new HarmonyMethod(typeof(FarmerPatches), "PreDoEmote")
        );
      }

      if (this.config.editFishes) {
        this.Monitor.Log("Fish editing enabled.");
        helper.Content.AssetEditors.Add(new FishEditor(this.Monitor));
      }
    }

    private void OnDayStarted(object o, DayStartedEventArgs e) {
      if (this.restoreData) {
        if (this.config.keepFarmer) {
          this.Monitor.Log("Restore player position.");
          LocationRequest request = Game1.getLocationRequest(map);
          request.OnWarp += delegate {
            Game1.fadeToBlackAlpha = this.config.smoothSaving ? 1 : -.2f; // Hide fading from black
            Game1.player.Position = this.pos; // Set player's float position
            if (this.sitted != null) { // Sit down
              this.sitted.AddSittingFarmer(Game1.player);
              Game1.player.sittingFurniture = this.sitted;
              Game1.player.isSitting.Value = true;
            }
          };
          Game1.warpFarmer(request, 0, 0, this.facing);
          Game1.fadeToBlackAlpha = 1.2f; // Hide fading to black
          if (Game1.player.mount != null) { // Remove the orphaned horse.
            Game1.getFarm().characters.Remove(Game1.getFarm().getCharacterFromName(Game1.player.horseName));
          }
        } else {
          this.Monitor.Log("Discard player position.");
          Horse mountedHorse = Game1.player.mount;
          if (mountedHorse != null) { // Dismount and remove horse
            mountedHorse.dismount();
            mountedHorse.currentLocation.characters.Remove(mountedHorse);
          }
          Game1.player.changeOutOfSwimSuit(); // Reset from bathhub
          Game1.player.swimming.Value = false;
        }
        if (this.config.keepStamina) {
          this.Monitor.Log("Restore player stamina.");
          Game1.player.stamina = this.stamina;
        }
        if (this.config.keepHealth) {
          this.Monitor.Log("Restore player health.");
          Game1.player.health = this.health;
        }
        if (this.config.keepHorse && this.horse != null) {
          this.Monitor.Log("Restore horse position.");
          Game1.warpCharacter(this.horse, this.horseMap, this.horsePos / 64);
          this.horse.faceDirection(this.horseFacing);
        }
        this.restoreData = false;
      }
      this.canCallNewDay = false;
      this.outdoorLight = Game1.isRaining ? this.rainLightColor : this.lightColor;
    }

    private void OnTimeChanged(object o, TimeChangedEventArgs e) {
      if (Game1.dayTimeMoneyBox.timeShakeTimer != 0 && this.config.noTimeShake) {
        this.Monitor.Log("Time shake supressed.");
        Game1.dayTimeMoneyBox.timeShakeTimer = 0;
      }
      if (Game1.timeOfDay == 2550 && this.config.stayUp) {
        this.Monitor.Log("Stay up.");
        this.canCallNewDay = this.config.newDayAt6Am;
        this.outdoorLight = Game1.isRaining ? this.rainDarkColor : this.darkColor;
        Game1.timeOfDay = 150;
      }
      if (e.NewTime > this.config.morningLight && e.NewTime <= 600) {
        double d = e.NewTime / 100 * 60 + e.NewTime % 100 - this.morningBegin;
        this.outdoorLight = this.GetAverageColor(
          Game1.isRaining ? this.rainLightColor : this.lightColor,
          Game1.isRaining ? this.rainDarkColor : this.darkColor,
          Math.Pow(d / this.morningDuration, this.config.morningLightPower)
        );
      }
      if (e.NewTime == 600 && this.canCallNewDay) {
        this.NewDayStayUp();
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

    private void NewDayStayUp() {
      this.Monitor.Log("Save player data.");
      this.restoreData = true;
      this.map = Game1.player.currentLocation.NameOrUniqueName;
      this.pos = Game1.player.Position;
      this.facing = Game1.player.facingDirection;
      this.sitted = Game1.player.sittingFurniture;
      this.stamina = Game1.player.stamina;
      this.health = Game1.player.health;
      this.horse = Game1.getCharacterFromName<Horse>(Game1.player.horseName, false);
      if (this.horse != null) {
        this.Monitor.Log("Save horse data.");
        this.horseMap = this.horse.currentLocation.NameOrUniqueName;
        this.horsePos = this.horse.Position;
        this.horseFacing = this.horse.FacingDirection;
      }
      this.Monitor.Log("Start new day.");
      Game1.player.passedOut = true;
      if (Game1.IsMultiplayer) {
        if (Game1.activeClickableMenu != null) {
          Game1.activeClickableMenu.emergencyShutDown();
          Game1.exitActiveMenu();
        }
        Game1.activeClickableMenu = new ReadyCheckDialog("sleep", false, delegate {
          Game1.NewDay(0f);
        }, null);
      } else {
        Game1.NewDay(0f);
        Game1.fadeToBlackAlpha = this.config.smoothSaving ? 0 : 1.2f;
      }
    }

    private void OnUpdateTicked(object o, UpdateTickedEventArgs e) {
      if (Game1.timeOfDay <= 600) { // Keep correct light when rain
        Game1.outdoorLight = this.outdoorLight;
      }
    }
  }
}