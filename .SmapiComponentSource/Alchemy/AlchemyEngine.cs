using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceShared;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System.IO;

namespace SwordAndSorcerySMAPI.Alchemy
{
    public class AlchemyEngine
    {
        public AlchemyEngine()
        {
            SoundEffect alchemyParticlize = SoundEffect.FromFile(Path.Combine(ModSnS.instance.Helper.DirectoryPath, "assets", "alchemy-particlize.wav"));
            Game1.soundBank.AddCue(new CueDefinition("spacechase0.MageDelve_alchemy_particlize", alchemyParticlize, 3));
            SoundEffect alchemySynthesize = SoundEffect.FromFile(Path.Combine(ModSnS.instance.Helper.DirectoryPath, "assets", "alchemy-synthesize.wav"));
            Game1.soundBank.AddCue(new CueDefinition("spacechase0.MageDelve_alchemy_synthesize", alchemySynthesize, 3));

            ModSnS.instance.Helper.ConsoleCommands.Add("sns_alchemy", "...", OnAlchemyCommand);
        }

        private void OnAlchemyCommand(string arg1, string[] arg2)
        {
            if (!Context.IsPlayerFree)
                return;

            Game1.activeClickableMenu = new FancyAlchemyMenu();
        }
    }
}
