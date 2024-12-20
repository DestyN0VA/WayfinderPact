using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using SwordAndSorcerySMAPI;
using SpaceCore;
using FarmerExtData = SwordAndSorcerySMAPI.FarmerExtData;
using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.Objects;
using SpaceCore.Guidebooks;

namespace CircleOfThornsSMAPI
{
    public class ModCoT
    {
        public static ModCoT Instance;

        public static ConditionalWeakTable<Farmer, FarmerExtData> farmerData = new();
        public static Configuration Config { get; set; }

        public static Texture2D[][] formTexs;

        public static Texture2D huckleberryTex;

        public static Texture2D walletItemTex;

        public static IJsonAssetsApi ja;

        public const string ShapeshiftingEventId = "SnS.Ch2.Hector.16";
        public const string DropEssencesEventId = "SnS.Ch2.Hector.12";
        public const string WalletItemEventID = "SnS.Ch2.Hector.19";


        internal static DruidicsSkill Skill;

        public IMonitor Monitor;
        public IManifest ModManifest;
        public IModHelper Helper;
        public ModCoT(IMonitor monitor, IManifest manifest, IModHelper helper)
        {
            Instance = this;
            Monitor = monitor;
            ModManifest = manifest;
            Helper = helper;
        }

        private static void PostTransform()
        {
            Game1.player.GetFarmerExtData().noMovementTimer = 0;
            for (int i = 0; i < 8; ++i)
            {
                Vector2 diff = new Vector2(Game1.random.Next(96) - 48, Game1.random.Next(96) - 48);
                Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, Game1.player.getStandingPosition() - new Vector2(32, 48) + diff, flicker: false, flipped: false));
            }

            Game1.player.currentLocation.playSound("coldSpell", Game1.player.Position);
        }

        public void Entry()
        {
            //I18n.Init(helper.Translation);

            Config = Helper.ReadConfig<Configuration>();
            formTexs = new Texture2D[3][]
                {
                    new Texture2D[] { Helper.ModContent.Load<Texture2D>("assets/doe.png"), Helper.ModContent.Load<Texture2D>("assets/doeeyes.png") },
                    new Texture2D[] { Helper.ModContent.Load<Texture2D>("assets/buck.png"), Helper.ModContent.Load<Texture2D>("assets/buckeyes.png") },
                    new Texture2D[] { Helper.ModContent.Load<Texture2D>("assets/wolf.png"), Helper.ModContent.Load<Texture2D>("assets/wolfeyes.png") }
                };
            huckleberryTex = Helper.ModContent.Load<Texture2D>("assets/huckleberry.png");
            walletItemTex = Helper.ModContent.Load<Texture2D>("assets/wallet-item.png");

            GameStateQuery.Register("PLAYER_HAS_WOLF_FORM", (args, ctx) =>
            {
            return GameStateQuery.Helpers.WithPlayer(ctx.Player, args[1], (f) => f.HasCustomProfession(DruidicsSkill.ProfessionShapeshiftWolf));
            });

            Ability.Abilities.Add("shapeshift_doe", new Ability("shapeshift_doe")
            {
                Name = I18n.Ability_Shapeshift_Name_Doe,
                Description = I18n.Ability_Shapeshift_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 1,
                ManaCost = () => Game1.player.GetFarmerExtData().transformed.Value ? 0 : 5,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.DRUIDICS_LEVEL Current 1",
                UnlockHint = I18n.Ability_Shapeshift_UnlockHint,
                Function = () =>
                {
                    var ext = Game1.player.GetFarmerExtData();
                    ext.form.Value = 0;
                    ext.transformed.Value = !ext.transformed.Value;
                    PostTransform();
                }
            });
            Ability.Abilities.Add("shapeshift_buck", new Ability("shapeshift_buck")
            {
                Name = I18n.Ability_Shapeshift_Name_Buck,
                Description = I18n.Ability_Shapeshift_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 1,
                ManaCost = () => Game1.player.GetFarmerExtData().transformed.Value ? 0 : 5,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.DRUIDICS_LEVEL Current 1",
                UnlockHint = I18n.Ability_Shapeshift_UnlockHint,
                Function = () =>
                {
                    var ext = Game1.player.GetFarmerExtData();
                    ext.form.Value = 1;
                    ext.transformed.Value = !ext.transformed.Value;
                    PostTransform();
                }
            });
            Ability.Abilities.Add("shapeshift_wolf", new Ability("shapeshift_wolf")
            {
                Name = I18n.Ability_Shapeshift_Name_Wolf,
                Description = I18n.Ability_Shapeshift_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 1,
                ManaCost = () => Game1.player.GetFarmerExtData().transformed.Value ? 0 : 5,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.DRUIDICS_LEVEL Current 1, PLAYER_HAS_WOLF_FORM Current",
                HiddenIfLocked = true,
                Function = () =>
                {
                    var ext = Game1.player.GetFarmerExtData();
                    ext.form.Value = 2;
                    ext.transformed.Value = !ext.transformed.Value;
                    PostTransform();
                }
            });

            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;

            Skill = new DruidicsSkill();
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            string[][] recipes =
                new string[][]
                {
                    null,
                    new string[] { "Ancient Amaranth Seeds", "Ancient Epiphytic Fern Seeds" },
                    new string[] { "Ancient Glowing Polypore Mushroom Spores" },
                    new string[] { "Ancient Wild Fairy Rose Seeds" },
                    new string[] { "Ancient Elderberry Seeds" },
                    null,
                    new string[] { "Ancient Bottle Gourd Seeds", "DN.SnS_lavaeelandstirfriedancientbottlegourd" },
                    new string[] { "Ancient Giant Apple Berry Seeds", "DN.SnS_mushroomsredsauce" },
                    new string[] { "Ancient Azure Detura", "DN.SnS_ferngreensandpineapple" },
                    new string[] { "Ancient Glowing Huckleberry Seeds", "DN.SnS_ancienthuckleberryicecream" },
                    null,
                };
            for (int level = 1; level <= Game1.player.GetCustomSkillLevel(Skill); ++level)
            {
                if (recipes[level] != null)
                {
                    Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                    if (recipes[level].Length == 2)
                    {
                        if (level == 1)
                        {
                            Game1.player.craftingRecipes.TryAdd(recipes[level][1], 0);
                        }
                        else
                        {
                            Game1.player.cookingRecipes.TryAdd(recipes[level][1], 0);
                        }
                    }
                }
            }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var sc = Helper.ModRegistry.GetApi<SpaceShared.APIs.ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterCustomProperty(typeof(Farmer), "shapeshiftFormId", typeof(bool), AccessTools.DeclaredMethod(typeof(FarmerExtData), nameof(FarmerExtData.FormGetter)), AccessTools.DeclaredMethod(typeof(FarmerExtData), nameof(FarmerExtData.FormSetter)));
            sc.RegisterCustomProperty(typeof(Farmer), "druidicsExpRemainder", typeof(float), AccessTools.DeclaredMethod(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderGetter)), AccessTools.DeclaredMethod(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderSetter)));
            Skills.RegisterSkill(Skill);
        }

        //private double shapeshiftPressedTimer = 0;
        private int regenTimer = 0;
        private int transformTimer = 0;
        private void GameLoop_UpdateTicking(object sender, StardewModdingAPI.Events.UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady|| Game1.player.currentLocation != null && Game1.player.currentLocation.currentEvent != null && !Game1.player.currentLocation.currentEvent.isFestival)
                return;

            var data = Game1.player.GetFarmerExtData();

            if (data.transformed.Value)
            {
                transformTimer += (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
                if ( transformTimer >= 3000 )
                {
                    transformTimer -= 3000;
                    if (Game1.player.CurrentTool?.QualifiedItemId != "(W)DN.SnS_DruidShield" && Game1.player.GetOffhand()?.QualifiedItemId != "(W)DN.SnS_DruidShield")
                    {
                        var ext = SwordAndSorcerySMAPI.Extensions.GetFarmerExtData(Game1.player);
                        ext.mana.Value = Math.Max(ext.mana.Value - 1, 0);
                        if (ext.mana.Value <= 0)
                        {
                            data.transformed.Value = false;
                            PostTransform();
                            return;
                        }
                    }
                }

                var b = Game1.player.buffs.AppliedBuffs.FirstOrDefault(pair => pair.Key == "shapeshifted").Value;
                if (b == null)
                {
                    b = new Buff(
                        "shapeshifted",
                        duration: 250,
                        effects: new()
                        {
                            Speed = { 0.5f + Game1.player.GetCustomBuffedSkillLevel( ModCoT.Skill ) * 0.05f },
                            ForagingLevel = { 1 },
                        },
                        displayName: I18n.Shapeshifted(),
                        iconTexture: ModCoT.Skill.Icon,
                        iconSheetIndex: 0);
                    if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionShapeshiftWolf))
                    {
                        b.effects.AttackMultiplier.Value = 1.10f;
                        b.effects.Defense.Value = 3;
                    }
                    Game1.player.applyBuff(b);
                }
                else
                {
                    b.millisecondsDuration = 250;
                }

                if (data.isResting)
                {
                    regenTimer += Game1.currentGameTime.ElapsedGameTime.Milliseconds * (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionShapeshiftStag) ? 2 : 1);
                    int timerCap = 200 - Game1.player.GetCustomBuffedSkillLevel(ModCoT.Skill) * 10;
                    if (regenTimer >= timerCap)
                    {
                        regenTimer -= timerCap;
                        if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionShapeshift) && Game1.player.Stamina < Game1.player.MaxStamina)
                            ++Game1.player.Stamina;
                        if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionShapeshiftStag) && Game1.player.health < Game1.player.maxHealth)
                            ++Game1.player.health;
                    }
                }
            }
        }
    }

    // Fruit tree stuff
    [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.IsInSeasonHere))]
    public static class FruitTreeHuckleberrySeasonPatch
    {
        public static bool Prefix(FruitTree __instance, ref bool __result)
        {
            if (__instance.treeId.Value != "DN.SnS_ancientglowinghuckleberry.seed")
                return true;

            var season = Game1.GetSeasonForLocation(__instance.Location);
            __result = season is Season.Summer or Season.Fall;
            __result = __result || __instance.IgnoresSeasonsHere();

            return false;
        }
    }

    [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.draw))]
    public static class FruitTreeDrawHuckleberryPatch
    {
        public static bool Prefix(FruitTree __instance, SpriteBatch spriteBatch, NetBool ___falling, float ___shakeTimer, float ___shakeRotation, List<Leaf> ___leaves, float ___alpha )
        {
            if (__instance.treeId.Value != "DN.SnS_ancientglowinghuckleberry.seed")
                return true;

            if ((bool)__instance.GreenHouseTileTree)
            {
                spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.Tile.X * 64f, __instance.Tile.Y * 64f)), new Rectangle(669, 1957, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
            }
            if ((int)__instance.growthStage.Value < 4)
            {
                /*
                Vector2 positionOffset = new Vector2((float)Math.Max(-8.0, Math.Min(64.0, Math.Sin((double)(__instance.Tile.X * 200f) / (Math.PI * 2.0)) * -16.0)), (float)Math.Max(-8.0, Math.Min(64.0, Math.Sin((double)(__instance.Tile.X * 200f) / (Math.PI * 2.0)) * -16.0))) / 2f;
                Rectangle sourceRect = Rectangle.Empty;
                sourceRect = (int)__instance.growthStage switch
                {
                    0 => new Rectangle(0, (int)__instance.GetSpriteRowNumber() * 5 * 16, 48, 80),
                    1 => new Rectangle(48, (int)__instance.GetSpriteRowNumber() * 5 * 16, 48, 80),
                    2 => new Rectangle(96, (int)__instance.GetSpriteRowNumber() * 5 * 16, 48, 80),
                    _ => new Rectangle(144, (int)__instance.GetSpriteRowNumber() * 5 * 16, 48, 80),
                };
                spriteBatch.Draw(__instance.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.Tile.X * 64f + 32f + positionOffset.X, __instance.Tile.Y * 64f - (float)sourceRect.Height + 128f + positionOffset.Y)), sourceRect, Color.White, ___shakeRotation, new Vector2(24f, 80f), 4f, __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)__instance.getBoundingBox().Bottom / 10000f - __instance.Tile.X / 1000000f);
                */
                Texture2D tex = __instance.texture;
                Rectangle rect = new(Math.Min(0, 3) * 48, 0, 48, 80);
                spriteBatch.Draw(tex, Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.Tile.X * 64f + 32f, __instance.Tile.Y * 64f + 64f)), rect, ((int)__instance.struckByLightningCountdown.Value > 0) ? (Color.Gray * ___alpha) : (Color.White * ___alpha), ___shakeRotation, new Vector2(24f, 80f), 4f, __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(__instance.getBoundingBox().Bottom - 16) / 10000f + 0.001f - __instance.Tile.X / 1000000f);
            }
            else
            {
                if (!__instance.stump.Value || (bool)___falling.Value)
                {
                    Texture2D tex = __instance.texture;
                    Rectangle rect = new(Math.Min(__instance.fruit.Count, 3) * 48, 0, 48, 80);

                    /*
                    if (!___falling)
                    {
                        spriteBatch.Draw(FruitTree.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)), new Rectangle((12 + (__instance.greenHouseTree ? 1 : Utility.getSeasonNumber(season)) * 3) * 16, (int)__instance.treeType * 5 * 16 + 64, 48, 16), ((int)__instance.struckByLightningCountdown > 0) ? (Color.Gray * ___alpha) : (Color.White * ___alpha), 0f, new Vector2(24f, 16f), 4f, __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1E-07f);
                    }
                    */
                    //spriteBatch.Draw(FruitTree.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)), new Rectangle((12 + (__instance.greenHouseTree ? 1 : Utility.getSeasonNumber(season)) * 3) * 16, (int)__instance.treeType * 5 * 16, 48, 64), ((int)__instance.struckByLightningCountdown > 0) ? (Color.Gray * ___alpha) : (Color.White * ___alpha), ___shakeRotation, new Vector2(24f, 80f), 4f, __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)__instance.getBoundingBox(tileLocation).Bottom / 10000f + 0.001f - tileLocation.X / 1000000f);
                    spriteBatch.Draw(tex, Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.Tile.X * 64f + 32f, __instance.Tile.Y * 64f + 64f)), rect, ((int)__instance.struckByLightningCountdown.Value > 0) ? (Color.Gray * ___alpha) : (Color.White * ___alpha), ___shakeRotation, new Vector2(24f, 80f), 4f, __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(__instance.getBoundingBox().Bottom-16) / 10000f + 0.001f - __instance.Tile.X / 1000000f);
                }
                else if ((float)__instance.health.Value >= 1f || (!___falling.Value && (float)__instance.health.Value > -99f))
                {
                    spriteBatch.Draw(__instance.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.Tile.X * 64f + 32f + ((___shakeTimer > 0f) ? ((float)Math.Sin(Math.PI * 2.0 / (double)___shakeTimer) * 2f) : 0f), __instance.Tile.Y * 64f + 64f)), new Rectangle(384, (int)__instance.GetSpriteRowNumber() * 5 * 16 + 48, 48, 32), ((int)__instance.struckByLightningCountdown.Value > 0) ? (Color.Gray * ___alpha) : (Color.White * ___alpha), 0f, new Vector2(24f, 32f), 4f, __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, ((bool)__instance.stump.Value && !___falling.Value) ? ((float)__instance.getBoundingBox().Bottom / 10000f) : ((float)__instance.getBoundingBox().Bottom / 10000f - 0.001f - __instance.Tile.X / 1000000f));
                }
                /*
                for (int i = 0; i < (int)__instance.fruitsOnTree; i++)
                {
                    switch (i)
                    {
                        case 0:
                            spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f - 64f + tileLocation.X * 200f % 64f / 2f, tileLocation.Y * 64f - 192f - tileLocation.X % 64f / 3f)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ((int)__instance.struckByLightningCountdown > 0) ? 382 : ((int)__instance.indexOfFruit), 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)__instance.getBoundingBox(tileLocation).Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
                            break;
                        case 1:
                            spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f - 256f + tileLocation.X * 232f % 64f / 3f)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ((int)__instance.struckByLightningCountdown > 0) ? 382 : ((int)__instance.indexOfFruit), 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)__instance.getBoundingBox(tileLocation).Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
                            break;
                        case 2:
                            spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + tileLocation.X * 200f % 64f / 3f, tileLocation.Y * 64f - 160f + tileLocation.X * 200f % 64f / 3f)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ((int)__instance.struckByLightningCountdown > 0) ? 382 : ((int)__instance.indexOfFruit), 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, (float)__instance.getBoundingBox(tileLocation).Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
                            break;
                    }
                }
                */
            }
            foreach (Leaf j in ___leaves)
            {
                spriteBatch.Draw(__instance.texture, Game1.GlobalToLocal(Game1.viewport, j.position), new Rectangle((24 + Game1.seasonIndex) * 16, (int)__instance.GetSpriteRowNumber() * 5 * 16, 8, 8), Color.White, j.rotation, Vector2.Zero, 4f, SpriteEffects.None, (float)__instance.getBoundingBox().Bottom / 10000f + 0.01f);
            }

            return false;
        }
    }
    
    // Shapeshift stuff
    [HarmonyPatch(typeof(Farmer), "farmerInit")]
    public static class FarmerInitPatch
    {
        public static void Postfix(Farmer __instance)
        {
            __instance.NetFields.AddField(__instance.GetFarmerExtData().form);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().transformed);
        }
    }

    [HarmonyPatch(typeof(Farmer), "updateCommon")]
    public static class FarmerUpdatePatch
    {
        public static void Postfix(Farmer __instance, GameTime time)
        {
            var data = __instance.GetFarmerExtData();

            if (__instance.movementDirections.Count > 0)
                data.noMovementTimer = 0;
            else
                data.noMovementTimer += time.ElapsedGameTime.TotalSeconds;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.gainExperience))]
    public static class FarmerExpInterceptPatch
    {
        public static void Postfix(Farmer __instance, int which, int howMuch)
        {
            if (!__instance.eventsSeen.Contains("SnS.Ch2.Hector.16"))
                return;
            if (which != Farmer.farmingSkill && which != Farmer.foragingSkill)
                return;

            var data = __instance.GetFarmerExtData();
            float exp = data.expRemainder.Value + howMuch / 2f;
            __instance.AddCustomSkillExperience(ModCoT.Skill, (int)MathF.Truncate( exp ));
            data.expRemainder.Value = exp - MathF.Truncate(exp);
        }
    }

    /*
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.GetBoundingBox))]
    public static class FarmerBoundingBoxPatch
    {
        public static void Postfix(Farmer __instance, ref Rectangle __result)
        {
            if (__instance.GetFarmerExtData().transformed.Value)
            {
                __result = new(__result.X - 16, __result.Y - 32, __result.Width + 32, __result.Height + 32);
            }
        }
    }
    */

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.useTool))]
    public static class FarmerUseToolPatch1
    {
        public static bool Prefix(Farmer who)
        {
            if (who.GetFarmerExtData().transformed.Value)
            {
                if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionShapeshiftWolf))
                    return true;
                return false;
            }
            return true;

        }
    }
    [HarmonyPatch(typeof(Farmer), "performBeginUsingTool")]
    public static class FarmerUseToolPatch2
    {
        public static bool Prefix(Farmer __instance)
        {
            if (__instance.GetFarmerExtData().transformed.Value)
            {
                if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionShapeshiftWolf))
                    return true;
                return false;
            }
            return true;

        }
    }

    // Essence drop stuff
    [HarmonyPatch(typeof(Crop), nameof(Crop.harvest))]
    public static class CropHarvestDropEssencesPatch
    {
        public static void Prefix(Crop __instance, int xTile, int yTile, HoeDirt soil, out bool __state, JunimoHarvester junimoHarvester = null, bool isForcedScytheHarvest = false)
        {
            __state = false;
            if (__instance.dead.Value)
            {
                return;
            }
            bool success = false;
            if (__instance.forageCrop.Value)
            {
                StardewValley.Object o = null;
                int experience = 3;
                if (__instance.whichForageCrop.Value == "2")
                {
                    return;
                }
                if (junimoHarvester != null)
                {
                    return;
                }
                if (isForcedScytheHarvest)
                {
                    return;
                }
                if (AddItemToInvBool(o))
                {
                    return;
                }
            }
            else if (__instance.currentPhase.Value >= __instance.phaseDays.Count - 1 && (!__instance.fullyGrown.Value || __instance.dayOfCurrentPhase.Value <= 0))
            {
                if (__instance.indexOfHarvest.Value == null)
                {
                    return;
                }
                CropData data = __instance.GetData();
                Item harvestedItem = __instance.programColored.Value ? new ColoredObject(__instance.indexOfHarvest.Value, 1, __instance.tintColor.Value)
                {
                    Quality = 0
                } : ItemRegistry.Create(__instance.indexOfHarvest.Value, 1, 0);
                HarvestMethod harvestMethod = data?.HarvestMethod ?? HarvestMethod.Grab;
                if (harvestMethod == HarvestMethod.Scythe || isForcedScytheHarvest)
                {
                    success = true;
                }
                else if (junimoHarvester != null || (harvestedItem != null && Game1.player.addItemToInventoryBool(harvestedItem.getOne())))
                {
                    success = true;
                }
            }
            __state = success;
        }

        public static bool AddItemToInvBool(Item item, bool makeActiveObject = false)
        {
            if (item == null)
            {
                return false;
            }

            if (Game1.player.IsLocalPlayer)
            {
                Item item2 = null;
                Game1.player.GetItemReceiveBehavior(item, out var needsInventorySpace, out var _);
                if (needsInventorySpace)
                {
                    item2 = Game1.player.addItemToInventory(item);
                }

                bool flag = item2?.Stack != item.Stack || item is SpecialItem;

                return flag;
            }

            return false;
        }

        public static void Postfix(int xTile, int yTile, JunimoHarvester junimoHarvester, bool __result, bool __state)
        {
            if (!Game1.player.eventsSeen.Contains(ModCoT.DropEssencesEventId))
                return;
            if (!__state)
                return;

            float mult = 0.1f / 8;
            mult += Game1.player.GetCustomSkillLevel(ModCoT.Skill) * 0.001f;
            if (Game1.player.hasOrWillReceiveMail("BrokenCircletPower"))
                mult += 0.01f;
            if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionAgricultureYggdrasil))
                mult += 0.01f;
            if (junimoHarvester == null && Game1.random.NextDouble() < 8 * mult)
            {
                Game1.createItemDebris(new StardewValley.Object("DN.SnS_druidicessence", 1), new Vector2(xTile, yTile) * Game1.tileSize, -1);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.makeHoeDirt))]
    public static class HoeDirtCreationDropEssencesPatch1
    {
        public static void Postfix(GameLocation __instance, Vector2 tileLocation)
        {
            if (!Game1.player.eventsSeen.Contains(ModCoT.DropEssencesEventId))
                return;
            if (!__instance.IsFarm)
                return;

            float mult = 0.1f / 4;
            mult += Game1.player.GetCustomSkillLevel(ModCoT.Skill) * 0.001f;
            if (Game1.player.hasOrWillReceiveMail("BrokenCircletPower"))
                mult += 0.01f;
            if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionAgricultureYggdrasil))
                mult += 0.01f;
            if (Game1.random.NextDouble() < 4 * mult)
            {
                Game1.createItemDebris(new StardewValley.Object("DN.SnS_druidicessence", 1), tileLocation * Game1.tileSize, -1);
            }
        }
    }

    [HarmonyPatch(typeof(HoeDirt), nameof(HoeDirt.performToolAction))]
    public static class HoeDirtCreationDropEssencesPatch2
    {
        public static void Prefix(HoeDirt __instance, Tool t)
        {
            if (!Game1.player.eventsSeen.Contains(ModCoT.DropEssencesEventId))
                return;
            if (!__instance.Location.IsFarm || t is not WateringCan || __instance.state.Value == HoeDirt.watered)
                return;

            float mult = 0.1f / 4;
            mult += Game1.player.GetCustomSkillLevel(ModCoT.Skill) * 0.001f;
            if (Game1.player.hasOrWillReceiveMail("BrokenCircletPower"))
                mult += 0.01f;
            if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionAgricultureYggdrasil))
                mult += 0.01f;
            if (Game1.random.NextDouble() < 4 * mult)
            {
                Game1.createItemDebris(new StardewValley.Object("DN.SnS_druidicessence", 1), __instance.Tile * Game1.tileSize, -1);
            }
        }
    }

    [HarmonyPatch(typeof(Grass), nameof(Grass.performToolAction))]
    public static class ScytheDropEssencesPatch
    {
        public static void Prefix(Grass __instance, Tool t)
        {
            if (!Game1.player.eventsSeen.Contains(ModCoT.DropEssencesEventId))
                return;
            if (!__instance.Location.IsFarm || t is not MeleeWeapon mw || !mw.isScythe())
                return;

            float mult = 0.1f / 2;
            mult += Game1.player.GetCustomSkillLevel(ModCoT.Skill) * 0.001f;
            if (Game1.player.hasOrWillReceiveMail("BrokenCircletPower"))
                mult += 0.01f;
            if (Game1.player.HasCustomProfession(DruidicsSkill.ProfessionAgricultureYggdrasil))
                mult += 0.01f;
            if (Game1.random.NextDouble() < 2 * mult)
            {
                Game1.createItemDebris(new StardewValley.Object("DN.SnS_druidicessence", 1), __instance.Tile * Game1.tileSize, -1);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.DayUpdate))]
    public static class GameLocationDayUpdateCropsWaterPatch
    {
        public static void Postfix(GameLocation __instance)
        {
            bool hasProfession = false;
            foreach (var player in Game1.getAllFarmers())
            {
                if (player.HasCustomProfession(DruidicsSkill.ProfessionAgricultureYggdrasil))
                {
                    hasProfession = true;
                    break;
                }
            }

            if (!hasProfession)
                return;

            foreach (var tf in __instance.terrainFeatures.Values)
            {
                if (tf is HoeDirt hd && hd.crop != null && !hd.crop.dead.Value)
                {
                    for (int ix = -1; ix <= 1; ix += 1)
                    {
                        for (int iy = -1; iy <= 1; iy += 1)
                        {
                            if (__instance.terrainFeatures.TryGetValue(hd.Tile + new Vector2(ix, iy), out TerrainFeature toWater))
                            {
                                if (toWater is HoeDirt twhd && twhd.state.Value == HoeDirt.dry)
                                    hd.state.Value = HoeDirt.watered;
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StardewValley.Object), "getPriceAfterMultipliers")]
    public static class ObjectPriceMultiplierForMidgardPatch
    {
        public static void Postfix(StardewValley.Object __instance, long specificPlayerID, ref float __result)
        {
            float saleMultiplier = 1f;
            foreach (Farmer player in Game1.getAllFarmers())
            {
                if (Game1.player.useSeparateWallets)
                {
                    if (specificPlayerID == -1)
                    {
                        if (player.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID || !player.isActive())
                        {
                            continue;
                        }
                    }
                    else if (player.UniqueMultiplayerID != specificPlayerID)
                    {
                        continue;
                    }
                }
                else if (!player.isActive())
                {
                    continue;
                }
                float multiplier = 1f;
                if (player.HasCustomProfession(DruidicsSkill.ProfessionAgricultureMidgard) && __instance.Category == -26)
                {
                    string[] ids = new string[]
                    {
                        "DN.SnS_ancientamaranth.object",
                        "DN.SnS_ancientepiphyticfern.object",
                        "DN.SnS_glowingpolyporemushrooms.object",
                        "DN.SnS_ancientwildfairyroses.object",
                        "DN.SnS_ancientelderberry.object",
                        "DN.SnS_ancientbottlegourd.object",
                        "DN.SnS_ancientgiantappleberry.object",
                        "DN.SnS_ancientazuredetura.object"
                    };
                    if (ids.Contains(__instance.preservedParentSheetIndex.Value))
                    {
                        multiplier *= 1.1f;
                    }
                }
                saleMultiplier = Math.Max(saleMultiplier, multiplier);
            }
            __result *= saleMultiplier;
        }
    }


    [HarmonyPatch(typeof(HoeDirt), nameof(HoeDirt.applySpeedIncreases))]
    public static class HoeDirtMidgardSpeedIncreasePatch
    {
        public static void Postfix(HoeDirt __instance, Farmer who)
        {
            if (!who.HasCustomProfession(DruidicsSkill.ProfessionAgricultureMidgard))
                return;

            if (__instance.crop == null)
            {
                return;
            }

            int totalDaysOfCropGrowth = 0;
            for (int j = 0; j < __instance.crop.phaseDays.Count - 1; j++)
            {
                totalDaysOfCropGrowth += __instance.crop.phaseDays[j];
            }
            float speedIncrease = 0.1f;
            int daysToRemove = (int)Math.Ceiling((float)totalDaysOfCropGrowth * speedIncrease);
            int tries = 0;
            while (daysToRemove > 0 && tries < 3)
            {
                for (int i = 0; i < __instance.crop.phaseDays.Count; i++)
                {
                    if ((i > 0 || __instance.crop.phaseDays[i] > 1) && __instance.crop.phaseDays[i] != 99999)
                    {
                        __instance.crop.phaseDays[i]--;
                        daysToRemove--;
                    }
                    if (daysToRemove <= 0)
                    {
                        break;
                    }
                }
                tries++;
            }
        }
    }
}
