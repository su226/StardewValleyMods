using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Minigames;
using System;

namespace Su226.DoYouLikeMinigames {
  class GameSelectMenu : IClickableMenu {
    private int buttonHeight;
    private Rectangle[] buttons = new Rectangle[6];
    private int offset = 0;

    private ClickableTextureComponent upArrow;
    private ClickableTextureComponent downArrow;
 
    private string error;
    private int errorTimer;
    private Random random = new Random();

    private Playable[] games = new Playable[] {
      new MinigamePlayable("abigail_game", () => new AbigailGame()),
      new MinigamePlayable("abigail_game_two", () => {
        if (M.Config.checkFriendship && Game1.player.getFriendshipHeartLevelForNPC("Abigail") < 2) {
          throw new CannotPlay("no_friendship");
        }
        return new AbigailGameTwo();
      }),
      new CalicoPlayable("calico_jack", 0, false),
      new CalicoPlayable("calico_jack_small", 100, false),
      new CalicoPlayable("calico_jack_big", 1000, true),
      new CranePlayable("crane", () => {
        if (M.Config.checkMoney) {
          if (Game1.player.Money < 500) {
            throw new CannotPlay("no_money");
          } else {
            Game1.player.Money -= 500;
          }
        }
        return new CraneGame();
      }),
      new CranePlayable("crane_practice", () => new CranePractice()),
      new MinigamePlayable("darts", () => {
        if (M.Config.checkIsland && !Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatFixed")) {
          throw new CannotPlay("no_island");
        }
        return new Darts();
      }),
      new FairPlayable("fishing", () => new FishingGameFixed()),
      new KartPlayable("minecart_endless", () => new MineCart(0, 2)),
      new KartPlayable("minecart_progress", () => new MineCart(0, 3)),
      new KartPlayable("old_minecart_endless", () => new OldMineCart(0, 2)),
      new KartPlayable("old_minecart_progress", () => new OldMineCart(0, 3)),
      new ClubPlayable("slots", () => new Slots()),
      new FairPlayable("target", () => new TargetGameFixed())
    };

    public GameSelectMenu() : base(
      (Game1.uiViewport.Width - 800) / 2,
      (Game1.uiViewport.Height  - 600) / 2,
      800,
      600,
      true
    ) {
      upArrow = new ClickableTextureComponent(
        new Rectangle(0, 0, 44, 48),
        Game1.mouseCursors,
        new Rectangle(421, 459, 11, 12),
        4f
      );
      downArrow = new ClickableTextureComponent(
        new Rectangle(0, 0, 44, 48),
        Game1.mouseCursors,
        new Rectangle(421, 472, 11, 12),
        4f
      );
      buttonHeight = (height - 32) / buttons.Length;
      for (int i = 0; i < buttons.Length; i++) {
        buttons[i] = new Rectangle(0, 0, width - 32, buttonHeight);
      }
      PlaceWidgets();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) {
      base.receiveLeftClick(x, y);
      for (int i = 0; i < buttons.Length; i++) {
        if (buttons[i].Contains(x, y)) {
          try {
            games[i + offset].Play();
            this.exitThisMenu();
          } catch (CannotPlay e) {
            error = e.Message;
            Game1.playSound("cancel");
            errorTimer = 120;
          }
          break;
        }
      }
      if (upArrow.containsPoint(x, y)) {
        ScrollUp();
      }
      if (downArrow.containsPoint(x, y)) {
        ScrollDown();
      }
    }
    
    public override void receiveScrollWheelAction(int direction) {
      if (direction > 0) {
        ScrollUp();
      } else {
        ScrollDown();
      }
    }

    public override void update(GameTime time) {
      if (errorTimer > 0) {
        if (--errorTimer == 0) {
          error = null;
        }
      }
    }

    public override void gameWindowSizeChanged(Rectangle oldrect, Rectangle newrect) {
      base.gameWindowSizeChanged(oldrect, newrect);
      PlaceWidgets();
    }

    public void ScrollUp() {
      if (offset > 0) {
        Game1.playSound("shiny4");
        offset--;
      }
    }

    public void ScrollDown() {
      if (offset < games.Length - buttons.Length) {
        Game1.playSound("shiny4");
        offset++;
      }
    }

    public void PlaceWidgets() {
      for (int i = 0; i < buttons.Length; i++) {
        buttons[i].X = xPositionOnScreen + 16;
        buttons[i].Y = yPositionOnScreen + 16 + i * buttonHeight;
      }
      upArrow.bounds.X = xPositionOnScreen + width + 16;
      upArrow.bounds.Y = yPositionOnScreen + 16;
      downArrow.bounds.X = xPositionOnScreen + width + 16;
      downArrow.bounds.Y = yPositionOnScreen + height - 64;
    }

    public override void draw(SpriteBatch b) {
      if (!Game1.options.showMenuBackground) {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
      }
      IClickableMenu.drawTextureBox(
        b,
        Game1.mouseCursors,
        new Rectangle(384, 373, 18, 18),
        xPositionOnScreen,
        yPositionOnScreen,
        width,
        height,
        Color.White,
        4f
      );
      for (int i = 0; i < buttons.Length; i++) {
        IClickableMenu.drawTextureBox(
          b,
          Game1.mouseCursors,
          new Rectangle(384, 396, 15, 15),
          buttons[i].X,
          buttons[i].Y,
          buttons[i].Width,
          buttons[i].Height + 4,
          buttons[i].Contains(Game1.getMouseX(), Game1.getMouseY()) ? Color.Wheat : Color.White,
          4f,
          false
        );
        SpriteText.drawString(
          b,
          games[i + offset].name,
          buttons[i].X + 48,
          buttons[i].Y + buttons[i].Height / 2 - 24
        );
      }
      if (error != null) {
        SpriteText.drawString(
          b,
          error,
          xPositionOnScreen + 16 + random.Next(-8, 8),
          yPositionOnScreen + height + 16 + random.Next(-8, 8),
          color: 2
        );
      } else {
        SpriteText.drawString(
          b,
          string.Format("{0} - {1} ({2})", offset + 1, offset + buttons.Length, games.Length),
          xPositionOnScreen + 16,
          yPositionOnScreen + height + 16,
          color: 4
        );
      }
      upArrow.draw(b);
      downArrow.draw(b);
      drawMouse(b);
    }
  }
}
