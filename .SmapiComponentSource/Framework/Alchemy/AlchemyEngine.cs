using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using SwordAndSorcerySMAPI.Framework.Menus;
using System.IO;

namespace SwordAndSorcerySMAPI.Framework.Alchemy
{
    public class AlchemyEngine(IModHelper helper)
    {
        public void InitAlchemy()
        {
            SoundEffect alchemyParticlize = SoundEffect.FromFile(Path.Combine(ModSnS.Instance.Helper.DirectoryPath, "assets", "alchemy-particlize.wav"));
            Game1.soundBank.AddCue(new CueDefinition("spacechase0.MageDelve_alchemy_particlize", alchemyParticlize, 3));
            SoundEffect alchemySynthesize = SoundEffect.FromFile(Path.Combine(ModSnS.Instance.Helper.DirectoryPath, "assets", "alchemy-synthesize.wav"));
            Game1.soundBank.AddCue(new CueDefinition("spacechase0.MageDelve_alchemy_synthesize", alchemySynthesize, 3));

            helper.ConsoleCommands.Add("sns_alchemy", "Opens the S&S alchemy menu.", OnAlchemyCommand);
        }

        private void OnAlchemyCommand(string arg1, string[] arg2)
        {
            if (!Context.IsPlayerFree)
                return;

            Game1.activeClickableMenu = new FancyAlchemyMenu();
        }
    }
}