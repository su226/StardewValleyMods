using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace Su226.AnySave {
  class AnySave : Mod {
    public Config config;

    private SaveData saveData;

    public override void Entry(IModHelper helper) {
      this.config = helper.ReadConfig<Config>();

      helper.Events.Input.ButtonPressed += this.OnButtonPressed;
      helper.Events.GameLoop.Saving += this.OnSaving;
      helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
      helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
    }

    private void OnButtonPressed(object o, ButtonPressedEventArgs e) {
      if (e.Button != this.config.saveButton) {
        return;
      }
      if (!Game1.player.CanMove) {
        Monitor.Log("Can't save: Can't move");
        return;
      }
      if (Game1.eventUp) {
        Monitor.Log("Can't save: Event up");
        return;
      }
      if (Game1.activeClickableMenu != null) {
        Monitor.Log("Can't save: Menu open");
        return;
      }
      if (!Game1.IsMasterGame) {
        Game1.addHUDMessage(new HUDMessage(
          Helper.Translation.Get("Su226.AnySave.requestSent"),
          HUDMessage.newQuest_type
        ));
        Helper.Multiplayer.SendMessage(
          Game1.player.UniqueMultiplayerID,
          "Su226.AnySave.Request",
          new string[] { "Su226.AnySave" },
          new long[] { Game1.MasterPlayer.UniqueMultiplayerID }
        );
        return;
      }
      // Save all NPCs.
      Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();
      foreach (GameLocation l in Game1.locations) {
        foreach (NPC c in l.characters) {
          string key = c.DefaultMap + c.Name;
          Monitor.Log(string.Format("Save NPC {0}", key));
          characters[key] = new CharacterData {
            map = c.currentLocation.NameOrUniqueName,
            x = c.position.X,
            y = c.position.Y,
            facing = c.facingDirection
          };
        }
      }
      // Save all players.
      Dictionary<long, FarmerData> farmers = new Dictionary<long, FarmerData>();
      foreach (Farmer f in Game1.getAllFarmers()) {
        Monitor.Log(string.Format("Save player {0}({1})", f.name, f.uniqueMultiplayerID));
        if (f.mount != null) {
          characters[f.mount.Name] = new CharacterData {
            map = f.currentLocation.NameOrUniqueName,
            x = f.position.X,
            y = f.position.Y,
            facing = f.facingDirection
          };
        }
        farmers[f.uniqueMultiplayerID] = new FarmerData {
          map = f.currentLocation.NameOrUniqueName,
          x = f.position.X,
          y = f.position.Y,
          facing = f.facingDirection,
          swimSuit = f.bathingClothes,
          swimming = f.swimming,
          horse = f.mount?.Name
        };
      }
      // Write data and save.
      Helper.Data.WriteSaveData("AnySave", new SaveData {
        character = characters,
        farmer = farmers,
        time = Game1.timeOfDay
      });
      Multiplayer mp = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
      if (mp != null) {
        Monitor.Log("Saving Farmhands.");
        mp.saveFarmhands();
      }
      Monitor.Log("Saving.");
      for (IEnumerator<int> saver = SaveGame.getSaveEnumerator(); saver.MoveNext(););
      // Show saved to all players.
      this.ShowSaved();
      Helper.Multiplayer.SendMessage("", "Su226.AnySave.ShowSaved", new string[] { "Su226.AnySave" });
    }

    private void OnModMessageReceived(object o, ModMessageReceivedEventArgs e) {
      switch (e.Type) {
      case "Su226.AnySave.GetFarmerData":
        long id = e.ReadAs<long>();
        if ((bool)this.saveData?.farmer.ContainsKey(id)) {
          Monitor.Log(string.Format("Send player {0} to {1}.", id, e.FromPlayerID));
          Helper.Multiplayer.SendMessage(
            this.saveData.farmer[id],
            "Su226.AnySave.ReceiveFarmerData",
            new string[] { e.FromModID },
            new long[] { e.FromPlayerID }
          );
        } else {
          Monitor.Log(string.Format("Can't find player {0}.", id));
        }
        break;
      case "Su226.AnySave.ReceiveFarmerData":
        Monitor.Log(string.Format("Received player from host."));
        this.RestorePlayerData(e.ReadAs<FarmerData>());
        break;
      case "Su226.AnySave.Request":
        Game1.addHUDMessage(new HUDMessage(
          Helper.Translation.Get("Su226.AnySave.requestReceived", new {
            name = Game1.getFarmer(e.ReadAs<long>()).Name
          }),
          HUDMessage.newQuest_type
        ));
        break;
      case "Su226.AnySave.ShowSaved":
        this.ShowSaved();
        break;
      }
    }

    private void OnSaving(object o, SavingEventArgs e) {
      // Erase data if saved without mod.
      Monitor.Log("Erasing.");
      Helper.Data.WriteSaveData<SaveData>("AnySave", null);
    }

    private void OnSaveLoaded(object o, SaveLoadedEventArgs e) {
      if (!Game1.IsMasterGame) {
        // Restore other player
        Monitor.Log("Getting player data from host.");
        Helper.Multiplayer.SendMessage(
          Game1.player.UniqueMultiplayerID,
          "Su226.AnySave.GetFarmerData",
          new string[] { "Su226.AnySave" },
          new long[] { Game1.MasterPlayer.UniqueMultiplayerID }
        );
        return;
      }
      // Read data from save
      this.saveData = Helper.Data.ReadSaveData<SaveData>("AnySave");
      if (this.saveData == null) {
        return;
      }
      // Restore time
      Game1.timeOfDay = this.saveData.time;
      // Restore master player
      if (this.saveData.farmer.ContainsKey(Game1.player.uniqueMultiplayerID)) {
        this.RestorePlayerData(this.saveData.farmer[Game1.player.uniqueMultiplayerID]);
      } else {
        Monitor.Log(string.Format("Can't find player {0}({1})", Game1.player.name, Game1.player.uniqueMultiplayerID));
      }
      // Restore all NPCs
      List<NPC> npcs = new List<NPC>();
      foreach (GameLocation l in Game1.locations) {
        npcs.AddRange(l.characters);
      }
      foreach (NPC c in npcs) {
        string key = c.DefaultMap + c.Name;
        if (this.saveData.character.ContainsKey(key)) {
          Monitor.Log(string.Format("Restore NPC {0}", key));
          CharacterData data2 = this.saveData.character[key];
          Game1.warpCharacter(c, data2.map, new Vector2(data2.x / 64, data2.y / 64));
          c.faceDirection(data2.facing);
        } else {
          Monitor.Log(string.Format("Can't find NPC {0}.", key));
        }
      }
    }

    private void ShowSaved() {
      Game1.playSound("money");
      Game1.addHUDMessage(new HUDMessage(
        Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11378"),
        HUDMessage.newQuest_type
      ));
    }

    private void RestorePlayerData(FarmerData data) {
      Monitor.Log(string.Format("Restore player {0}({1})", Game1.player.name, Game1.player.uniqueMultiplayerID));
      LocationRequest request = Game1.getLocationRequest(data.map);
      request.OnWarp += delegate {
        Game1.player.Position = new Vector2(data.x, data.y);
      };
      Game1.warpFarmer(request, 0, 0, data.facing);
      Game1.fadeToBlackAlpha = 1.2f;
      if (data.swimSuit) {
        Game1.player.changeIntoSwimsuit();
      }
      if (data.swimming) {
        Game1.player.swimming.Value = true;
      }
    }
  }
}