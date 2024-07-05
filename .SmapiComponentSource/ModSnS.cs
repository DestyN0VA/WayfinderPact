using CircleOfThornsSMAPI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using NeverEndingAdventure;
using NeverEndingAdventure.Utils;
using SpaceCore;
using SpaceCore.VanillaAssetExpansion;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.GameData.SpecialOrders;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Weapons;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using xTile.Tiles;

namespace SwordAndSorcerySMAPI
{
    public class FarmerExtData
    {
        public readonly NetBool hasTakenLoreWeapon = new(false);
        public static void SetHasTakenLoreWeapon(Farmer farmer, NetBool val)
        {
        }

        public static NetBool HasTakenLoreWeapon(Farmer farmer)
        {
            return farmer.GetFarmerExtData().hasTakenLoreWeapon;
        }

        public readonly NetBool inShadows = new(false);

        public readonly NetArray<string, NetString> adventureBar = new(8 * 2);
        public static void SetAdventureBar(Farmer farmer, NetArray<string, NetString> val)
        {
        }

        public static NetArray<string, NetString> GetAdventureBar(Farmer farmer)
        {
            return farmer.GetFarmerExtData().adventureBar;
        }

        public readonly NetInt mana = new();
        public readonly NetInt maxMana = new();
        public static void SetMaxMana(Farmer farmer, NetInt val)
        {
        }

        public static NetInt GetMaxMana(Farmer farmer)
        {
            return farmer.GetFarmerExtData().maxMana;
        }

        public readonly NetFloat expRemainderRogue = new(0);
        public static NetFloat ExpRemainderRogueGetter(Farmer farmer)
        {
            return farmer.GetFarmerExtData().expRemainderRogue;
        }
        public static void ExpRemainderRogueSetter(Farmer farmer, NetFloat val)
        {
        }

        public readonly NetInt armorUsed = new(0);
        public readonly NetInt mirrorImages = new(0);
        public int currRenderingMirror = 0;
        public bool mageArmor = false;
        public readonly NetBool isGhost = new(false);
        public readonly NetVector2 ghostOrigPosition = new();
    }

    [HarmonyPatch(typeof(Farmer), "initNetFields")]
    public static class AddLoreWeaponField
    {
        public static void Postfix(Farmer __instance)
        {
            __instance.NetFields.AddField(__instance.GetFarmerExtData().hasTakenLoreWeapon);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().inShadows);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().adventureBar);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().mana);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().maxMana);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().expRemainderRogue);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().armorUsed);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().mirrorImages);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().isGhost);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().ghostOrigPosition);
        }
    }

    public static partial class Extensions
    {
        public static FarmerExtData GetFarmerExtData(this Farmer instance)
        {
            return ModSnS.farmerData.GetOrCreateValue(instance);
        }

        public static bool IsArmorItem(this Item item)
        {
            return (item.GetArmorAmount(includeMageArmor: false) ?? -1) > 0;
        }

        public static int? GetArmorAmount(this Item item, bool includeMageArmor = true)
        {
            int mageArmor = Game1.player.GetFarmerExtData().mageArmor ? 50 : 0;
            if (!includeMageArmor)
                mageArmor = 0;

            if (item != null && ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).RawData is ObjectData data &&
                ( data.CustomFields?.TryGetValue("ArmorValue", out string valStr) ?? false ) && int.TryParse(valStr, out int val))
            {
                return (int)( val * ( Game1.player.HasCustomProfession( RogueSkill.ProfessionArmorCap ) ? 1.5f : 1)) + mageArmor;
            }
            return mageArmor == 0 ? null : mageArmor;
        }
    }

    public class State
    {
        public ThrownShield MyThrown { get; set; }
        public float ThrowCooldown { get; set; } = 0;

        public float BlockCooldown { get; set; } = 0;
        public bool CanRepairArmor { get; set; } = true;
        public bool HasCraftedFree { get; set; } = false;

        public Monster LastAttacked { get; set; } = null;
        public int LastAttackedCounter { get; set; } = 0;

        public Point LastWalkedTile { get; set; } = new();
        public Dictionary<string, string> TeleportCircles { get; set; } = new();
    }

    public class Configuration
    {
        public KeybindList ConfigureAdventureBar = new(SButton.U);
        public KeybindList ToggleAdventureBar = new(new Keybind(SButton.LeftControl, SButton.U));

        public KeybindList AbilityBar1Slot1 = new(new Keybind(SButton.LeftControl, SButton.D1));
        public KeybindList AbilityBar1Slot2 = new(new Keybind(SButton.LeftControl, SButton.D2));
        public KeybindList AbilityBar1Slot3 = new(new Keybind(SButton.LeftControl, SButton.D3));
        public KeybindList AbilityBar1Slot4 = new(new Keybind(SButton.LeftControl, SButton.D4));
        public KeybindList AbilityBar1Slot5 = new(new Keybind(SButton.LeftControl, SButton.D5));
        public KeybindList AbilityBar1Slot6 = new(new Keybind(SButton.LeftControl, SButton.D6));
        public KeybindList AbilityBar1Slot7 = new(new Keybind(SButton.LeftControl, SButton.D7));
        public KeybindList AbilityBar1Slot8 = new(new Keybind(SButton.LeftControl, SButton.D8));
        public KeybindList AbilityBar2Slot1 = new(new Keybind(SButton.LeftShift, SButton.D1));
        public KeybindList AbilityBar2Slot2 = new(new Keybind(SButton.LeftShift, SButton.D2));
        public KeybindList AbilityBar2Slot3 = new(new Keybind(SButton.LeftShift, SButton.D3));
        public KeybindList AbilityBar2Slot4 = new(new Keybind(SButton.LeftShift, SButton.D4));
        public KeybindList AbilityBar2Slot5 = new(new Keybind(SButton.LeftShift, SButton.D5));
        public KeybindList AbilityBar2Slot6 = new(new Keybind(SButton.LeftShift, SButton.D6));
        public KeybindList AbilityBar2Slot7 = new(new Keybind(SButton.LeftShift, SButton.D7));
        public KeybindList AbilityBar2Slot8 = new(new Keybind(SButton.LeftShift, SButton.D8));

        public KeybindList ThrowShieldKey = new(SButton.R);
    }

    public partial class ModSnS : StardewModdingAPI.Mod
    {
        public static ModSnS instance;
        public static Configuration Config { get; set; }
        private static PerScreen<State> _state = new(() => new State());
        public static State State => _state.Value;

        public static Texture2D ArmorSlotBackground;
        public static Texture2D ShieldSlotBackground;
        public static Texture2D ShieldItemTexture;
        public static Texture2D SwordOverlay;

        public static ConditionalWeakTable<Farmer, FarmerExtData> farmerData = new();

        public static RogueSkill RogueSkill;

        public const string ShadowstepEventReq = "SnS.Ch1.Mateo.18";

        public override void Entry(IModHelper helper)
        {
            instance = this;
            I18n.Init(Helper.Translation);
            Config = Helper.ReadConfig<Configuration>();

            Event.RegisterCommand("sns_rogueunlock", (Event @event, string[] args, EventContext context) =>
            {
                // This implementation is incredibly lazy
                ArgUtility.TryGetVector2(args, 1, out Vector2 center, out string error);
                center.Y -= 0.5f;

                List<TemporaryAnimatedSprite> tass = new();

                @event.aboveMapSprites ??= new();

                Color[] cols = [Color.White, Color.White, Color.White, Color.White, Color.White, Color.White, Color.White, Color.White];

                Rectangle[] srcRects =
                [
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.sapphireIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.amethystClusterIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.rubyIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.diamondIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.emeraldIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.aquamarineIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, StardewValley.Object.topazIndex ),
                    Game1.getSquareSourceRectForNonStandardTileSheet( Game1.objectSpriteSheet, 16, 16, 80 ),
                ];

                int soFar = 0;
                void makeNote()
                {
                    TemporaryAnimatedSprite tas = new(Game1.objectSpriteSheetName, srcRects[soFar], center * Game1.tileSize + new Vector2(0, -96), false, 0, cols[soFar])
                    {
                        layerDepth = 1,
                        scale = 4,
                    };
                    tass.Add(tas);
                    @event.aboveMapSprites.Add(tas);
                    Game1.playSound("miniharp_note", soFar * 175);
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

            ArmorSlotBackground = Helper.ModContent.Load<Texture2D>("assets/armor-bg.png");
            string[] recolors =
            [
                "daisyniko.earthyinterface",
                "shinchan.cppurpleinterface",
                "enteis.woodeninterfeis",
                "thefrenchdodo.sakurainterfaceredux",
                "nom0ri.vintageuifix",
                "Sqbr.StarryBlueUI",
            ];
            foreach (var recolor in recolors)
            {
                if (Helper.ModRegistry.IsLoaded( recolor ) && File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "armor-bg", recolor + ".png")))
                {
                    ArmorSlotBackground = Helper.ModContent.Load<Texture2D>($"assets/armor-bg/{recolor}.png");
                }
            }
            ShieldSlotBackground = Helper.ModContent.Load<Texture2D>("assets/shield-bg.png");
            ShieldItemTexture = Helper.ModContent.Load<Texture2D>("assets/shield-item.png");
            SwordOverlay = Helper.ModContent.Load<Texture2D>("assets/SwordOverlay.png");

            Helper.Events.Content.AssetRequested += Content_AssetRequested;
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged;
            Helper.Events.GameLoop.SaveCreated += GameLoop_SaveCreated; ;
            Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.Display.RenderedHud += Display_RenderedHud;
            Helper.Events.Display.RenderedWorld += Display_RenderedWorld;
            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;

            GameLocation.RegisterTileAction("SwordAndSorceryOrderBoard", (loc, args, farmer, point) =>
            {
                Game1.player.team.ordersBoardMutex.RequestLock(() =>
                {
                    Game1.activeClickableMenu = new SpecialOrdersBoard("SwordSorcery")
                    {
                        behaviorBeforeCleanup = (menu) => { Game1.player.team.ordersBoardMutex.ReleaseLock(); }
                    };
                });
                return true;
            });


            GameStateQuery.Register("PLAYER_HAS_SHADOWSTEP", (args, ctx) =>
            {
                return GameStateQuery.Helpers.WithPlayer(ctx.Player, args[1], (f) => f.HasCustomProfession(RogueSkill.ProfessionShadowStep));
            });

            Ability.Abilities.Add("shadowstep", new Ability("shadowstep")
            {
                Name = I18n.Ability_Shadowstep_Name,
                Description = I18n.Ability_Shadowstep_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 0,
                KnownCondition = $"PLAYER_HAS_SHADOWSTEP Current",
                HiddenIfLocked = true,
                ManaCost = () => 15,
                Function = () =>
                {
                    Game1.player.GetFarmerExtData().inShadows.Value = true;
                }
            });

            Helper.ConsoleCommands.Add("sns_setmaxaether", "...", (cmd, args) => Game1.player.GetFarmerExtData().maxMana.Value = int.Parse(args[0]));
            Helper.ConsoleCommands.Add("sns_refillaether", "...", (cmd, args) => Game1.player.GetFarmerExtData().mana.Value = Game1.player.GetFarmerExtData().maxMana.Value);
            Helper.ConsoleCommands.Add("sns_repairarmor", "...", (cmd, args) => Game1.player.GetFarmerExtData().armorUsed.Value = 0);
            Helper.ConsoleCommands.Add("sns_finishorders", "...", (cmd, args) =>
            {
                string[] valid =
                [
                    "Mateo.SpecialOrders.BuildGuild",
                    "CAGQuest.UntimedSpecialOrder.Pentacle1",
                    "CAGQuest.UntimedSpecialOrder.Pentacle2",
                    "CAGQuest.UntimedSpecialOrder.Pentacle3",
                    "CAGQuest.UntimedSpecialOrder.Pentacle4",
                    "CAGQuest.UntimedSpecialOrder.River1",
                    "CAGQuest.UntimedSpecialOrder.River2",
                    "CAGQuest.UntimedSpecialOrder.LionsMane",
                ];

                foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                {
                    if (!valid.Contains(specialOrder.questKey.Value))
                        continue;

                    foreach (OrderObjective objective in specialOrder.objectives)
                    {
                        objective.SetCount(objective.maxCount.Value);
                        objective.CheckCompletion(false);
                        Helper.Reflection.GetField<bool>(objective, "_complete").SetValue(true);
                    }

                    specialOrder.CheckCompletion();
                }
            });

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll( Assembly.GetExecutingAssembly() );

            new ModCoT(Monitor, ModManifest, Helper).Entry();
            new ModNEA(Monitor, ModManifest, Helper).Entry(harmony);
            new ModUP(Monitor, ModManifest, Helper).Entry();
            new ModTOP(Monitor, ModManifest, Helper).Entry();

            InitArsenal();
        }

        private void GameLoop_SaveCreated(object sender, StardewModdingAPI.Events.SaveCreatedEventArgs e)
        {
            Game1.player.mailReceived.OnValueAdded += OnMailReceived;
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            Game1.player.mailReceived.OnValueAdded += OnMailReceived;
        }

        private void OnMailReceived(string value)
        {
            // I hope this doesn't multi trigger...
            if (value == "DrakeScalePower")
                Game1.player.GetFarmerExtData().maxMana.Value += 25;
        }

        private void GameLoop_TimeChanged(object sender, StardewModdingAPI.Events.TimeChangedEventArgs e)
        {
            if (e.NewTime % 100 == 0)
            {
                State.CanRepairArmor = true;
            }
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            var ext = Game1.player.GetFarmerExtData();
            ext.mana.Value = ext.maxMana.Value;
            ext.armorUsed.Value = 0;
            ModSnS.State.HasCraftedFree = false;

            if (Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value)
            {
                if (!Game1.player.knowsRecipe("DN.SnS_Bullet"))
                {
                    Game1.player.craftingRecipes.Add("DN.SnS_Bullet", 0);
                }
                if (Game1.player.knowsRecipe("DN.SnS_FirestormArrow") && !Game1.player.knowsRecipe("DN.SnS_FirestormBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_FirestormBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_IcicleArrow") && !Game1.player.knowsRecipe("DN.SnS_IcicleBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_IcicleBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_RicochetArrow") && !Game1.player.knowsRecipe("DN.SnS_RicochetBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_RicochetBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_WindwakerArrow") && !Game1.player.knowsRecipe("DN.SnS_WindwakerBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_WindwakerBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_LightbringerArrow") && !Game1.player.knowsRecipe("DN.SnS_LightbringerBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_LightbringerBullet", 0);
            }
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button.IsActionButton() && Game1.currentLocation is Farm farm &&
                e.Cursor.Tile == farm.GetGrandpaShrinePosition().ToVector2() &&
                farm.grandpaScore.Value == 4 && !Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value)
            {
                Game1.player.addItemByMenuIfNecessaryElseHoldUp(new MeleeWeapon("DN.SnS_longlivetheking"));
                Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value = true;

                if (!Game1.player.knowsRecipe("DN.SnS_Bullet"))
                {
                    Game1.player.craftingRecipes.Add("DN.SnS_Bullet", 0);
                }
                if (Game1.player.knowsRecipe("DN.SnS_FirestormArrow") && !Game1.player.knowsRecipe("DN.SnS_FirestormBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_FirestormBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_IcicleArrow") && !Game1.player.knowsRecipe("DN.SnS_IcicleBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_IcicleBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_RicochetArrow") && !Game1.player.knowsRecipe("DN.SnS_RicochetBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_RicochetBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_WindwakerArrow") && !Game1.player.knowsRecipe("DN.SnS_WindwakerBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_WindwakerBullet", 0);
                if (Game1.player.knowsRecipe("DN.SnS_LightbringerArrow") && !Game1.player.knowsRecipe("DN.SnS_LightbringerBullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_LightbringerBullet", 0);
            }

            if (Context.IsWorldReady && Context.IsPlayerFree && e.Button.IsActionButton())
            {
                if (Game1.player.ActiveItem.QualifiedItemId == "(W)DN.SnS_longlivetheking")
                {
                    var w = new Slingshot("DN.SnS_longlivetheking_gun");
                    if (Game1.player.CurrentTool.attachments.Count > 0 && Game1.player.CurrentTool.attachments[0] != null)
                    {
                        w.attachments[0] = (StardewValley.Object) Game1.player.CurrentTool.attachments[0].getOne();
                        w.attachments[0].Stack = Game1.player.CurrentTool.attachments[0].Stack;
                    }
                    Game1.player.Items[Game1.player.CurrentToolIndex] = w;
                }
                else if (Game1.player.ActiveItem.QualifiedItemId == "(W)DN.SnS_longlivetheking_gun")
                {
                    var w = new MeleeWeapon("DN.SnS_longlivetheking");
                    if (Game1.player.CurrentTool.attachments.Count > 0 && Game1.player.CurrentTool.attachments[0] != null)
                    {
                        w.attachments.SetCount(1);
                        w.attachments[0] = (StardewValley.Object)Game1.player.CurrentTool.attachments[0].getOne();
                        w.attachments[0].Stack = Game1.player.CurrentTool.attachments[0].Stack;
                    }
                    Game1.player.Items[Game1.player.CurrentToolIndex] = w;
                }
            }
        }

        private void Display_RenderedWorld(object sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch1.Mateo.12") ||
                 Game1.player.team.acceptedSpecialOrderTypes.Contains("SwordSorcery") ||
                 Game1.eventUp)
            {
                return;
            }

            if (Game1.currentLocation.NameOrUniqueName == "Custom_EastScarpe")
            {
                Vector2 tile = new(24.4f, 82);
                float yOffset = 4 * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250f), 2);
                e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * Game1.tileSize + Game1.pixelZoom, tile.Y * Game1.tileSize - Game1.tileSize + Game1.tileSize / 8 + yOffset)), new Rectangle(395, 497, 3, 8), Color.White, 0f, new Vector2(1, 4), Game1.pixelZoom + Math.Max(0, .25f - yOffset / 8f), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            }
        }

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/ObjectInformation"))
            {
                e.Edit((asset) =>
                {
                    var dict = asset.AsDictionary<string, ObjectData>().Data;
                    dict.Add("DestyNova.SwordAndSorcery_BrokenHeroRelic", new ObjectData()
                    {
                        Name = "BrokenHeroRelic",
                        Price = 500,
                        Edibility = 300,
                        Category = StardewValley.Object.metalResources,
                        Type = "Metal",
                        DisplayName = I18n.HeroRelic_Broken_Name(),
                        Description = I18n.HeroRelic_Broken_Description(),
                        Texture = "DestyNova.SwordAndSorcery\\shields.png",
                        SpriteIndex = 0,
                        ContextTags = { "not_giftable" },
                        ExcludeFromShippingCollection = true,
                    });
                    dict.Add("DestyNova.SwordAndSorcery_RepairedHeroRelic", new ObjectData()
                    {
                        Name = "RepairedHeroRelic",
                        Price = 500,
                        Edibility = 300,
                        Category = StardewValley.Object.metalResources,
                        Type = "Metal",
                        DisplayName = I18n.HeroRelic_Repaired_Name(),
                        Description = I18n.HeroRelic_Repaired_Description(),
                        Texture = "DestyNova.SwordAndSorcery\\shields.png",
                        SpriteIndex = 1,
                        ContextTags = { "not_giftable" },
                        ExcludeFromShippingCollection = true,
                    });
                    dict.Add("DestyNova.SwordAndSorcery_LegendaryHeroRelic", new ObjectData()
                    {
                        Name = "LegendaryHeroRelic",
                        Price = 500,
                        Edibility = 300,
                        Category = StardewValley.Object.metalResources,
                        Type = "Metal",
                        DisplayName = I18n.HeroRelic_Legendary_Name(),
                        Description = I18n.HeroRelic_Legendary_Description(),
                        Texture = "DestyNova.SwordAndSorcery\\shields.png",
                        SpriteIndex = 2,
                        ContextTags = { "not_giftable" },
                        ExcludeFromShippingCollection = true,
                    });
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit((asset) =>
                {
                    var dict = asset.AsDictionary<string, ToolData>().Data;

                    dict.Add("DestyNova.SwordAndSorcery_Harp", new()
                    {
                        ClassName = "WarpHarp, SwordAndSorcerySMAPI",
                        Name = "Harp",
                        DisplayName = I18n.Harp_Name(),
                        Description = I18n.Harp_Description(),
                        Texture = "DestyNova.SwordAndSorcery/harp.png",
                        CanBeLostOnDeath = false
                    });
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("spacechase0.SpaceCore/ObjectExtensionData"))
            {
                e.Edit((asset) =>
                {
                    var dict = asset.AsDictionary<string, ObjectExtensionData>().Data;

                    ObjectExtensionData data = new()
                    {
                        CategoryTextOverride = I18n.ShieldCategory(),
                        CanBeShipped = false,
                        CanBeTrashed = false,
                        MaxStackSizeOverride = 1
                    };

                    dict.Add("DestyNova.SwordAndSorcery_BrokenHeroRelic", data);
                    dict.Add("DestyNova.SwordAndSorcery_RepairedHeroRelic", data);
                    dict.Add("DestyNova.SwordAndSorcery_LegendaryHeroRelic", data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("DestyNova.SwordAndSorcery/shields.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/shield-item.png", StardewModdingAPI.Events.AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("DestyNova.SwordAndSorcery/harp.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/harp.png", StardewModdingAPI.Events.AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("DestyNova.SwordAndSorcery/HarpSongs"))
            {
                e.LoadFrom(() =>
                {
                    var songs = new Dictionary<string, SongData>();

#if !NDEBUG
                    songs.Add("test", new()
                    {
                        DisplayName = "Test Song",
                        WarpLocationName = "FarmHouse",
                        WarpLocationTile = new(5, 7),
                        Notes = { SongData.Note.Up, SongData.Note.Down, SongData.Note.Left, SongData.Note.Right }
                    });
#endif

                    return songs;
                }, StardewModdingAPI.Events.AssetLoadPriority.Medium);
            }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var gmcm = Helper.ModRegistry.GetApi<SpaceShared.APIs.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm != null)
            {
                gmcm.Register(ModManifest, () => Config = new(), () => Helper.WriteConfig(Config));
                gmcm.AddKeybindList(ModManifest, () => Config.ConfigureAdventureBar, (val) => Config.ConfigureAdventureBar = val, I18n.Keybind_ConfigureBar_Name, I18n.Keybind_ConfigureBar_Description);
                gmcm.AddKeybindList(ModManifest, () => Config.ToggleAdventureBar, (val) => Config.ToggleAdventureBar = val, I18n.Keybind_ToggleBar_Name, I18n.Keybind_ToggleBar_Description);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot1, (val) => Config.AbilityBar1Slot1 = val, I18n.Keybind_Ability_1_1, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot2, (val) => Config.AbilityBar1Slot2 = val, I18n.Keybind_Ability_1_2, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot3, (val) => Config.AbilityBar1Slot3 = val, I18n.Keybind_Ability_1_3, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot4, (val) => Config.AbilityBar1Slot4 = val, I18n.Keybind_Ability_1_4, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot5, (val) => Config.AbilityBar1Slot5 = val, I18n.Keybind_Ability_1_5, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot6, (val) => Config.AbilityBar1Slot6 = val, I18n.Keybind_Ability_1_6, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot7, (val) => Config.AbilityBar1Slot7 = val, I18n.Keybind_Ability_1_7, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar1Slot8, (val) => Config.AbilityBar1Slot8 = val, I18n.Keybind_Ability_1_8, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot1, (val) => Config.AbilityBar2Slot1 = val, I18n.Keybind_Ability_2_1, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot2, (val) => Config.AbilityBar2Slot2 = val, I18n.Keybind_Ability_2_2, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot3, (val) => Config.AbilityBar2Slot3 = val, I18n.Keybind_Ability_2_3, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot4, (val) => Config.AbilityBar2Slot4 = val, I18n.Keybind_Ability_2_4, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot5, (val) => Config.AbilityBar2Slot5 = val, I18n.Keybind_Ability_2_5, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot6, (val) => Config.AbilityBar2Slot6 = val, I18n.Keybind_Ability_2_6, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot7, (val) => Config.AbilityBar2Slot7 = val, I18n.Keybind_Ability_2_7, I18n.Keybind_Ability_Desc);
                gmcm.AddKeybindList(ModManifest, () => Config.AbilityBar2Slot8, (val) => Config.AbilityBar2Slot8 = val, I18n.Keybind_Ability_2_8, I18n.Keybind_Ability_Desc);
                // shield throw is going away
            }

            Skills.RegisterSkill(RogueSkill = new RogueSkill());
            SpaceCore.CustomCraftingRecipe.CraftingRecipes.Add("DN.SnS_Bow", new BowCraftingRecipe());

            var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(ThrownShield));
            sc.RegisterCustomProperty(typeof(Farmer), "shieldSlot", typeof(NetRef<Item>), AccessTools.Method(typeof(Farmer_ArmorSlot), nameof(Farmer_ArmorSlot.get_armorSlot)), AccessTools.Method(typeof(Farmer_ArmorSlot), nameof(Farmer_ArmorSlot.set_armorSlot)));
            sc.RegisterCustomProperty(typeof(Farmer), "takenLoreWeapon", typeof(NetBool), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.HasTakenLoreWeapon)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetHasTakenLoreWeapon)));
            sc.RegisterCustomProperty(typeof(Farmer), "adventureBar", typeof(NetArray<string,NetString>), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.GetAdventureBar)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetAdventureBar)));
            sc.RegisterCustomProperty(typeof(Farmer), "maxMana", typeof(NetInt), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.GetMaxMana)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetMaxMana)));
            sc.RegisterCustomProperty(typeof(Farmer), "expRemainderRogue", typeof(NetFloat), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderRogueGetter)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderRogueSetter)));
        }

        private void GameLoop_UpdateTicking(object sender, StardewModdingAPI.Events.UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (State.MyThrown != null)
            {
                if ((State.MyThrown.GetPosition() - State.MyThrown.Target.Value).Length() < 1)
                {
                    var playerPos = Game1.player.getStandingPosition();
                    playerPos.X -= 16;
                    playerPos.Y -= 64;
                    State.MyThrown.Target.Value = playerPos;
                    if ((State.MyThrown.GetPosition() - playerPos).Length() < 16)
                    {
                        State.MyThrown.Dead = true;
                    }
                }
                if (State.MyThrown.Dead)
                    State.MyThrown = null;
            }
            else
            {
                if (State.ThrowCooldown > 0)
                    State.ThrowCooldown = MathF.Max(0, State.ThrowCooldown - (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds);

                if (Game1.player.get_armorSlot().Value?.QualifiedItemId == "(O)DestyNova.SwordAndSorcery_LegendaryHeroRelic")
                {
                    if (Config.ThrowShieldKey.JustPressed())
                    {
                        State.MyThrown = new ThrownShield(Game1.player, 30, Helper.Input.GetCursorPosition().AbsolutePixels, 10);
                        Game1.currentLocation.projectiles.Add(State.MyThrown);
                    }
                }
            }

            if (State.BlockCooldown > 0)
                State.BlockCooldown = MathF.Max(0, State.BlockCooldown - (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds);

            // ---

            if (/*Game1.player.eventsSeen.Contains(ModSnS.ShadowstepEventReq) &&*/ Game1.player.GetFarmerExtData().inShadows.Value)
            {
                var b = Game1.player.buffs.AppliedBuffs.FirstOrDefault(pair => pair.Key == "shadowstep").Value;
                if (b == null)
                {
                    b = new Buff(
                        "shadowstep",
                        duration: 250,
                        displayName: I18n.Ability_Shadowstep_Name(),
                        effects: new() { CriticalChanceMultiplier = { 999f } },
                        iconTexture: Game1.content.Load<Texture2D>(Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name ),
                        iconSheetIndex: 0);
                    Game1.player.applyBuff(b);
                }
                else
                {
                    b.millisecondsDuration = 250;
                }
            }

            // ---

            var ext = Game1.player.GetFarmerExtData();
            var hasBar = Game1.onScreenMenus.Any(m => m is AdventureBar);

            if (Config.ToggleAdventureBar.JustPressed() && Game1.player.hasOrWillReceiveMail("SnS_AdventureBar"))
            {
                AdventureBar.Hide = !AdventureBar.Hide;
                if (AdventureBar.Hide && hasBar)
                    Game1.onScreenMenus.Remove(Game1.onScreenMenus.Where(m => m is AdventureBar).First());
            }
            else if ( Config.ConfigureAdventureBar.JustPressed() && Game1.activeClickableMenu == null &&
                 Game1.player.hasOrWillReceiveMail("SnS_AdventureBar") )
            {
                Game1.activeClickableMenu = new AdventureBarConfigureMenu();
            }
            if ( e.IsOneSecond && !hasBar && !AdventureBar.Hide && Game1.player.hasOrWillReceiveMail( "SnS_AdventureBar" ) )
            {
                Game1.onScreenMenus.Add(new AdventureBar(editing: false));
            }
            else if (hasBar)
            {
                KeybindList[][] binds = new KeybindList[8][]
                {
                    new KeybindList[2] { Config.AbilityBar1Slot1, Config.AbilityBar2Slot1 },
                    new KeybindList[2] { Config.AbilityBar1Slot2, Config.AbilityBar2Slot2 },
                    new KeybindList[2] { Config.AbilityBar1Slot3, Config.AbilityBar2Slot3 },
                    new KeybindList[2] { Config.AbilityBar1Slot4, Config.AbilityBar2Slot4 },
                    new KeybindList[2] { Config.AbilityBar1Slot5, Config.AbilityBar2Slot5 },
                    new KeybindList[2] { Config.AbilityBar1Slot6, Config.AbilityBar2Slot6 },
                    new KeybindList[2] { Config.AbilityBar1Slot7, Config.AbilityBar2Slot7 },
                    new KeybindList[2] { Config.AbilityBar1Slot8, Config.AbilityBar2Slot8 },
                };

                for ( int islot = 0; islot < 8; ++islot )
                {
                    string abilId = null;
                    if (binds[islot][1].JustPressed() )
                    {
                        Helper.Input.SuppressActiveKeybinds(binds[islot][1]);
                        abilId = ext.adventureBar[8 + islot];
                    }
                    else if (binds[ islot ][ 0 ].JustPressed() )
                    {
                        Helper.Input.SuppressActiveKeybinds(binds[islot][0]);
                        abilId = ext.adventureBar[islot];
                    }

                    if ( abilId != null && Ability.Abilities.TryGetValue( abilId ?? "", out var abil ) && abil.ManaCost() <= ext.mana.Value && abil.CanUse() )
                    {
                        ext.mana.Value -= abil.ManaCost();
                        abil.Function();
                    }
                }
            }
        }

        private void Player_Warped(object sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            if (State.MyThrown != null)
            {
                State.MyThrown.Dead = true;
                State.MyThrown = null;
            }

            if (Game1.player.eventsSeen.Contains(ModSnS.ShadowstepEventReq))
            {
                //ModSnS.State.InShadows = true;
            }

            var ext = Game1.player.GetFarmerExtData();
            int? armorAmount = Game1.player.get_armorSlot().Value.GetArmorAmount();
            if (State.CanRepairArmor && Game1.player.HasCustomProfession(RogueSkill.ProfessionArmorRecovery) && armorAmount.HasValue)
            {
                State.CanRepairArmor = false;
                ext.armorUsed.Value = Math.Max(0, ext.armorUsed.Value - armorAmount.Value / 5);
            }
        }

        private void Display_RenderedHud(object sender, StardewModdingAPI.Events.RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.CurrentEvent != null)
                return;

            if (State.BlockCooldown > 0)
            {
                var toolbar = Game1.onScreenMenus.First((menu) => menu is Toolbar);
                float y = toolbar.yPositionOnScreen - toolbar.height / 2 - Game1.smallFont.MeasureString("T").Y - 16;
                if (toolbar.yPositionOnScreen < Game1.uiViewport.Height / 2)
                    y = toolbar.yPositionOnScreen + toolbar.height + 16;

                e.SpriteBatch.DrawString(Game1.smallFont, I18n.ShieldBlockText(State.BlockCooldown), new Vector2(toolbar.xPositionOnScreen + 64, y) + new Vector2(2, 2), Color.Black);
                e.SpriteBatch.DrawString(Game1.smallFont, I18n.ShieldBlockText(State.BlockCooldown), new Vector2(toolbar.xPositionOnScreen + 64, y) + new Vector2(-2, 2), Color.Black);
                e.SpriteBatch.DrawString(Game1.smallFont, I18n.ShieldBlockText(State.BlockCooldown), new Vector2(toolbar.xPositionOnScreen + 64, y) + new Vector2(-2, -2), Color.Black);
                e.SpriteBatch.DrawString(Game1.smallFont, I18n.ShieldBlockText(State.BlockCooldown), new Vector2(toolbar.xPositionOnScreen + 64, y) + new Vector2(2, -2), Color.Black);
                e.SpriteBatch.DrawString(Game1.smallFont, I18n.ShieldBlockText(State.BlockCooldown), new Vector2(toolbar.xPositionOnScreen + 64, y), Color.White);
            }

            var armorAmt = Game1.player.get_armorSlot().Value.GetArmorAmount();
            if ((armorAmt ?? -1) >= 0 && Game1.showingHealth)
            {
                float modifier = 0.625f;
                Vector2 topOfBar = new Vector2(Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Right - 48 - 8, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 224 - 16 - (int)((float)(Game1.player.MaxStamina - 270) * modifier));
                if (Game1.isOutdoorMapSmallerThanViewport())
                {
                    topOfBar.X = Math.Min(topOfBar.X, -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 48);
                }
                if (Game1.staminaShakeTimer > 0)
                {
                    topOfBar.X += Game1.random.Next(-3, 4);
                    topOfBar.Y += Game1.random.Next(-3, 4);
                }
                topOfBar.X -= 56 + ((Game1.hitShakeTimer > 0) ? Game1.random.Next(-3, 4) : 0);
                topOfBar.Y = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 224 - 16 - (Game1.player.maxHealth - 100);

                Vector2 spot = topOfBar + new Vector2(24, -40);
                e.SpriteBatch.Draw(ShieldItemTexture, spot, new Rectangle(16, 0, 16, 16), Color.White, 0, new Vector2(8, 8), 4, SpriteEffects.None, 1);

                string str = (armorAmt - Game1.player.GetFarmerExtData().armorUsed.Value) + $"/{armorAmt}";
                Vector2 size = Game1.smallFont.MeasureString(str);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(-2, 0), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(2, 0), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(0, -2), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(0, 2), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(0, 0), Color.White, 0, size / 2, 1, SpriteEffects.None, 1);
            }
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.withinPlayerThreshold), new Type[] { typeof( int ) } )]
    public static class NpcShadowstepPatch
    {
        public static void Postfix(NPC __instance, int threshold, ref bool __result )
        {
            if ( __instance is Monster && Game1.player.GetFarmerExtData().inShadows.Value)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.leftClick))]
    public static class WeaponSwingShadowstepPatch
    {
        public static void Postfix(Farmer who, NPC __instance)
        {
            who.GetFarmerExtData().inShadows.Value = false;
        }
    }

    [HarmonyPatch(typeof(Farm), nameof(Farm.draw))]
    public static class FarmDrawSwordOverlayPatch
    {
        public static void Postfix(SpriteBatch b)
        {
            if (Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value)
                return;

            Vector2 spot = (Game1.currentLocation as Farm).GetGrandpaShrinePosition().ToVector2();
            spot += new Vector2(0, -1);
            spot *= Game1.tileSize;
            spot += new Vector2(3, 5) * Game1.pixelZoom;
            b.Draw(ModSnS.SwordOverlay, Game1.GlobalToLocal(Game1.viewport, spot), null, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, ((Game1.currentLocation as Farm).GetGrandpaShrinePosition().ToVector2().Y * Game1.tileSize + 1) / 10000f);
        }
    }

    [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.IsTimedQuest))]
    public static class SpecialOrderUntimedQuestsPatch1
    {
        public static void Postfix(SpecialOrder __instance, ref bool __result)
        {
            if (__instance.questKey.Value.StartsWith("CAGQuest.UntimedSpecialOrder") || __instance.questKey.Value == "Mateo.SpecialOrders.BuildGuild")
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.UpdateAvailableSpecialOrders))]
    public static class SpecialOrderAvailabilityPatch
    {
        public static void Postfix(string orderType, bool forceRefresh)
        {
            if (orderType == "" && forceRefresh)
            {
                SNSUpdateAvailability();
            }
        }

        private static void SNSUpdateAvailability()
        {
            string orderType = "SwordSorcery";
            bool forceRefresh = true;

            foreach (SpecialOrder order in Game1.player.team.availableSpecialOrders)
            {
                if ((order.questDuration.Value == QuestDuration.TwoDays || order.questDuration.Value == QuestDuration.ThreeDays) && !Game1.player.team.acceptedSpecialOrderTypes.Contains(order.orderType.Value))
                {
                    order.SetDuration(order.questDuration.Value);
                }
            }
            if (!forceRefresh)
            {
                foreach (SpecialOrder availableSpecialOrder in Game1.player.team.availableSpecialOrders)
                {
                    if (availableSpecialOrder.orderType.Value == orderType)
                    {
                        return;
                    }
                }
            }
            SpecialOrder.RemoveAllSpecialOrders(orderType);
            List<string> keyQueue = new List<string>();
            foreach (KeyValuePair<string, SpecialOrderData> pair in DataLoader.SpecialOrders(Game1.content))
            {
                if (pair.Value.OrderType == orderType && SpecialOrder.CanStartOrderNow(pair.Key, pair.Value))
                {
                    keyQueue.Add(pair.Key);
                }
            }
            List<string> keysIncludingCompleted = new List<string>(keyQueue);
            if (orderType == "")
            {
                //keyQueue.RemoveAll((string id) => Game1.player.team.completedSpecialOrders.Contains(id));
            }
            Random r = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.DaysPlayed * 1.3);
            for (int i = 0; i < 2; i++)
            {
                if (keyQueue.Count == 0)
                {
                    if (keysIncludingCompleted.Count == 0)
                    {
                        break;
                    }
                    keyQueue = new List<string>(keysIncludingCompleted);
                }
                string key = r.ChooseFrom(keyQueue);
                Game1.player.team.availableSpecialOrders.Add(SpecialOrder.GetSpecialOrder(key, r.Next()));
                keyQueue.Remove(key);
                keysIncludingCompleted.Remove(key);
            }
        }
    }

    [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.SetDuration))]
    public static class SpecialOrderUntimedQuestsPatch2
    {
        public static bool Prefix(SpecialOrder __instance)
        {
            // I don't know why this is necessary
            if (__instance.questKey.Value.StartsWith("CAGQuest.UntimedSpecialOrder") || __instance.questKey.Value == "Mateo.SpecialOrders.BuildGuild")
            {
                __instance.dueDate.Value = Game1.Date.TotalDays + 999;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpecialOrdersBoard), nameof(SpecialOrdersBoard.GetPortraitForRequester))]
    public static class SpecialOrderPortraitPatch
    {
        public static void Postfix(string requester_name, ref KeyValuePair<Texture2D, Rectangle>? __result)
        {
            List<string> npc = new List<string>() { "Mateo", "Hector", "Cirrus", "Dandelion", "Roslin" };
            int x = npc.IndexOf(requester_name);
            if (x == -1)
                return;

            __result = new KeyValuePair<Texture2D, Rectangle>(Game1.content.Load<Texture2D>("LooseSprites/CAGemojis"), new Rectangle(x * 9, 0, 9, 9));
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public static class FarmerArmorBlocksDamagePatch
    {
        public static bool Prefix(Farmer __instance, ref int damage, bool overrideParry, Monster damager)
        {
            var ext = Game1.player.GetFarmerExtData();
            if (__instance != Game1.player || overrideParry || !Game1.player.CanBeDamaged() ||
                Game1.player.get_armorSlot().Value == null ||
                ext.armorUsed.Value >= (Game1.player.get_armorSlot().Value.GetArmorAmount() ?? -1))
                return true;

            bool flag = (damager == null || !damager.isInvincible()) && (damager == null || (!(damager is GreenSlime) && !(damager is BigSlime)) || !__instance.isWearingRing("520"));
            if (!flag) return true;

            __instance.playNearbySoundAll("parry");

            ext.armorUsed.Value = Math.Min(Game1.player.get_armorSlot().Value.GetArmorAmount().Value, ext.armorUsed.Value + damage);
            damage = 0;

            return true;
        }
    }

    [HarmonyPatch(typeof(CraftingRecipe), nameof(CraftingRecipe.consumeIngredients))]
    public static class CraftingRecipeFlashOfGeniusPatch
    {
        public static bool Prefix()
        {
            if (!ModSnS.State.HasCraftedFree && Game1.player.HasCustomProfession(RogueSkill.ProfessionCrafting))
            {
                ModSnS.State.HasCraftedFree = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.GetEffectsOfRingMultiplier))]
    public static class FarmerRingEffectsMultiplierPatch
    {
        public static void Postfix(Farmer __instance, string ringId, ref int __result)
        {
            if (ringId == "863" && __instance.eventsSeen.Contains("SnS.Ch3.Cirrus.9"))
                ++__result;
        }
    }
}