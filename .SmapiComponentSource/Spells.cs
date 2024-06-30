using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwordAndSorcerySMAPI
{
    internal static class Spells
    {
        public static void Haste()
        {
            var buff = new Buff("spell_haste", "spell_haste", duration: 7000 * 6 * 5, effects: new StardewValley.Buffs.BuffEffects() { Speed = { 1 } }, displayName: I18n.Witchcraft_Spell_Haste_Name() );
            Game1.player.applyBuff( buff );
        }

        public static void RevivePlant()
        {
            for (int ix = -2; ix <= 2; ++ix)
            {
                for (int iy = -2; iy <= 2; ++iy)
                {
                    Vector2 pos = Game1.player.Tile + new Vector2(ix, iy);
                    if (Game1.player.currentLocation.terrainFeatures.TryGetValue(pos, out TerrainFeature tf) && tf is HoeDirt hd)
                    {
                        if (hd.crop.dead.Value)
                        {
                            hd.crop.dead.Value = false;
                        }
                    }
                }
            }
        }

        public static void PocketChest()
        {
            string invName = $"{ModTOP.Instance.ModManifest.UniqueID}/PocketChest/{Game1.player.UniqueMultiplayerID}";

            var chest = new Chest();
            chest.GlobalInventoryId = invName;
            chest.ShowMenu();
        }
    }
}
