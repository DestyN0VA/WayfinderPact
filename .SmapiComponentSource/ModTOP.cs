using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwordAndSorcerySMAPI
{
    public class ModTOP
    {
        public static ModTOP Instance;

        public static Texture2D SpellCircle;

        public IMonitor Monitor;
        public IManifest ModManifest;
        public IModHelper Helper;
        public ModTOP(IMonitor monitor, IManifest manifest, IModHelper helper)
        {
            Instance = this;
            Monitor = monitor;
            ModManifest = manifest;
            Helper = helper;
        }

        private static void CastSpell(Color spellColor, Action onCast)
        {
            onCast = () => { };
            Game1.player.completelyStopAnimatingOrDoingAction();
            Game1.player.faceDirection(Game1.down);
            Game1.player.canMove = false;
            Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[]
            {
                new FarmerSprite.AnimationFrame(57, 0),
                new FarmerSprite.AnimationFrame(57, 500, false, false),
                new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 500, false, false, player => { onCast(); player.CanMove = true; })
            });
            float drawLayer = Math.Max(0f, (float)(Game1.player.StandingPixel.Y + 3) / 10000f);
            float drawLayer2 = Math.Max(0f, (float)(Game1.player.StandingPixel.Y - 3) / 10000f);
            Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites/Cursors_1_6", new Rectangle(304, 397, 11, 11), 30, 11, 0, Game1.player.Position + new Vector2(8, Game1.tileSize * -1.8f), false, false, drawLayer, 0, spellColor, Game1.pixelZoom, 0, 0, 0)
            {
                light = true,
                lightRadius = 0.5f,
                lightcolor = new Color(255 - spellColor.R, 255 - spellColor.G, 255 - spellColor.B)
            });
            Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(Instance.Helper.ModContent.GetInternalAssetName("assets/spellcircle.png").BaseName, new Rectangle(0, 0, 48, 48), 1000, 1, 0, Game1.player.Position + new Vector2(-96 / 4 + 4, -96 / 4), false, false, drawLayer2, 0, spellColor * 0.75f, 2, 0, 0, 0)
            {
                alpha = 0,
                alphaFade = -0.035f * 3,
                alphaFadeFade = -0.002f * 4,
                light = true,
                lightRadius = 0.5f,
                lightcolor = new Color(255 - spellColor.R, 255 - spellColor.G, 255 - spellColor.B)
            });
            for (int i = 0; i < 4 * 5; ++i)
            {
                float drawLayer3 = Math.Max(0f, (float)(Game1.player.StandingPixel.Y + 3) / 10000f);
                Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(Instance.Helper.ModContent.GetInternalAssetName("assets/particle.png").BaseName, new Rectangle(0, 0, 5, 5), 1000, 1, 0, Game1.player.Position + new Vector2(-96 / 4 + 4, -96 / 4) + new Vector2(Game1.random.Next(80), Game1.random.Next(64)), false, false, drawLayer3, 0, spellColor * 0.75f, 3, 0, 0, 0)
                {
                    motion = new Vector2(0, -2),
                    alphaFade = 0.05f,
                    xPeriodic = true,
                    xPeriodicLoopTime = 375,
                    xPeriodicRange = 8,
                    delayBeforeAnimationStart = i / 5 * 100,
                });
            }
        }

        public void Entry()
        {
            SpellCircle = Helper.ModContent.Load<Texture2D>("assets/spellcircle.png");

            //RegisterSpells();
        }

        private void RegisterSpells()
        {
            Ability.Abilities.Add("spell_haste", new Ability("spell_haste")
            {
                Name = I18n.Witchcraft_Spell_Haste_Name,
                Description = I18n.Witchcraft_Spell_Haste_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 0,
                ManaCost = () => 3,
                KnownCondition = $"TRUE",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.LimeGreen, () => Spells.Haste());
                }
            });

            Ability.Abilities.Add("spell_reviveplant", new Ability("spell_reviveplant")
            {
                Name = I18n.Witchcraft_Spell_RevivePlant_Name,
                Description = I18n.Witchcraft_Spell_RevivePlant_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 13,
                ManaCost = () => 10,
                KnownCondition = $"TRUE",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Magenta, () => Spells.RevivePlant());
                }
            });

            Ability.Abilities.Add("spell_pocketchest", new Ability("spell_pocketchest")
            {
                Name = I18n.Witchcraft_Spell_PocketChest_Name,
                Description = I18n.Witchcraft_Spell_PocketChest_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 18,
                ManaCost = () => 0,
                KnownCondition = $"TRUE",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Aqua, () => Spells.PocketChest());
                }
            });
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.OnStoneDestroyed))]
    public static class DropEarthEssencePatch2
    {
        public static void Postfix(GameLocation __instance, int x, int y, Farmer who)
        {
            long farmerId = who?.UniqueMultiplayerID ?? 0;
            if (who != null && Game1.random.NextDouble() < 0.15)
                Game1.createObjectDebris("(O)KCC.SnS_EarthEssence", x, y, farmerId, __instance);
        }
    }

    [HarmonyPatch(typeof(FishingRod), "doneFishing")]
    public static class DropWaterEssencePatch
    {
        public static void Postfix(Farmer who, bool consumeBaitAndTackle)
        {
            long farmerId = who?.UniqueMultiplayerID ?? 0;
            if (Game1.random.NextDouble() < .35)
                Game1.createObjectDebris("(O)KCC.SnS_WaterEssence", (int)who.Tile.X, (int)who.Tile.Y, farmerId, who.currentLocation);
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    public static class DropVariousMonsterEssencesPatch
    {
        public static void Postfix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
        {
            if (monster.isGlider.Value && Game1.random.NextDouble() < 25)
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)KCC.SnS_AirEssence"), new(x, y), 1, __instance);
            }
            /*
            if (__instance is MineShaft ms)
            {
                if (ms.mineLevel >= 80 && ms.mineLevel < 120 && Game1.random.NextDouble() < 0.04 * (who.professions.Contains(ArcanaSkill.MoreEssencesProfession.GetVanillaId()) ? 2 : 1))
                {
                    Game1.createItemDebris(ItemRegistry.Create("(O)KCC.SnS_FireEssence"), new(x, y), Game1.random.Next(4), __instance);
                }
            }
            */
            if (__instance is VolcanoDungeon vd && Game1.random.NextDouble() < 0.25)
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)KCC.SnS_FireEssence"), new(x, y), 1, __instance);
            }
        }
    }
}
