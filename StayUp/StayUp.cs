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
    private LightTransition light;

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

    public override void Entry(IModHelper helper) {
      this.Monitor.Log("Starting.");
      this.config = helper.ReadConfig<Config>();

      helper.Events.GameLoop.DayStarted += this.OnDayStarted;
      helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;

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

      if (this.config.morningLight != -1) {
        this.light = new LightTransition(helper, this.config);
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
      this.light?.UseLightColor();
    }

    private void OnTimeChanged(object o, TimeChangedEventArgs e) {
      if (Game1.dayTimeMoneyBox.timeShakeTimer != 0 && this.config.noTimeShake) {
        this.Monitor.Log("Time shake supressed.");
        Game1.dayTimeMoneyBox.timeShakeTimer = 0;
      }
      if (Game1.timeOfDay == 2550 && this.config.stayUp) {
        this.Monitor.Log("Stay up.");
        this.canCallNewDay = this.config.newDayAt6Am;
        this.light?.UseDarkColor();
        Game1.timeOfDay = 150;
      }
      if (e.NewTime == 600 && this.canCallNewDay) {
        this.NewDayStayUp();
      }
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
  }
}