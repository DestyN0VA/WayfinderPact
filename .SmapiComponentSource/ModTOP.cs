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

            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;


            Event.RegisterCommand("sns_essenceunlock", (Event @event, string[] args, EventContext context) =>
            {
                // This implementation is incredibly lazy
                ArgUtility.TryGetVector2(args, 1, out Vector2 center, out string error);
                center.Y -= 0.5f;

                List<TemporaryAnimatedSprite> tass = new();

                @event.aboveMapSprites ??= new();

                Color[] cols = [Color.White, Color.White, Color.White, Color.White, Color.White, Color.White, Color.White, Color.White];

                Rectangle[] srcRects =
                [
                    new Rectangle(80, 144, 16, 16),
                    new Rectangle(96, 144, 16, 16),
                    new Rectangle(112, 144, 16, 16),
                    new Rectangle(128, 144, 16, 16),
                    new Rectangle(80, 144, 16, 16),
                    new Rectangle(96, 144, 16, 16),
                    new Rectangle(112, 144, 16, 16),
                    new Rectangle(128, 144, 16, 16),
                ];

                int soFar = 0;
                void makeNote()
                {
                    TemporaryAnimatedSprite tas = new("SMAPI/DN.SnS/assets/Items & Crops/SnSObjects.png", srcRects[soFar], center * Game1.tileSize + new Vector2(0, -96), false, 0, cols[soFar])
                    {
                        layerDepth = 1,
                        scale = 4,
                    };
                    tass.Add(tas);
                    @event.aboveMapSprites.Add(tas);
                    Game1.playSound("coldSpell", soFar * 175);
                    ++soFar;
                }
                for (int i = 0; i < cols.Length; ++i)
                {
                    DelayedAction.functionAfterDelay(() =>
                    {
                        makeNote();
                    }, i * 429);
                }
                for (int i_ = 0; i_ < 8000; i_ += 16)
                {
                    int i = i_;
                    float getSpeed()
                    {
                        if (tass.Count < 7) return 100;
                        return Math.Min(100 + (i - 3000) / 16, 720);
                    }
                    float getLength()
                    {
                        if (tass.Count < 7 || i < 4000) return 96;
                        if (i <= 6000) return 96 + (i - 4000) / 16;
                        return (96 + 2000 / 16) - (i - 6000) / 4;
                    }

                    DelayedAction.functionAfterDelay(() =>
                    {
                        foreach (var tas in tass)
                        {
                            var p = tas.Position - center * Game1.tileSize;
                            float angle = MathF.Atan2(p.Y, p.X);
                            angle += getSpeed() / 180 * MathF.PI * (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                            tas.Position = center * Game1.tileSize + new Vector2(MathF.Cos(angle) * getLength(), MathF.Sin(angle) * getLength());

                            if (i >= 4000 && i <= 6000)
                                tas.scaleChange = 0.025f;
                            else if (i >= 6000 && i <= 8000)
                            {
                                tas.scaleChange = -0.1f;
                            }

                            if (tas.scale < 0 || getLength() < 0)
                            {
                                @event.aboveMapSprites.Remove(tas);
                            }
                        }
                    }, i);
                }

                @event.CurrentCommand++;
            });

            RegisterSpells();
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            Game1.player.GetFarmerExtData().mageArmor = false;
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

            Ability.Abilities.Add("spell_magearmor", new Ability("spell_magearmor")
            {
                Name = I18n.Witchcraft_Spell_MageArmor_Name,
                Description = I18n.Witchcraft_Spell_MageArmor_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 5,
                ManaCost = () => 5,
                KnownCondition = $"TRUE",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Orange, () => Spells.MageArmor());
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

            Ability.Abilities.Add("spell_mirrorimage", new Ability("spell_mirrorimage")
            {
                Name = I18n.Witchcraft_Spell_MirrorImage_Name,
                Description = I18n.Witchcraft_Spell_MirrorImage_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 14,
                ManaCost = () => 15,
                KnownCondition = $"TRUE",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Yellow, () => Spells.MirrorImage());
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
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;

            long farmerId = who?.UniqueMultiplayerID ?? 0;
            if (who != null && Game1.random.NextDouble() < 0.15)
                Game1.createObjectDebris("(O)DN.SnS_EarthEssence", x, y, farmerId, __instance);
        }
    }

    [HarmonyPatch(typeof(FishingRod), "doneFishing")]
    public static class DropWaterEssencePatch
    {
        public static void Postfix(Farmer who, bool consumeBaitAndTackle)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;

            long farmerId = who?.UniqueMultiplayerID ?? 0;
            if (Game1.random.NextDouble() < .35)
                Game1.createObjectDebris("(O)DN.SnS_WaterEssence", (int)who.Tile.X, (int)who.Tile.Y, farmerId, who.currentLocation);
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    public static class DropVariousMonsterEssencesPatch
    {
        public static void Postfix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;

            if (monster.isGlider.Value && Game1.random.NextDouble() < 25)
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_AirEssence"), new(x, y), 1, __instance);
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
                Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_FireEssence"), new(x, y), 1, __instance);
            }
        }
    }

    // NOTE: Transparency for mirror image rendering is in the Shadowstep patch (since they share the transparency variable)

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static class FarmerMirrorImageDamagePatch
    {
        public static bool Prefix(Farmer __instance, ref int damage, bool overrideParry, Monster damager)
        {
            var ext = Game1.player.GetFarmerExtData();
            if (__instance != Game1.player || overrideParry || !Game1.player.CanBeDamaged() ||
                ext.mirrorImages.Value <= 0 )
                return true;

            bool flag = (damager == null || !damager.isInvincible()) && (damager == null || (!(damager is GreenSlime) && !(damager is BigSlime)) || !__instance.isWearingRing("520"));
            if (!flag) return true;

            if (Game1.random.Next(ext.mirrorImages.Value + 1) != 0)
            {
                Vector2 spot = Game1.player.StandingPixel.ToVector2();
                float rad = (float)-Game1.currentGameTime.TotalGameTime.TotalSeconds / 3 * 2;
                /*
                switch (ext.mirrorImages.Value)
                {
                    case 1: spot -= new Vector2(0, Game1.tileSize); break;
                    case 2: spot += new Vector2(-Game1.tileSize, Game1.tileSize); break;
                    case 3: spot += new Vector2(Game1.tileSize, Game1.tileSize); break;
                }
                */
                rad += MathF.PI * 2 / 3 * (ext.mirrorImages.Value - 1);
                spot += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);

                ext.mirrorImages.Value -= 1;
                for (int i = 0; i < 8; ++i)
                {
                    Vector2 diff = new Vector2(Game1.random.Next(96) - 48, Game1.random.Next(96) - 48);
                    Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, spot - new Vector2(32, 48) + diff, flicker: false, flipped: false));
                }
                __instance.playNearbySoundAll("coldSpell");

                damage = 0;
            }
            return true;
        }
    }
}
