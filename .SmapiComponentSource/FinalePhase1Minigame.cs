using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NeverEndingAdventure.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using StardewValley.Minigames;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwordAndSorcerySMAPI
{
    [HarmonyPatch(typeof(GameLocation), "drawFarmers")]
    public class GameLocationDrawFarmerInFinalePatch
    {
        public static void Postfix(GameLocation __instance, SpriteBatch b)
        {
            if (Game1.currentMinigame is FinalePhase1Minigame)
                Game1.player.draw(b);
        }
    }

    public class BattlerInfo
    {
        public int Health { get; set; } = 100;
        public int Defense { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }

        public Action Ability { get; set; }

        public bool InShadowstep { get; set; } = false;
    }

    internal class FinalePhase1Minigame : IMinigame
    {

        public bool Finished { get; set; } = false;
        public Event Event { get; }
        public EventContext Context { get; }

        public Texture2D BossSprite { get; set; }

        public Character CurrentTurn { get; set; }

        public List<Character> Battlers { get; set; } = new();
        public Dictionary<string, BattlerInfo> BattlerData { get; set; } = new();

        public FinalePhase1Minigame(Event @event, EventContext context)
        {
            Event = @event;
            Context = context;

            BossSprite = Game1.content.Load<Texture2D>("Characters/Monsters/Angry Roger");

            CurrentTurn = @event.actors.First();

            Game1.fadeToBlack = false; // WHY WAS THIS KEEPING THINGS FROM TICKING

            Battlers.AddRange(@event.actors);
            Battlers.Insert(1, Game1.player);

            BattlerData = new()
            {
                { "Mateo", new BattlerInfo() { Defense = 8, Mana = 30, MaxMana = 30 } },
                { Game1.player.Name, new BattlerInfo() { Defense = Game1.player.buffs.Defense + (Game1.player.GetArmorItem()?.GetArmorAmount() ?? 0) / 25, Mana = Game1.player.GetFarmerExtData().mana.Value, MaxMana = Game1.player.GetFarmerExtData().maxMana.Value } },
                { "Dandelion", new BattlerInfo() { Defense = 12, Mana = 40, MaxMana = 40 } },
                { "Hector", new BattlerInfo() { Defense = 3, Mana = 70, MaxMana = 70 } },
                { "Cirrus", new BattlerInfo() { Defense = 0, Mana = 50, MaxMana = 50 } },
                { "Roslin", new BattlerInfo() { Defense = 0, Mana = 100, MaxMana = 100 } },
                { "Gunnar", new BattlerInfo() { Defense = 5, Mana = 25, MaxMana = 25 } }
            };

            foreach (var actor in @event.actors.ToList())
            {
                if (!BattlerData.ContainsKey(actor.Name) && actor.Name != "Duskspire")
                    @event.actors.Remove(actor);
            }
        }

        public string minigameId()
        {
            return $"{ModSnS.instance.ModManifest.UniqueID}_finale_phase1";
        }

        public void changeScreenSize()
        {
        }
        public void receiveKeyPress(Keys k)
        {
        }

        public void receiveKeyRelease(Keys k)
        {
        }

        public void receiveLeftClick(int x, int y, bool playSound = true)
        {
        }

        public void receiveRightClick(int x, int y, bool playSound = true)
        {
            Finished = true;
        }

        public void releaseLeftClick(int x, int y)
        {
        }

        public void releaseRightClick(int x, int y)
        {
        }

        public bool tick(GameTime time)
        {
            return Finished;
        }

        public void draw(SpriteBatch b)
        {
            Point windowSize = new(Game1.game1.Window.ClientBounds.Width, Game1.game1.Window.ClientBounds.Height);

            Game1.viewportCenter = new Point(14 * Game1.tileSize, 13 * Game1.tileSize);
            Game1.UpdateViewPort(overrideFreeze: true, Game1.viewportCenter);

            /*
            b.Begin();
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, windowSize.X, windowSize.Y), Color.Black);
            b.End();
            */

            Game1.currentLocation.Map.Update(Game1.currentGameTime.ElapsedGameTime.Milliseconds);
            Game1.game1.DrawWorld(Game1.currentGameTime, Game1.game1.ShouldDrawOnBuffer() ? Game1.game1.screen : null);

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            /*
            Vector2 bossPos = new Vector2(18, 13);
            Rectangle bossSrc = new Rectangle(0, 96, 32, 32);
            bossPos *= Game1.tileSize;
            b.Draw(BossSprite, Game1.GlobalToLocal(bossPos - new Vector2((bossSrc.Width - 16) / 2, bossSrc.Height - 16) * Game1.pixelZoom), bossSrc, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, bossPos.Y / 10000f);
            */

            if (CurrentTurn != null)
            {
                var rect = new Rectangle(324, 477, 7, 19);
                b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(CurrentTurn.StandingPixel.ToVector2() - new Vector2(12, 200 + 8 * MathF.Sin( (float) Game1.currentGameTime.TotalGameTime.TotalSeconds * 4 ))), rect, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.FlipVertically, 1);

                int y = Game1.viewport.Height - 200 - 16 - 64;

                //IClickableMenu.drawTextureBox(b, 16, y, 275, 80, Color.White);
                IClickableMenu.drawTextureBox(b, 16, y + 64, 450, 200, Color.White);

                SpriteText.drawStringWithScrollCenteredAt(b, CurrentTurn.displayName, 16 + 450 / 2, y, 350);
                SpriteText.drawString(b, "<100/100^=50/50", 250, y + 16 + 64);

                Utility.drawTextWithShadow(b, "   Attack", Game1.dialogueFont, new Vector2(40, y + 80 + 40 * 0), Color.Black);
                Utility.drawTextWithShadow(b, "   Ability", Game1.dialogueFont, new Vector2(40, y + 80 + 40 * 1), Color.Black);
                Utility.drawTextWithShadow(b, "> Guard", Game1.dialogueFont, new Vector2(40, y + 80 + 40 * 2), Color.Black);
                Utility.drawTextWithShadow(b, "   Use Potion   (3 left)", Game1.dialogueFont, new Vector2(40, y + 80 + 40 * 3), Color.Black);

                int x = Game1.viewport.Width - 450 - 16;

                //IClickableMenu.drawTextureBox(b, x, y + 64, 450, 200, Color.White);

                //SpriteText.drawStringWithScrollCenteredAt(b, "Shadowstep =25", x + 450 / 2, y, 350);
                //Utility.drawTextWithShadow(b, "Step into the shadows, making the\nuser be unable to be targeted\ndirectly until their next attack.\nThe next attack will do critical\ndamage.", Game1.smallFont, new Vector2( x + 32, y + 96 ), Color.Black );
            }

            //SpriteText.drawStringHorizontallyCenteredAt(b, "Click to simulate victory", windowSize.X / 2, windowSize.Y / 2);

            b.End();
        }

        public bool doMainGameUpdates()
        {
            return false;
        }

        public bool forceQuit()
        {
            return false;
        }

        public void leftClickHeld(int x, int y)
        {
        }

        public bool overrideFreeMouseMovement()
        {
            return false;
        }

        public void receiveEventPoke(int data)
        {
        }

        public void unload()
        {
            // This is really bad. Pathos don't kill me.
            var modInfo = ModSnS.instance.Helper.ModRegistry.Get("DN.SnS");
            var pack = modInfo.GetType().GetProperty("ContentPack")?.GetValue(modInfo) as IContentPack;
            var partnerInfos = pack.ReadJsonFile<Dictionary<string, FinalePartnerInfo>>("Data/FinalePartners.json");

            FinalePartnerInfo partnerInfo;
            if (Game1.player.spouse == null || !partnerInfos.TryGetValue(Game1.player.spouse, out partnerInfo))
                partnerInfo = partnerInfos["default"];

            var commands = new List<string>(Event.eventCommands);
            commands.Insert(Event.CurrentCommand + 1, $"switchEventFull {partnerInfo.IntermissionEventId}");
            Event.eventCommands = commands.ToArray();

            Event.CurrentCommand++;
        }
    }
}