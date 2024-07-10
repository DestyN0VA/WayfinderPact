using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NeverEndingAdventure.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Minigames;
using System.Collections.Generic;
using System.IO;

namespace SwordAndSorcerySMAPI
{
    internal class FinalePhase1Minigame : IMinigame
    {

        public bool Finished { get; set; } = false;
        public Event Event { get; }
        public EventContext Context { get; }

        public FinalePhase1Minigame(Event @event, EventContext context)
        {
            Event = @event;
            Context = context;
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
            Finished = true;
        }

        public void receiveRightClick(int x, int y, bool playSound = true)
        {
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

            b.Begin();
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, windowSize.X, windowSize.Y), Color.Black);

            SpriteText.drawStringHorizontallyCenteredAt(b, "Click to simulate victory", windowSize.X / 2, windowSize.Y / 2);

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
            commands.Insert(Event.CurrentCommand + 1, $"switchEvent {partnerInfo.IntermissionEventId}");
            Event.eventCommands = commands.ToArray();

            Event.CurrentCommand++;
        }
    }
}