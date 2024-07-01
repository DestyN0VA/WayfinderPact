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
using static System.Net.Mime.MediaTypeNames;

namespace SwordAndSorcerySMAPI
{
    internal static class Spells
    {
        public static void Haste()
        {
            var buff = new Buff("spell_haste", "spell_haste", duration: 7000 * 6 * 5, effects: new StardewValley.Buffs.BuffEffects() { Speed = { 1 } }, displayName: I18n.Witchcraft_Spell_Haste_Name() );
            Game1.player.applyBuff( buff );
        }

        public static void MageArmor()
        {
            Game1.player.GetFarmerExtData().mageArmor = true;
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

        public static void MirrorImage()
        {
            Game1.player.GetFarmerExtData().mirrorImages.Value = 3;

            Game1.player.playNearbySoundLocal("coldSpell");

            for (int im = 1; im <= 3; ++im)
            {
                Vector2 spot = Game1.player.StandingPixel.ToVector2();
                float rad = (float)-Game1.currentGameTime.TotalGameTime.TotalSeconds / 3 * 2;
                /*
                switch (im)
                {
                    case 1: spot -= new Vector2(0, Game1.tileSize); break;
                    case 2: spot += new Vector2(-Game1.tileSize, Game1.tileSize); break;
                    case 3: spot += new Vector2(Game1.tileSize, Game1.tileSize); break;
                }
                */
                rad += MathF.PI * 2 / 3 * (im - 1);
                spot += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);

                for (int i = 0; i < 8; ++i)
                {
                    Vector2 diff = new Vector2(Game1.random.Next(96) - 48, Game1.random.Next(96) - 48);
                    Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, spot - new Vector2(32, 48) + diff, flicker: false, flipped: false));
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
