using ContentPatcher;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using RadialMenu;
using SpaceCore;
using SpaceCore.Dungeons;
using SpaceShared.APIs;
using StarControl;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Weapons;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.Tools;
using StardewValley.Triggers;
using SwordAndSorcerySMAPI.Deprecated;
using SwordAndSorcerySMAPI.Framework.Abilities;
using SwordAndSorcerySMAPI.Framework.Alchemy;
using SwordAndSorcerySMAPI.Framework.DualWieldingAndWeapons;
using SwordAndSorcerySMAPI.Framework.Finale;
using SwordAndSorcerySMAPI.Framework.IgnoreMarriageSchedule;
using SwordAndSorcerySMAPI.Framework.Menus;
using SwordAndSorcerySMAPI.Framework.Menus.AdventureBar;
using SwordAndSorcerySMAPI.Framework.Menus.AdventureBar.ControllerSupport;
using SwordAndSorcerySMAPI.Framework.MercenaryPort;
using SwordAndSorcerySMAPI.Framework.ModSkills;
using SwordAndSorcerySMAPI.Framework.NEA;
using SwordAndSorcerySMAPI.Framework.NEA.Utils;
using SwordAndSorcerySMAPI.Framework.Shield;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SwordAndSorcerySMAPI
{
    public class FinalePartnerInfo
    {
        public string IntermissionEventId { get; set; }
        public string VictoryEventId { get; set; }
    }

    public class FarmerExtData
    {
        public readonly NetInt form = new(0);
        public readonly NetBool transformed = new(false);
        public readonly NetFloat expRemainder = new(0);
        public double noMovementTimer = 0;
        public double MovementTimer = 0;

        public bool IsResting => noMovementTimer >= 3;

        public static int FormGetter(Farmer farmer)
        {
            return farmer.GetFarmerExtData().form.Value;
        }
        public static void FormSetter(Farmer farmer, int val)
        {
            farmer.GetFarmerExtData().form.Value = val;
        }
        public static float ExpRemainderGetter(Farmer farmer)
        {
            return farmer.GetFarmerExtData().expRemainder.Value;
        }
        public static void ExpRemainderSetter(Farmer farmer, float val)
        {
            farmer.GetFarmerExtData().expRemainder.Value = val;
        }

        public readonly NetBool hasTakenLoreWeapon = new(false);

        public static void SetHasTakenLoreWeapon(Farmer farmer, NetBool val)
        { }
        
        public static NetBool HasTakenLoreWeapon(Farmer farmer)
        {
            return farmer.GetFarmerExtData().hasTakenLoreWeapon;
        }

        public readonly NetBool inShadows = new(false);

        public readonly NetArray<string, NetString> adventureBar = new(8 * 2);

        public static void SetAdventureBar(Farmer farmer, NetArray<string, NetString> val)
        { }
        
        public static NetArray<string, NetString> GetAdventureBar(Farmer farmer)
        {
            return farmer.GetFarmerExtData().adventureBar;
        }

        public readonly NetInt mana = [];
        public readonly NetInt maxMana = [];

        public static void SetMaxMana(Farmer farmer, NetInt val)
        { }
        
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
        { }
        
        public Dictionary<string, int> Cooldowns = [];
        public readonly NetInt armorUsed = new(0);
        public readonly NetInt mirrorImages = new(0);
        public int currRenderingMirror = 0;
        public bool mageArmor = false;
        public readonly NetBool isGhost = new(false);
        public readonly NetVector2 ghostOrigPosition = [];
        public readonly NetFloat stasisTimer = new(-1);

        public NetBool DoingFinale = new(false);
        public bool StartingArtificer = false;
        public bool StartingDruidics = false;
        public bool StartingBardics = false;
        public bool StartingSorcery = false;
        public bool StartingPaladin = false;
        public readonly Dictionary<MeleeWeapon, List<BaseEnchantment>> OrigEnchs = [];
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
            __instance.NetFields.AddField(__instance.GetFarmerExtData().stasisTimer);
            __instance.NetFields.AddField(__instance.GetFarmerExtData().DoingFinale);
        }
    }

    public static partial class Extensions
    {
        public static FarmerExtData GetFarmerExtData(this Farmer instance)
        {
            return ModSnS.FarmerData.GetOrCreateValue(instance);
        }

        public static bool IsArmorItem(this Item item)
        {
            return item.GetArmorAmount(false, false) != null;
        }

        public static bool IsShieldItem(this MeleeWeapon mw)
        {
            return mw?.GetData()?.CustomFields?.ContainsKey("DN.SnS_Shield") ?? false;
        }

        public static int? GetArmorAmount(this Item item, bool includeArmor = true, bool includeMageArmor = true)
        {
            int ArmorAmount;
            int ShieldAmount = 0;
            int MageArmor = Game1.player.GetFarmerExtData().mageArmor ? 50 : 0;

            if (Game1.player.CurrentTool is MeleeWeapon mw1 && mw1.IsShieldItem())
                ShieldAmount += GetAmount(mw1) ?? 0;
            if (Game1.player.GetOffhand() is MeleeWeapon mw2 && mw2.IsShieldItem())
                ShieldAmount += GetAmount(mw2) ?? 0;

            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldArmor2))
                ShieldAmount *= 4;
            else if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldArmor1))
                ShieldAmount *= 2;

            ArmorAmount = (int)(GetAmount(item) * (Game1.player.HasCustomProfession(RogueSkill.ProfessionArmorCap) ? 1.5f : 1));

            int FinalAmount = ArmorAmount + (includeArmor ? ShieldAmount : 0) + (includeMageArmor ? MageArmor : 0);
            
            return FinalAmount == 0 ? null : FinalAmount;
        }

        public static int? GetAmount(Item item)
        {
            if (item is MeleeWeapon)
            {
                if (item != null && ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).RawData is WeaponData data &&
                        (data?.CustomFields?.TryGetValue("ArmorValue", out string valStr) ?? false) && int.TryParse(valStr, out int val))
                    return val;
                else
                    return 25;
            }
            else if (item != null && ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).RawData is ObjectData data &&
                (data?.CustomFields?.TryGetValue("ArmorValue", out string valStr) ?? false) && int.TryParse(valStr, out int val))
                return val;
            
            return 0;
        }
    }

    public class State
    {
        public List<ThrownShield> MyThrown { get; set; } = [];
        public float ThrowCooldown { get; set; } = 0;

        public float BlockCooldown { get; set; } = 0;
        public bool CanRepairArmor { get; set; } = true;
        public bool HasCraftedFree { get; set; } = false;

        public Monster LastAttacked { get; set; } = null;
        public int LastAttackedCounter { get; set; } = 0;

        public Point LastWalkedTile { get; set; } = new();
        public Dictionary<string, string> TeleportCircles { get; set; } = [];

        public string ReturnPotionLocation { get; set; }
        public Point ReturnPotionCoordinates { get; set; }

        public string PocketDimensionLocation { get; set; } = "FarmHouse";
        public Point PocketDimensionCoordinates { get; set; } = new Point(5, 7);

        public int PreCastFacingDirection { get; set; }

        public bool DoFinale { get; set; } = false;
        public Monster FinaleBoss { get; set; }

        public class PolymorphData
        {
            public Monster Original { get; init; }
            public float Timer { get; set; } = 10;
        }
        public class BanishData
        {
            public GameLocation Location { get; init; }
            public float Timer { get; set; } = 15;
        }

        public Dictionary<GreenSlime, PolymorphData> Polymorphed { get; set; } = [];
        public Dictionary<Monster, BanishData> Banished { get; set; } = [];
    }

    public class Configuration
    {
        public int Red = 0;
        public int Green = 255;
        public int Blue = 255;

        public int TextRed = 0;
        public int TextGreen = 0;
        public int TextBlue = 0;

        public float MonsterHealthBuff { get; set; } = 1.75f;

        public KeybindList ConfigureAdventureBar = new(SButton.U);
        public KeybindList ToggleAdventureBar = new(new Keybind(SButton.LeftControl, SButton.U));
        public string ShieldThrowMethod = "both";
        public KeybindList ShieldThrowKeybind = new(new Keybind(SButton.None));

        public bool LltkToggleRightClick = false;
        public KeybindList LltkToggleKeybind = new(new Keybind(SButton.None));
        public string LltkDifficulty = "Medium";


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
    }

    public partial class ModSnS : Mod
    {
        private static ModSnS instance;
        public static Configuration Config { get; set; }
        private static readonly PerScreen<State> _state = new(() => new State());
        public static State State => _state.Value;

        public static ModSnS Instance { get => instance; set => instance = value; }

        public static Texture2D ShieldItemTexture { get; set; }
        public static Texture2D SwordOverlay { get; set; }

        public static ConditionalWeakTable<Farmer, FarmerExtData> FarmerData { get; set; } = [];

        public static RogueSkill RogueSkill { get; set; }

        public const string ShadowstepEventReq = "SnS.Ch1.Val.18";

        public static ISpaceCoreApi SpaceCore { get; set; }
        public static IRadialMenuApi Radial { get; set; }
        public static IStarControlApi StarControl { get; set; }

        public static double AetherRestoreTimer { get; set; } = 0;

        private Harmony Harmony { get; set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float AetherDamageMultiplier()
        {
            return 1;
        }

        public override void Entry(IModHelper helper)
        {
            if (!Helper.ModRegistry.IsLoaded("DN.SnS"))
            {
                Monitor.Log("Failed to find the CP component of S&S, make sure you installed everything from the download. (S&S will not load without it.)", LogLevel.Error);
                return;
            }

            Instance = this;
            I18n.Init(Helper.Translation);
            Config = Helper.ReadConfig<Configuration>();

            helper.ConsoleCommands.Add("sns_checkStartingSkills", "...", (cmd, args) =>
            {
                var ext = Game1.player.GetFarmerExtData();
                Log.Warn($"Artificer: {ext.StartingArtificer}, Druidics: {ext.StartingDruidics}, Bardics: {ext.StartingBardics}, Sorcery: {ext.StartingSorcery}, Paladin: {ext.StartingPaladin}");
            });

            Event.RegisterCommand("sns_rogueunlock", (Event @event, string[] args, EventContext context) =>
            {
                // This implementation is incredibly lazy
                ArgUtility.TryGetVector2(args, 1, out Vector2 center, out string error);
                center.Y -= 0.5f;

                List<TemporaryAnimatedSprite> tass = [];

                @event.aboveMapSprites ??= [];

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

            Event.RegisterCommand("sns_lock_finale", (Event @event, string[] args, EventContext context) =>
            {
                Game1.player.GetFarmerExtData().DoingFinale.Value = true;
                @event.CurrentCommand++;
            });
            Event.RegisterCommand("sns_finale_phase1", (Event @event, string[] args, EventContext context) =>
            {
                Game1.currentMinigame ??= new FinalePhase1Minigame(@event, context);
            });
            Event.RegisterCommand("sns_finale_phase2", (Event @event, string[] args, EventContext context) =>
            {
                State.DoFinale = true;
                @event.CurrentCommand++;
            });
            ShieldItemTexture = Helper.ModContent.Load<Texture2D>("assets/shield-item.png");
            SwordOverlay = Helper.ModContent.Load<Texture2D>("assets/SwordOverlay.png");

            Helper.Events.Content.AssetRequested += Content_AssetRequested;
            Helper.Events.Content.AssetRequested += IgnoreMarriageScheduleAssetManager.AssetRequested;
            Helper.Events.Content.AssetsInvalidated += Content_AssetsInvalidated;
            Helper.Events.Content.AssetsInvalidated += IgnoreMarriageScheduleAssetManager.AssetInvalidated;
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            Helper.Events.GameLoop.SaveLoaded += DualWieldingEnchants.HandleEnchants;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.DayStarted += KeychainsAndTrinkets.DayStarted;
            Helper.Events.GameLoop.DayStarted += IgnoreMarriageScheduleUtil.DayStarted;
            Helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            Helper.Events.GameLoop.DayEnding += KeychainsAndTrinkets.DayEnding;
            Helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged;
            Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.Player.InventoryChanged += Player_InventoryChanged;
            Helper.Events.Display.RenderedHud += Display_RenderedHud;
            Helper.Events.Display.RenderedWorld += Display_RenderedWorld;
            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.World.NpcListChanged += World_NpcListChanged;
            Helper.Events.World.LocationListChanged += World_LocationListChanged;
            Helper.Events.GameLoop.ReturnedToTitle += GameLoop_ReturnedToTitle;

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

            GameLocation.RegisterTileAction("DN.SnS_ShieldSigilMenu", (loc, args, f, p) =>
            {
                if (ModTOP.PaladinSkill.ShouldShowOnSkillsPage)
                    Game1.activeClickableMenu = new ShieldSigilMenu();
                return true;
            });
            
            GameLocation.RegisterTileAction("DN.SnS_DuskspireWarp", (loc, args, f, p) =>
            {
                if (Game1.getAllFarmers().Any(f => f.GetFarmerExtData().DoingFinale.Value))
                {
                    Game1.addHUDMessage(new(I18n.CannotWarp()));
                    return true;
                }
                Game1.warpFarmer("EastScarp_DuskspireLair", 3, 15, 2);
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
                KnownCondition = "PLAYER_HAS_SHADOWSTEP Current",
                HiddenIfLocked = true,
                ManaCost = () => 15,
                Function = () =>
                {
                    Game1.player.GetFarmerExtData().inShadows.Value = true;
                }
            });

            Ability.Abilities.Add("remoteguide", new Ability("remoteguide")
            {
                Name = I18n.Ability_Remoteguide_Name,
                Description = I18n.Ability_Remoteguide_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 1,
                KnownCondition = "true",
                HiddenIfLocked = true,
                ManaCost = () => 0,
                Function = () =>
                {
                    string openguide = "spacechase0.SpaceCore_OpenGuidebook DN.SnS";
                    if (!TriggerActionManager.TryRunAction(openguide, out string error, out Exception ex))
                        Log.Error($"Failed running action '{openguide}': {error}", ex);
                }
            });

            Ability.Abilities.Add("remotealchemy", new Ability("remotealchemy")
            {
                Name = I18n.Ability_Remotealchemy_Name,
                Description = I18n.Ability_Remotealchmey_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/witchcraft/stuff.png").Name,
                SpriteIndex = 0,
                KnownCondition = "PLAYER_HAS_SEEN_EVENT Current SnS.Ch4.Dandelion.6",
                HiddenIfLocked = true,
                ManaCost = () => 0,
                Function = () =>
                {
                    Game1.activeClickableMenu ??= new FancyAlchemyMenu();
                }
            });

            Ability.Abilities.Add("remoteunderforge", new Ability("remoteunderforge")
            {
                Name = I18n.Ability_Remotearsenal_Name,
                Description = I18n.Ability_Remotearsenal_Description,
                TexturePath = "Textures/DN.SnS/SnSObjects",
                SpriteIndex = 21,
                KnownCondition2 = () => ModTOP.PaladinSkill.ShouldShowOnSkillsPage,
                HiddenIfLocked = true,
                ManaCost = () => 0,
                Function = () =>
                {
                    Game1.activeClickableMenu ??= new ArsenalMenu();
                }
            });

            Ability.Abilities.Add("remoteshieldsigil", new Ability("remoteshieldsigil")
            {
                Name = I18n.Ability_Remotesigil_Name,
                Description = I18n.Ability_Remotesigil_Description,
                TexturePath = "Textures/DN.SnS/SnSObjects",
                SpriteIndex = 16,
                KnownCondition = $"PLAYER_HAS_SEEN_EVENT Current SnS.Ch4.Finale",
                HiddenIfLocked = true,
                ManaCost = () => 0,
                Function = () =>
                {
                    Game1.activeClickableMenu ??= new ShieldSigilMenu();
                }
            });

            Ability.Abilities.Add("swaplltk", new Ability("swaplltk")
            {
                Name = I18n.Ability_LltkToggle_Name,
                Description = I18n.Ability_LltkToggle_Description,
                TexturePath = "Textures/DN.SnS/SnSObjects",
                SpriteIndex = 45,
                KnownCondition = "PLAYER_HAS_MAIL Current DN.SnS_ObtainedLLTK",
                HiddenIfLocked = true,
                ManaCost = () => 0,
                Function = () => SwapLltk(),
                CanUse = () => (Game1.player.ActiveItem?.QualifiedItemId.ContainsIgnoreCase("(W)DN.SnS_longlivetheking") ?? false)
            });

            Helper.ConsoleCommands.Add("sns_setmaxaether", "Sets Max Aether to the provided amount, or resets to default with the 'reset' argument", (cmd, args) => {
                Game1.player.mailReceived.RemoveWhere(m => m.StartsWith("DN.SnS.MaxMana_"));
                if (args[0].EqualsIgnoreCase("reset"))
                {
                    RecalculateAether();
                    return;
                }
                int newMax = int.Parse(args[0]);
                Game1.player.mailReceived.Add($"DN.SnS.MaxMana_{newMax}");
                Game1.player.GetFarmerExtData().maxMana.Value = newMax;
            });
            Helper.ConsoleCommands.Add("sns_refillaether", "Refills your aether.", (cmd, args) => Game1.player.GetFarmerExtData().mana.Value = Game1.player.GetFarmerExtData().maxMana.Value);
            Helper.ConsoleCommands.Add("sns_repairarmor", "Repairs your armor", (cmd, args) => Game1.player.GetFarmerExtData().armorUsed.Value = 0);
            Helper.ConsoleCommands.Add("sns_finishorders", "Auto-Completes your active S&S special orders.", (cmd, args) =>
            {
                string[] valid =
                [
                    "Val.SpecialOrders.BuildGuild",
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
            Helper.ConsoleCommands.Add("sns_startfinalephase2", "While in the Duskspire Lair, starts the finale phase 2 fight.", (cmd, args) =>
            {
                if (!Context.IsPlayerFree || Game1.currentLocation.NameOrUniqueName != "EastScarp_DuskspireLair")
                {
                    Log.Info("Invalid situation");
                    return;
                }
                State.DoFinale = true;
            });

            Harmony = new Harmony(ModManifest.UniqueID);

            new ModCoT(Monitor, ModManifest, Helper).Entry();
            new ModNEA(Monitor, ModManifest, Helper, Harmony).Entry();
            new ModUP(Monitor, ModManifest, Helper).Entry();
            new ModTOP(Monitor, ModManifest, Helper).Entry();
            new MercenaryEngine(Helper).InitMercenary();
            new AlchemyEngine(Helper).InitAlchemy();
            Spells.RegisterSpells(helper);
            InitArsenal();
        }

        private void GameLoop_ReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            Spells.CurrPositioningAbil = null;
        }

        private void Player_InventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (e.Removed.Any(i => i is MeleeWeapon or Slingshot && i.ItemId.ContainsIgnoreCase("DN.SnS_longlivetheking")))
                foreach (Item i in e.Removed.Where(i => i is MeleeWeapon or Slingshot && i.ItemId.ContainsIgnoreCase("DN.SnS_longlivetheking")))
                {
                    if (i is not Tool) continue;
                    Tool LLTK = i as Tool;
                    if (LLTK.attachments?[1] is Trinket) KeychainsAndTrinkets.HandleTrinketEquipUnequip(Old: LLTK.attachments[1]);
                }

            if (e.Added.Any(i => i is MeleeWeapon or Slingshot && i.ItemId.ContainsIgnoreCase("DN.SnS_longlivetheking")))
                foreach (Item i in e.Added.Where(i => i is MeleeWeapon or Slingshot && i.ItemId.ContainsIgnoreCase("DN.SnS_longlivetheking")))
                {
                    if (i is not Tool) continue;
                    Tool LLTK = i as Tool;
                    if (LLTK.attachments?[1] is Trinket) KeychainsAndTrinkets.HandleTrinketEquipUnequip(New: LLTK.attachments[1]);
                }
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            Spells.CurrPositioningAbil = null;

            Utility.ForEachLocation((loc) =>
            {
                List<Monster> dusk = [];
                foreach (Character npc in loc.characters)
                    if (npc is Monster && npc is DuskspireMonster m)
                        dusk.Add(m);
                loc.characters.RemoveWhere(dusk.Contains);
                return true;
            });

            Helper.Data.WriteGlobalData<Dictionary<string, bool>>("VB.FM_Data", new()
                {
                    {"Finale", Game1.player.eventsSeen.Contains("SnS.Ch4.Finale")},
                });
        }

        private void Content_AssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(l => l.BaseName.EqualsIgnoreCase("DN.SnS/AlchemyRecipe")))
            {
                AlchemyRecipes._RecipeData = null;
            }
        }

        private void World_LocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            foreach (var loc in e.Added)
            {
                if (loc is MineShaft ms && ms.mineLevel < 20)
                    return;

                foreach (var npc in loc.characters)
                {
                    if (npc is Monster monster && !monster.modData.ContainsKey($"{ModManifest.UniqueID}_BuffedHealth"))
                    {
                        monster.MaxHealth = (int)(monster.MaxHealth * Config.MonsterHealthBuff);
                        monster.Health = (int)(monster.Health * Config.MonsterHealthBuff);
                        monster.modData.Add($"{ModManifest.UniqueID}_BuffedHealth", "hoot");
                    }
                }
            }
        }

        private void World_NpcListChanged(object sender, StardewModdingAPI.Events.NpcListChangedEventArgs e)
        {
            if (e.Location is MineShaft ms && ms.mineLevel < 20)
                return;

            foreach (var npc in e.Added)
            {
                if (npc is Monster monster && !monster.modData.ContainsKey($"{ModManifest.UniqueID}_BuffedHealth"))
                {
                    monster.MaxHealth = (int)(monster.MaxHealth * Config.MonsterHealthBuff);
                    monster.Health = (int)(monster.Health * Config.MonsterHealthBuff);
                    monster.modData.Add($"{ModManifest.UniqueID}_BuffedHealth", "hoot");
                }
            }
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Remove Seasonal Offerings Special Orders
            Game1.player.team.specialOrders.RemoveWhere(o => o.questKey.Value.StartsWithIgnoreCase("CAGQuest.UntimedSpecialOrder.Pentacle"));

            // Legacy armor slot migration
            foreach (var player in Game1.getAllFarmers())
            {
                var armorSlot = player.get_armorSlot();
                if (armorSlot.Value != null && SpaceCore.GetItemInEquipmentSlot(player, $"{ModManifest.UniqueID}_Armor") == null)
                {
                    SpaceCore.SetItemInEquipmentSlot(player, $"{ModManifest.UniqueID}_Armor", armorSlot.Value);
                    armorSlot.Value = null;
                }
            }
        }

        private void GameLoop_TimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (e.NewTime % 100 == 0)
            {
                State.CanRepairArmor = true;

                if (Game1.player.hasOrWillReceiveMail("MagicalGrimoirePower"))
                {
                    var ext = Game1.player.GetFarmerExtData();
                    float perc = 0.05f
                        + Game1.player.GetCustomSkillLevel(ModCoT.DruidSkill) / 5 * 0.02f
                        + Game1.player.GetCustomSkillLevel(ModUP.BardSkill) / 5 * 0.02f
                        + Game1.player.GetCustomSkillLevel(ModTOP.SorcerySkill) / 5 * 0.02f;

                    ext.mana.Value = Math.Min(ext.maxMana.Value, ext.mana.Value + (int)(ext.maxMana.Value * perc));
                }
            }
        }

        private static void RecalculateAether()
        { 
            var ext = Game1.player.GetFarmerExtData();

            int maxMana = 0;

            //DrakeScalePower
            if (Game1.player.hasOrWillReceiveMail("DrakeScalePower"))
                maxMana += 25;

            //Artificer Mana
            if (Game1.player.GetCustomSkillLevel(RogueSkill.Id) >= 1)
                maxMana += 30;

            //Druidics Mana
            for (int i = 0; i < Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Druidics"); i++)
            {
                if (i == 4 || i == 9) continue;
                else if (i >= 10) break;
                else maxMana += 5;
            }

            //Bardics Mana
            for (int i = 0; i < Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Bardics"); i++)
            {
                if (i == 4 || i == 9) continue;
                else if (i >= 10) break;
                else maxMana += 10;
            }

            //Sorcery Mana
            for (int i = 0; i < Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Witchcraft"); i++)
            {
                if (i == 4 || i == 9) continue;
                else if (i >= 10) break;
                else maxMana += 10;
            }

            if (Game1.player.HasCustomProfession(SorcerySkill.ProfessionAetherBuff))
            {
                maxMana += 75;
            }

            if (Game1.player.mailReceived.Any(m => m.StartsWith("DN.SnS.MaxMana_")) && int.TryParse(Game1.player.mailReceived.First(m => m.StartsWith("DN.SnS.MaxMana_")).Split('_')[1], out int OverridenMaxMana))
                maxMana = OverridenMaxMana;

            ext.maxMana.Value = maxMana;
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Game1.player.knowsRecipe("DN.SnS_SteelShield"))
                Game1.player.craftingRecipes.Add("DN.SnS_SteelShield", 0);

            var ext = Game1.player.GetFarmerExtData();

            if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) == "Mon")
                Game1.getLocationFromName("EastScarp_DuskspireLair").modData.Remove("DN.SnS_DuskspireFaught");

            RecalculateAether();

            ext.mana.Value = ext.maxMana.Value;
            ext.armorUsed.Value = 0;
            State.HasCraftedFree = false;

            if (Game1.player.GetCustomSkillLevel(ModTOP.SorcerySkill) >= 4 && !Game1.player.knowsRecipe("DN.SnS_TeleportCircle"))
                Game1.player.craftingRecipes.Add("DN.SnS_TeleportCircle", 1);

            if (Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value)
            {
                if (!Game1.player.knowsRecipe("DN.SnS_Bullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_Bullet", 0);
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

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Spells.CurrPositioningAbil != null)
            {
                if (Game1.didPlayerJustLeftClick())
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.SorcerySkill, Spells.CurrPositioningAbil.XP * Spells.WitchcraftExpMultiplier);
                    Spells.CastSpell(Spells.CurrPositioningAbil.Ability, Spells.CurrPositioningAbil.SpellColor, Spells.CurrPositioningAbil.OnCast);
                }
                else if (Game1.didPlayerJustRightClick())
                    Spells.CurrPositioningAbil = null;
            }

            bool Difficulty = Game1.getFarm().grandpaScore.Value == 4;
            if (Config.LltkDifficulty.EndsWithIgnoreCase("Medium") || Config.LltkDifficulty.EndsWithIgnoreCase("Hard"))
                Difficulty = Difficulty && Game1.player.achievements.Contains(42);

            if (Config.LltkDifficulty.EndsWithIgnoreCase("Hard"))
                Difficulty = Difficulty && Utility.percentGameComplete() >= 0.5;

            if (e.Button.IsActionButton() && Game1.currentLocation is Farm farm &&
                e.Cursor.GrabTile == farm.GetGrandpaShrinePosition().ToVector2() &&
                !Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value &&
                Difficulty)
            {
                Game1.player.addItemByMenuIfNecessaryElseHoldUp(new MeleeWeapon("DN.SnS_longlivetheking") { specialItem = true});
                Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value = true;

                if (!Game1.player.knowsRecipe("DN.SnS_Bullet"))
                    Game1.player.craftingRecipes.Add("DN.SnS_Bullet", 0);
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

                Game1.player.mailReceived.Add("DN.SnS_ObtainedLLTK");
            }

            if (Config.LltkToggleRightClick)
            {
                if (Context.IsWorldReady && Context.IsPlayerFree && e.Button.IsActionButton() && Game1.player.ActiveItem != null)
                {
                    if (Game1.currentLocation.GetTilePropertySplitBySpaces("Action", "Buildings", (int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y).Length == 0)
                    {
                        if (SwapLltk())
                        {
                            Helper.Input.Suppress(e.Button);
                        }
                    }
                }
            }

            var ext = Game1.player.GetFarmerExtData();
            bool hasBar = Game1.onScreenMenus.Any(m => m is AdventureBar);
            if (hasBar && Game1.activeClickableMenu == null && !Game1.IsChatting)
            {
                KeybindList[][] binds =
                [
                    [ Config.AbilityBar1Slot1, Config.AbilityBar2Slot1 ],
                    [ Config.AbilityBar1Slot2, Config.AbilityBar2Slot2 ],
                    [ Config.AbilityBar1Slot3, Config.AbilityBar2Slot3 ],
                    [ Config.AbilityBar1Slot4, Config.AbilityBar2Slot4 ],
                    [ Config.AbilityBar1Slot5, Config.AbilityBar2Slot5 ],
                    [ Config.AbilityBar1Slot6, Config.AbilityBar2Slot6 ],
                    [ Config.AbilityBar1Slot7, Config.AbilityBar2Slot7 ],
                    [ Config.AbilityBar1Slot8, Config.AbilityBar2Slot8 ],
                ];

                string abilId = null;
                for (int islot = 0; islot < 8; ++islot)
                {
                    if (abilId != null) break;
                    
                    for (int i = 0; i < 2; i++)
                    {
                        if (abilId != null) break;
                        if (binds[islot][i].JustPressed())
                            abilId = ext.adventureBar[i * 8 + islot];
                    }
                }

                if (abilId != null && Ability.Abilities.TryGetValue(abilId, out var abil) && abil.ManaCost() <= ext.mana.Value && abil.CanUse())
                {
                    CastAbility(abil);
                }
            }

            if (Config.ShieldThrowMethod != "Weapon Special" && State.ThrowCooldown <= 0 && Config.ShieldThrowKeybind.JustPressed())
            {
                if (Game1.player.CurrentTool is MeleeWeapon weapon && weapon.IsShieldItem())
                    ThrowShield(weapon);
                if (Game1.player.GetOffhand().IsShieldItem())
                    ThrowShield(Game1.player.GetOffhand());
            }
        }

        private static bool ThrowShield(MeleeWeapon __instance)
        {
            if (!__instance.IsShieldItem() || __instance.lastUser != Game1.player)
                return false;

            Vector2 diff = ModSnS.Instance.Helper.Input.GetCursorPosition().AbsolutePixels - Game1.player.StandingPixel.ToVector2();
            if (diff.Length() > 0 && diff.Length() > 8 * Game1.tileSize)
            {
                diff.Normalize();
                diff = diff * 8 * Game1.tileSize;
            }
            if (diff.Length() < Game1.tileSize || Game1.options.gamepadControls)
            {
                Vector2[] facings = [-Vector2.UnitY, Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX];
                diff = facings[Game1.player.FacingDirection] * Game1.tileSize * 8;
            }
            Vector2 target = Game1.player.Position + diff;
            float damageMult = 0.5f;
            int bounceCount = 1;
            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldThrowHit2))
            {
                ++bounceCount;
            }
            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldThrowHit3))
            {
                ++bounceCount;
                damageMult = 1;
            }
            State.MyThrown.Add(new ThrownShield(Game1.player, (int)((__instance.minDamage.Value + __instance.maxDamage.Value) / 2 * damageMult), target, 15, __instance.QualifiedItemId, bounceCount));
            Game1.currentLocation.projectiles.Add(State.MyThrown.Last());

            State.ThrowCooldown = 3500;
            if (__instance.lastUser?.professions.Contains(Farmer.acrobat) ?? false)
                State.ThrowCooldown /= 2;
            if (__instance.hasEnchantmentOfType<ArtfulEnchantment>())
                State.ThrowCooldown /= 2;

            Game1.player.playNearbySoundLocal("daggerswipe");

            AnimatedSprite.endOfAnimationBehavior endOfAnimFunc = __instance.triggerDefenseSwordFunction;
            switch (__instance.lastUser.FacingDirection)
            {
                case 0:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(252, 250, 1, endOfAnimFunc);
                    __instance.Update(0, 0, __instance.lastUser);
                    break;
                case 1:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(243, 250, 1, endOfAnimFunc);
                    __instance.Update(1, 0, __instance.lastUser);
                    break;
                case 2:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(234, 250, 1, endOfAnimFunc);
                    __instance.Update(2, 0, __instance.lastUser);
                    break;
                default:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(259, 250, 1, endOfAnimFunc);
                    __instance.Update(3, 0, __instance.lastUser);
                    break;
            }

            Instance.Helper.Reflection.GetMethod(__instance, "beginSpecialMove").Invoke(__instance.lastUser);

            return true;
        }

        private static bool SwapLltk()
        {
            if (Game1.player.ActiveItem == null)
                return false;

            if (Game1.player.ActiveItem.QualifiedItemId.EqualsIgnoreCase("(W)DN.SnS_longlivetheking"))
            {
                var w = new Slingshot("DN.SnS_longlivetheking_gun");
                Game1.player.CurrentTool.CopyEnchantments(Game1.player.CurrentTool, w);
                w.modData.Set(Game1.player.CurrentTool.modData.Pairs);
                w.AttachmentSlotsCount = 2;
                for (int i = 0; i < 2; i++)
                {
                    if (Game1.player.CurrentTool.attachments[i] != null)
                    {
                        w.attachments[i] = Game1.player.CurrentTool.attachments[i];
                    }
                }
                Game1.player.Items[Game1.player.CurrentToolIndex] = w;
                return true;
            }
            else if (Game1.player.ActiveItem.QualifiedItemId.EqualsIgnoreCase("(W)DN.SnS_longlivetheking_gun"))
            {
                var w = new MeleeWeapon("DN.SnS_longlivetheking");
                Game1.player.CurrentTool.CopyEnchantments(Game1.player.CurrentTool, w);
                w.modData.Set(Game1.player.CurrentTool.modData.Pairs);
                w.AttachmentSlotsCount = 2;
                for (int i = 0; i < 2; i++)
                {
                    if (Game1.player.CurrentTool.attachments[i] != null)
                    {
                        w.attachments[i] = Game1.player.CurrentTool.attachments[i];
                    }
                }
                Game1.player.Items[Game1.player.CurrentToolIndex] = w;
                return true;
            }
            return false;
        }

        private void Display_RenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch1.Val.12") ||
                 Game1.player.team.acceptedSpecialOrderTypes.Contains("SwordSorcery") ||
                 Game1.eventUp)
            {
                return;
            }

            if (Game1.currentLocation.NameOrUniqueName == "EastScarp_Village")
            {
                Vector2 tile = new(24.4f, 82);
                float yOffset = 4 * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250f), 2);
                e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * Game1.tileSize + Game1.pixelZoom, tile.Y * Game1.tileSize - Game1.tileSize + Game1.tileSize / 8 + yOffset)), new Rectangle(395, 497, 3, 8), Color.White, 0f, new Vector2(1, 4), Game1.pixelZoom + Math.Max(0, .25f - yOffset / 8f), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            }
        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/Duskspire"))
                e.LoadFromModFile<Texture2D>("assets/duskspire-behemoth.png", AssetLoadPriority.High);

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/AlchemyRecipes"))
                e.LoadFrom(() => new Dictionary<string, AlchemyData>(), AssetLoadPriority.Exclusive);

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/FinalePartners"))
                e.LoadFrom(() => new Dictionary<string, FinalePartnerInfo>(), AssetLoadPriority.Exclusive);

            string[] recolors =
            [
                "daisyniko.earthyinterface",
                "shinchan.cppurpleinterface",
                "enteis.woodeninterfeis",
                "thefrenchdodo.sakurainterfaceredux",
                "nom0ri.vintageuifix",
                "Sqbr.StarryBlueUI",
                "Bos.UIInterface",
                "VinillaBean.LavenderDreams",
                "silvermoonchan.PurpleGalaxyUI",
                "ManaKirel.VintageInterface2",
                "notbelovely.CAccent",
                "Slime.SlimeUI"
            ];

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/ArmorSlot"))
            {
                string ArmorSlot = "assets/armor-bg.png";
                foreach (var recolor in recolors)
                {
                    if (Helper.ModRegistry.IsLoaded(recolor) && File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "armor-bg", (recolor == recolors[9] ? recolors[4] : recolor) + ".png")))
                    {
                        ArmorSlot = $"assets/armor-bg/{(recolor == recolors[9] ? recolors[4] : recolor)}.png";
                    }
                }
                e.LoadFromModFile<Texture2D>(ArmorSlot, AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/OffhandSlot"))
            {
                string OffhandSlot = "assets/offhand-bg.png";
                foreach (var recolor in recolors)
                {
                    if (Helper.ModRegistry.IsLoaded(recolor) && File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "armor-bg", (recolor == recolors[9] ? recolors[4] : recolor) + "_offhand.png")))
                    {
                        OffhandSlot = $"assets/armor-bg/{(recolor == recolors[9] ? recolors[4] : recolor)}_offhand.png";
                    }
                }
                e.LoadFromModFile<Texture2D>(OffhandSlot, AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Textures/DN.SnS/ForgeButton"))
            {
                string ForgeButton = "assets/ForgeButton.png";
                foreach (var recolor in recolors)
                {
                    if (Helper.ModRegistry.IsLoaded(recolor) && File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "forgeButton", (recolor == recolors[9] ? recolors[4] : recolor) + "_offhand.png")))
                    {
                        ForgeButton = $"assets/forgeButton/{(recolor == recolors[9] ? recolors[4] : recolor)}_ForgeButton.png";
                    }
                }
                e.LoadFromModFile<Texture2D>(ForgeButton, AssetLoadPriority.Exclusive);
            }
        }

        private double Perc;
        private double wait = 0;

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm != null)
            {
                gmcm.Register(ModManifest, () => Config = new(), () => Helper.WriteConfig(Config));

                gmcm.AddSectionTitle(ModManifest, I18n.Config_Section_Balancing);
                gmcm.AddNumberOption(ModManifest, () => Config.MonsterHealthBuff, (val) => Config.MonsterHealthBuff = val, I18n.Config_MonsterHealthBuff_Name, I18n.Config_MonsterHealthBuff_Description, 1.0f, 3.0f, 0.05f, f => ((int)((f - 1.0) * 100)).ToString());

                gmcm.AddSectionTitle(ModManifest, I18n.Section_AetherBar_Name, I18n.Section_AetherBar_Description);
                gmcm.AddNumberOption(ModManifest, () => Config.Red, (val) => Config.Red = val, I18n.Int_Red_Name, I18n.Int_Red_Descripion, 0, 255);
                gmcm.AddNumberOption(ModManifest, () => Config.Blue, (val) => Config.Blue = val, I18n.Int_Blue_Name, I18n.Int_Blue_Descripion, 0, 255);
                gmcm.AddNumberOption(ModManifest, () => Config.Green, (val) => Config.Green = val, I18n.Int_Green_Name, I18n.Int_Green_Descripion, 0, 255);
                gmcm.AddNumberOption(ModManifest, () => Config.TextRed, (val) => Config.TextRed = val, I18n.Int_TextRed_Name, I18n.Int_TextRed_Descripion, 0, 255);
                gmcm.AddNumberOption(ModManifest, () => Config.TextBlue, (val) => Config.TextBlue = val, I18n.Int_TextBlue_Name, I18n.Int_TextBlue_Descripion, 0, 255);
                gmcm.AddNumberOption(ModManifest, () => Config.TextGreen, (val) => Config.TextGreen = val, I18n.Int_TextGreen_Name, I18n.Int_TextGreen_Descripion, 0, 255);
                gmcm.AddComplexOption(ModManifest, I18n.String_ManabarPeview, (b, pos) =>
                {
                    var ext = Game1.player?.GetFarmerExtData();
                    double x;

                    if (wait <= 0)
                    {
                        Perc = x = Game1.random.NextDouble();
                        wait = 5000;
                    }
                    else
                    {
                        x = Perc;
                        wait -= Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
                    }

                    double perc = x;
                    string manaStr = $"{MathF.Round((float)(10 * x), mode: MidpointRounding.ToPositiveInfinity)}/10";
                    IClickableMenu.drawTextureBox(b, (int)pos.X, (int)pos.Y, 64 * 4 + 24, 32 + 12 + 12, Color.White);
                    b.Draw(Game1.staminaRect, new Rectangle((int)pos.X + 12, (int)pos.Y + 12, (int)(64 * 4 * perc), 32), Utility.StringToColor($"{ModSnS.Config.Red} {ModSnS.Config.Green} {ModSnS.Config.Blue}") ?? Color.Aqua);
                    b.DrawString(Game1.smallFont, manaStr, new Vector2(pos.X + 12 + 64 * 4 / 2 - Game1.smallFont.MeasureString(manaStr).X / 2, (int)pos.Y + 12), Utility.StringToColor($"{ModSnS.Config.TextRed} {ModSnS.Config.TextGreen} {ModSnS.Config.TextBlue}") ?? Color.Black);
                }, height: () => 56);

                gmcm.AddSectionTitle(ModManifest, I18n.Config_Section_Lltk);
                gmcm.AddParagraph(ModManifest, I18n.Config_Section_Lltk_Text);
                gmcm.AddBoolOption(ModManifest, () => Config.LltkToggleRightClick, (val) => Config.LltkToggleRightClick = val, I18n.Config_LltkToggleRightClick_Name, I18n.Config_LltkToggleRightClick_Description);
                gmcm.AddKeybindList(ModManifest, () => Config.LltkToggleKeybind, (val) => Config.LltkToggleKeybind = val, I18n.Config_LltkToggleKeybind_Name, I18n.Config_LltkToggleKeybind_Description);
                gmcm.AddTextOption(ModManifest, () => Config.LltkDifficulty, (val) => Config.LltkDifficulty = val, I18n.Config_LltkDifficulty_Name, I18n.Config_LltkDifficulty_Description, ["Easy", "Medium", "Hard"]);

                gmcm.AddSectionTitle(ModManifest, I18n.Section_Keybinds_Name, I18n.Section_Keybinds_Description);
                gmcm.AddTextOption(ModManifest, () => Config.ShieldThrowMethod, (val) => Config.ShieldThrowMethod = val, I18n.Keybind_ShieldThrowMethod_Name, I18n.Keybind_ShieldThrowMethod_Description, ["Weapon Special", "Both", "Keybind"]);
                gmcm.AddKeybindList(ModManifest, () => Config.ShieldThrowKeybind, (val) => Config.ShieldThrowKeybind = val, I18n.Keybind_ShieldThrowKeybind_Name, I18n.Keybind_ShieldThrowKeybind_Description);
                gmcm.AddParagraph(ModManifest, () => "");
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
            }




            StarControl = Helper.ModRegistry.GetApi<IStarControlApi>("focustense.StarControl");
            StarControl?.RegisterCustomMenuPage(ModManifest, "AdventureBar", new AdventureBarStarControlPageFactory());

            Skills.RegisterSkill(RogueSkill = new RogueSkill());

            SpaceCore = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            SpaceCore.RegisterSerializerType(typeof(ThrownShield));
            SpaceCore.RegisterCustomProperty(typeof(Farmer), "shieldSlot", typeof(NetRef<Item>), AccessTools.Method(typeof(Farmer_ArmorSlot), nameof(Farmer_ArmorSlot.get_armorSlot)), AccessTools.Method(typeof(Farmer_ArmorSlot), nameof(Farmer_ArmorSlot.set_armorSlot)));
            SpaceCore.RegisterCustomProperty(typeof(Farmer), "takenLoreWeapon", typeof(NetBool), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.HasTakenLoreWeapon)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetHasTakenLoreWeapon)));
            SpaceCore.RegisterCustomProperty(typeof(Farmer), "adventureBar", typeof(NetArray<string, NetString>), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.GetAdventureBar)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetAdventureBar)));
            SpaceCore.RegisterCustomProperty(typeof(Farmer), "maxMana", typeof(NetInt), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.GetMaxMana)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetMaxMana)));
            SpaceCore.RegisterCustomProperty(typeof(Farmer), "expRemainderRogue", typeof(NetFloat), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderRogueGetter)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderRogueSetter)));

            SpaceCore.RegisterEquipmentSlot(ModManifest,
                $"{ModManifest.UniqueID}_Armor",
                item => item == null || (item.IsArmorItem() && item is not MeleeWeapon),
                I18n.UiSlot_Armor,
                Game1.content.Load<Texture2D>("DN.SnS/ArmorSlot"));

            SpaceCore.RegisterEquipmentSlot(ModManifest,
                $"{ModManifest.UniqueID}_Offhand",
                item => item == null || (item is MeleeWeapon mw && !mw.isScythe()),
                I18n.UiSlot_Offhand,
                Game1.content.Load<Texture2D>("DN.SnS/OffhandSlot"));

            SpaceCore.RegisterSpawnableMonster("Skull", (pos, data) =>
            {
                Bat Skull = new(pos);
                Skull.reloadSprite();
                Helper.Reflection.GetField<float>(Skull, "extraVelocity").SetValue(3);
                Helper.Reflection.GetField<float>(Skull, "maxSpeed").SetValue(8);
                Skull.shakeTimer = 100;
                Skull.cursedDoll.Value = true;
                Skull.hauntedSkull.Value = true;
                return Skull;
            });

            SpaceCore.RegisterSpawnableMonster("MagmaSprite", (pos, data) =>
            {
                Bat MagmaSprite = new(pos);
                MagmaSprite.reloadSprite();
                if (!data.TryGetValue("Sparkler", out _))
                {
                    MagmaSprite.Slipperiness *= 2;
                    Helper.Reflection.GetField<float>(MagmaSprite, "maxSpeed").SetValue(Game1.random.Next(3, 9));
                }
                else
                {
                    MagmaSprite.Slipperiness += 3;
                    Helper.Reflection.GetField<float>(MagmaSprite, "maxSpeed").SetValue(Game1.random.Next(3, 8));
                    MagmaSprite.canLunge.Value = true;
                }
                Helper.Reflection.GetField<float>(MagmaSprite, "extraVelocity").SetValue(2);
                MagmaSprite.shakeTimer = 100;
                MagmaSprite.cursedDoll.Value = true;
                MagmaSprite.magmaSprite.Value = true;

                return MagmaSprite;
            });

            var CP = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            if (CP != null)
            {
                CP.RegisterToken(ModManifest, "PocketDimensionUpgrade", () =>
                {
                    Farmer player;

                    if (Context.IsWorldReady)
                        player = Game1.player;
                    else if (SaveGame.loaded?.player != null)
                        player = SaveGame.loaded.player;
                    else
                        return null;

                    int i = 1;
                    if (Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Witchcraft") >= 10)
                        i++;
                    if (Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Bardics") >= 10)
                        i++;
                    if (Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Druidics") >= 10)
                        i++;
                    if (Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Paladin") >= 10)
                        i++;
                    if (Game1.player.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Rogue") >= 10)
                        i++;

                    if (i++ > 4) i = 4;

                    return [$"{i}"];
                });

                CP.RegisterToken(ModManifest, "CirrusHair", () =>
                {
                    if (!Context.IsWorldReady)
                        return null;

                    if (Game1.stats.Get("CirrusCooldown") == 0 && Game1.random.NextBool(1 / 3))
                    {
                        Game1.stats.Increment("CirrusCooldown", 6);
                        Game1.stats.Increment("CirrusHair", 1);
                    }
                    else if (Game1.stats.Get("CirrusCooldown") != 0)
                    {
                        Game1.stats.Increment("CirrusCooldown", -1);
                    }

                    Random r = Utility.CreateRandom(Game1.hash.GetDeterministicHashCode("CirrusHair"), Game1.uniqueIDForThisGame, Game1.stats.Get("CirrusHair"));

                    return r.Next(0, 5) switch
                    {
                        0 => ["Brown"],
                        1 => ["Red"],
                        2 => ["Orange"],
                        3 => ["Yellow"],
                        _ => ["Blue"],
                    };
                });

                CP.RegisterToken(ModManifest, "HorseName", () =>
                {
                    Farmer player;

                    if (Context.IsWorldReady)
                        player = Game1.player;
                    else if (SaveGame.loaded?.player != null)
                        player = SaveGame.loaded.player;
                    else
                        return null;

                    string playerhorsename = Game1.player.horseName.Value;

                    if (playerhorsename == null)
                        return [I18n.Cptoken_Horse()];

                    else
                        return [$"{playerhorsename}"];

                });

                CP.RegisterToken(ModManifest, "PaladinUnlocked", () =>
                {
                    if (!Context.IsWorldReady)
                        return null;

                    return [ModTOP.PaladinSkill.ShouldShowOnSkillsPage.ToString()];
                });

                CP.RegisterToken(ModManifest, "SpecialOrderBoardBook", () =>
                {
                    if (!Context.IsWorldReady)
                        return null;
                    List<string> possibleBooks = [];
                    if (RogueSkill.ShouldShowOnSkillsPage)
                        possibleBooks.Add("artificerbook");
                    if (ModCoT.DruidSkill.ShouldShowOnSkillsPage)
                        possibleBooks.Add("druidbook");
                    if (ModUP.BardSkill.ShouldShowOnSkillsPage)
                        possibleBooks.Add("bardbook");
                    if (ModTOP.SorcerySkill.ShouldShowOnSkillsPage)
                        possibleBooks.Add("sorcerybook");
                    if (ModTOP.PaladinSkill.ShouldShowOnSkillsPage)
                        possibleBooks.Add("paladinbook");

                    if (possibleBooks.Count > 0)
                        return [$"DN.SnS_{possibleBooks[Game1.random.Next(possibleBooks.Count)]}"];
                    return ["SkillBook_4"];
                });
            }

            // This late because of accessing SpaceCore's local variable API
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            Harmony.Patch(AccessTools.Constructor(typeof(CharacterCustomization), [typeof(CharacterCustomization.Source), typeof(bool)]),
                postfix: new HarmonyMethod(typeof(SkillSelectMenu.CharacterCustomizationPatch1), nameof(SkillSelectMenu.CharacterCustomizationPatch1.Postfix)));

            // This is like, don't, don't do this normally kids, like please don't. Don't harmony patch other mods. It's fragile, and stupid, but this was the easy way out and I like me some shortcuts.
            if (Helper.ModRegistry.IsLoaded("leclair.bettercrafting"))
            {
                Harmony.Patch(
                    AccessTools.TypeByName("Leclair.Stardew.Common.CraftingHelper").GetMethod("ConsumeIngredients"),
                    prefix: new HarmonyMethod(typeof(CraftingRecipeFlashOfGeniusPatch), nameof(CraftingRecipeFlashOfGeniusPatch.Prefix))
                    );
            }
        }

        private void GameLoop_UpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            if (!Game1.player.mailReceived.Contains("DN.SnS_IntermissionShield") && Game1.player.eventsSeen.Any(m => m.StartsWith("SnS.Ch4.Victory")))
            {
                Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(W)DN.SnS_PaladinShield", 1, 0, false));
                Game1.addMail("DN.SnS_IntermissionShield", true, false);
            }

            if (Game1.currentLocation != null && Game1.currentLocation.Name == "EastScarp_DuskspireBehemoth" && Game1.getOnlineFarmers().Any(f => f != Game1.player && f.GetFarmerExtData().DoingFinale.Value))
            {
                Game1.warpFarmer("EastScarp_DeepDarkEntrance", 17, 7, 2);
                Game1.addHUDMessage(new(I18n.DuskspireTeleportOut()));
            }

            if (State.DoFinale || State.FinaleBoss != null)
            {
                if (State.FinaleBoss == null && Game1.CurrentEvent == null && Game1.locationRequest == null)
                {
                    if (!Game1.player.mailReceived.Contains("DN.SnS_IntermissionShield"))
                    {
                        Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(W)DN.SnS_PaladinShield", 1, 0, false));
                        Game1.addMail("DN.SnS_IntermissionShield", true, false);
                    }
                    Game1.getLocationFromName("EastScarp_DuskspireLair").characters.Add(State.FinaleBoss = new DuskspireMonster(new Vector2(18, 13) * Game1.tileSize));

                    DelayedAction.playMusicAfterDelay("SnS.DuskspirePhase2", 500, true);

                    string partner = null;
                    var partnerInfos = Game1.content.Load<Dictionary<string, FinalePartnerInfo>>("DN.SnS/FinalePartners");

                    foreach (string key in partnerInfos.Keys)
                    {
                        if (Game1.player.friendshipData.TryGetValue(key, out var data) && data.IsDating())
                        {
                            partner = key;
                            break;
                        }
                    }
                    foreach (string key in partnerInfos.Keys)
                    {
                        if (Game1.player.friendshipData.TryGetValue(key, out var data) && data.IsRoommate())
                        {
                            partner = key;
                            break;
                        }
                    }

                    if (partner == null)
                    {
                        if (Game1.player.hasOrWillReceiveMail("FarmerGuildmasterBattle"))
                        {
                            partner = "Val";
                        }
                    }

                    if (partner != null)
                    {
                        Game1.player.GetCurrentMercenaries().Add(new Mercenary(partner, Game1.player.Position));
                    }

                    State.DoFinale = false;
                }
                if (State.FinaleBoss != null)
                {
                    if (Game1.player.health <= 0)
                    {
                        State.FinaleBoss.currentLocation.characters.Remove(State.FinaleBoss);
                        State.FinaleBoss = null;

                        Game1.player.GetCurrentMercenaries().Clear();
                    }
                    else
                    {
                        if (Game1.locationRequest != null)
                        {
                            Game1.locationRequest = null;
                            Helper.Reflection.GetField<bool>(typeof(Game1), "_isWarping").SetValue(false);
                            var fade = Helper.Reflection.GetField<ScreenFade>(typeof(Game1), "screenFade").GetValue();
                            fade.globalFade = false;
                            fade.fadeToBlack = false;
                            fade.fadeIn = false;
                            fade.fadeToBlackAlpha = 0;
                            Game1.player.CanMove = true;

                            Game1.addHUDMessage(new HUDMessage(I18n.CannotWarp()));
                        }
                    }
                }
            }

            if (State.MyThrown.Count > 0)
            {
                foreach (var thrown in State.MyThrown.ToList())
                {
                    if ((thrown.GetPosition() - thrown.Target.Value).Length() < 16 && (thrown.Bounces.Value <= 0 || thrown.TargetMonster.Get(Game1.player.currentLocation) == null))
                    {
                        var playerPos = Game1.player.getStandingPosition();
                        playerPos.X -= 16;
                        playerPos.Y -= 64;
                        thrown.Target.Value = playerPos;
                        if ((thrown.GetPosition() - playerPos).Length() < 16)
                        {
                            thrown.Dead = true;
                        }
                    }
                    if (thrown.Dead)
                        State.MyThrown.Remove(thrown);
                }
            }
            
            if (State.ThrowCooldown > 0 && Game1.activeClickableMenu == null)
                State.ThrowCooldown = MathF.Max(0, State.ThrowCooldown - (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds);

            if (State.BlockCooldown > 0 && Game1.activeClickableMenu == null)
                State.BlockCooldown = MathF.Max(0, State.BlockCooldown - (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds);

            // ---

            if (Game1.player.GetFarmerExtData().inShadows.Value)
            {
                var b = Game1.player.buffs.AppliedBuffs.FirstOrDefault(pair => pair.Key == "shadowstep").Value;
                if (b == null)
                {
                    b = new Buff(
                        "shadowstep",
                        duration: 250,
                        displayName: I18n.Ability_Shadowstep_Name(),
                        effects: new() { CriticalChanceMultiplier = { 999f } },
                        iconTexture: Game1.content.Load<Texture2D>(Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name),
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

            if (Config.ToggleAdventureBar.JustPressed() && Game1.activeClickableMenu == null && Game1.player.hasOrWillReceiveMail("SnS_AdventureBar"))
            {
                AdventureBar.Hide = !AdventureBar.Hide;
                if (AdventureBar.Hide && hasBar)
                    Game1.onScreenMenus.Remove(Game1.onScreenMenus.Where(m => m is AdventureBar).First());
            }
            else if (Config.ConfigureAdventureBar.JustPressed() && Game1.activeClickableMenu == null && !Game1.IsChatting &&
                 Game1.player.hasOrWillReceiveMail("SnS_AdventureBar"))
            {
                Game1.activeClickableMenu = new AdventureBarConfigureMenu();
            }
            if (e.IsOneSecond && !hasBar && !AdventureBar.Hide && Game1.player.hasOrWillReceiveMail("SnS_AdventureBar"))
            {
                Game1.onScreenMenus.Add(new AdventureBar(editing: false));
            }

            SpaceCore = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");

            if (RogueSkill.ShouldShowOnSkillsPage && Game1.player.GetCustomSkillLevel(RogueSkill) < 1)
            {
                SpaceCore.AddExperienceForCustomSkill(Game1.player, RogueSkill.Id, 100);
            }

            if (ModCoT.DruidSkill.ShouldShowOnSkillsPage && Game1.player.GetCustomSkillLevel(ModCoT.DruidSkill) < 1)
            {
                SpaceCore.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Druidics", 100);
            }

            if (ModUP.BardSkill.ShouldShowOnSkillsPage && Game1.player.GetCustomSkillLevel(ModUP.BardSkill) < 1)
            {
                SpaceCore.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Bardics", 100);
            }

            if (ModTOP.SorcerySkill.ShouldShowOnSkillsPage && Game1.player.GetCustomSkillLevel(ModTOP.SorcerySkill) < 1)
            {
                SpaceCore.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Witchcraft", 100);
            }

            if (ModTOP.PaladinSkill.ShouldShowOnSkillsPage && Game1.player.GetCustomSkillLevel(ModTOP.PaladinSkill) < 1)
            {
                SpaceCore.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Paladin", 100);
            }

            if (Config.LltkToggleKeybind.JustPressed())
            {
                SwapLltk();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CastAbility(Ability abil)
        {
            abil.Function();
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (State.MyThrown.Count > 0)
            {
                foreach (var entry in State.MyThrown)
                    entry.Dead = true;
                State.MyThrown.Clear();
            }

            var ext = Game1.player.GetFarmerExtData();
            int? armorAmount = Game1.player.GetArmorItem().GetArmorAmount();
            if (State.CanRepairArmor && Game1.player.HasCustomProfession(RogueSkill.ProfessionArmorRecovery) && armorAmount.HasValue)
            {
                State.CanRepairArmor = false;
                ext.armorUsed.Value = Math.Max(0, ext.armorUsed.Value - armorAmount.Value / 5);
            }

            if (e.NewLocation.Name == "EastScarp_DuskspireLair" && !e.NewLocation.modData.ContainsKey("DN.SnS_DuskspireFaught") && Game1.player.eventsSeen.Contains("SnS.Ch4.Finale") && !e.NewLocation.characters.Any(m => m.Name == "Duskspire Remnant"))
            {
                e.NewLocation.characters.Add(new DuskspireMonster(new Vector2(18, 13) * Game1.tileSize, "Duskspire Remnant"));
            }
        }

        private void Display_RenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.CurrentEvent != null)
                return;

            if (Spells.CurrPositioningAbil != null)
            {
                var tex = Game1.content.Load<Texture2D>(Spells.CurrPositioningAbil.Ability.TexturePath);
                e.SpriteBatch.Draw(tex, Game1.getMousePosition().ToVector2() + new Vector2(16, 16), Game1.getSquareSourceRectForNonStandardTileSheet(tex, 16, 16, Spells.CurrPositioningAbil.Ability.SpriteIndex), Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1);
            }

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

            var armorAmt = Game1.player.GetArmorItem().GetArmorAmount();
            if ((armorAmt ?? -1) >= 0 && Game1.showingHealth)
            {
                float modifier = 0.625f;
                Vector2 topOfBar = new(Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Right - 48 - 8, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 224 - 16 - (int)((float)(Game1.player.MaxStamina - 270) * modifier));
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

                string str = Math.Max(0, (armorAmt - Game1.player.GetFarmerExtData().armorUsed.Value) ?? 0) + $"/{armorAmt}";
                Vector2 size = Game1.smallFont.MeasureString(str);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(-2, 0), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(2, 0), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(0, -2), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(0, 2), Color.Black, 0, size / 2, 1, SpriteEffects.None, 1);
                e.SpriteBatch.DrawString(Game1.smallFont, str, spot - new Vector2(0, 0), Color.White, 0, size / 2, 1, SpriteEffects.None, 1);
            }
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.withinPlayerThreshold), [typeof(int)])]
    public static class NpcShadowstepPatch
    {
        public static void Postfix(NPC __instance, ref bool __result )
        {
            if ( __instance is Monster && Game1.player.GetFarmerExtData().inShadows.Value)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.leftClick))]
    public static class WeaponSwingShadowstepPatch
    {
        public static void Postfix(Farmer who)
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
            if (__instance.questKey.Value.StartsWith("CAGQuest.UntimedSpecialOrder") || __instance.questKey.Value == "Val.SpecialOrders.BuildGuild")
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
                SpecialOrder.UpdateAvailableSpecialOrders("SwordSorcery", forceRefresh);
            }
        }
    }

    [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.SetDuration))]
    public static class SpecialOrderUntimedQuestsPatch2
    {
        public static bool Prefix(SpecialOrder __instance)
        {
            // I don't know why this is necessary
            if (__instance.questKey.Value.StartsWith("CAGQuest.UntimedSpecialOrder") || __instance.questKey.Value == "Val.SpecialOrders.BuildGuild")
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
            List<string> npc = ["Val", "Hector", "Cirrus", "Dandelion", "Roslin"];
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
            if (__instance.GetFarmerExtData().stasisTimer.Value > 0)
            {
                return false;
            }

            if (__instance.HasCustomProfession(SorcerySkill.ProfessionAetherBuff) && __instance.CanBeDamaged() && __instance.GetFarmerExtData().maxMana.Value > __instance.GetFarmerExtData().mana.Value)
                __instance.GetFarmerExtData().mana.Value += (int)MathF.Min(Game1.random.Next(5,10), __instance.GetFarmerExtData().maxMana.Value - __instance.GetFarmerExtData().mana.Value);

            int ArmorAmount = __instance.GetArmorItem().GetArmorAmount() ?? -1;
            var ext = __instance.GetFarmerExtData();
            bool num = damager != null && !damager.isInvincible() && !overrideParry;
            bool flag = (damager == null || !damager.isInvincible()) && (damager == null || (damager is not GreenSlime && damager is not BigSlime) || !__instance.isWearingRing("520"));
            bool playerParryable = __instance.CurrentTool is MeleeWeapon weapon && weapon.isOnSpecial && (int)weapon.type.Value == 3;

            if (num && playerParryable)
                return true;

            if (__instance != Game1.player ||  overrideParry || !__instance.CanBeDamaged() || !flag)
                return true;

            if (ArmorAmount > 0 && ext.armorUsed.Value < ArmorAmount)
            {
                __instance.playNearbySoundAll("parry");
                ext.armorUsed.Value = Math.Min(ArmorAmount, ext.armorUsed.Value + damage);
                damager?.parried(0, __instance);
                __instance.temporarilyInvincible = true;
                __instance.flashDuringThisTemporaryInvincibility = true;
                __instance.temporaryInvincibilityTimer = 0;
                __instance.currentTemporaryInvincibilityDuration = 1200 + __instance.GetEffectsOfRingMultiplier("861") * 400;
            }
            else if (ext.mirrorImages.Value != 0 && Game1.random.Next(ext.mirrorImages.Value + 1) != 0)
            {
                Vector2 spot = Game1.player.StandingPixel.ToVector2();
                float rad = (float)-Game1.currentGameTime.TotalGameTime.TotalSeconds / 3 * 2;
                rad += MathF.PI * 2 / 3 * (ext.mirrorImages.Value - 1);
                spot += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);

                ext.mirrorImages.Value -= 1;
                for (int i = 0; i < 8; ++i)
                {
                    Vector2 diff = new(Game1.random.Next(96) - 48, Game1.random.Next(96) - 48);
                    __instance.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, spot - new Vector2(32, 48) + diff, flicker: false, flipped: false));
                }
                __instance.playNearbySoundAll("coldSpell");
                __instance.temporarilyInvincible = true;
                __instance.flashDuringThisTemporaryInvincibility = true;
                __instance.temporaryInvincibilityTimer = 0;
                __instance.currentTemporaryInvincibilityDuration = 1200 + __instance.GetEffectsOfRingMultiplier("861") * 400;
            }

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

    [HarmonyPatch(typeof(GameLocation), "resetLocalState")]
    public static class DeepDarkDungeonAdventurersPatch
    {
        public static void Postfix(GameLocation __instance)
        {
            if (Game1.IsMultiplayer)
                return;
            if (__instance.mapPath.Value != "Maps/DeepDark5")
                return;
            if (Game1.player.hasOrWillReceiveMail("DuskspireDefeated"))
                return;

            Point basePoint = new(28, 7);
            Dictionary<string, (string sprite, string portrait, string dialogue)> data = new()
            {
                { "Val", new( "Armor_Mateo", "Armor_Mateo", I18n.FakeNpc_Deepdark_MateoMine() ) },
                { "Hector", new( "Hector_HoodDown", "Hector_HoodDown", I18n.FakeNpc_Deepdark_HectorMine() ) },
                { "Cirrus", new( "Cirrus_Glamrock", "Cirrus_Glamrock", I18n.FakeNpc_Deepdark_CirrusMine() ) },
                { "Dandelion", new( "Dandelion_armored", "Dandelion_armored", I18n.FakeNpc_Deepdark_DandelionMine() ) },
                { "Roslin", new( "Roslin_armored", "Roslin_armored", I18n.FakeNpc_Deepdark_RoslinMine() ) },
            };

            foreach (var entry in data)
            {
                NPC npc = new(new AnimatedSprite($"Characters\\{entry.Value.sprite}", 0, 16, 32), basePoint.ToVector2() * Game1.tileSize, "EastScarp_TNPCWaitingWarpRoom", Game1.down, $"{entry.Key}Mine", false, Game1.content.Load<Texture2D>($"Portraits\\{entry.Value.portrait}"))
                {
                    displayName = NPC.GetDisplayName(entry.Key)
                };
                npc.setNewDialogue(new Dialogue(npc, "deepdarkdialogue", entry.Value.dialogue));
                if (entry.Key == "Val")
                {
                    /*
                    npc.Sprite.setCurrentAnimation(
                    [
                        new(41, 750),
                        new(42, 250),
                    ]);
                    */
                }
                __instance.characters.Add(npc);
                basePoint.X++;
            }
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.GetEffectsOfRingMultiplier))]
    public static class FarmerWearingFakeYobaForD20Patch
    {
        public static void Postfix(Farmer __instance, string ringId, ref int __result)
        {
            if (__instance.hasOrWillReceiveMail("ForgedD20Power") && ringId == "863")
            {
                ++__result;
            }
        }
    }

    [HarmonyPatch(typeof(Item), nameof(Item.actionWhenPurchased))]
    public static class PurchaseElysiumBladeRecipeFix
    {
        public static void Postfix(Item __instance, ref bool __result)
        {
            if (__instance.QualifiedItemId == "(W)DN.SnS_ElysiumBlade" && __instance.IsRecipe)
            {
                if (Game1.activeClickableMenu is ShopMenu shop && shop.heldItem != null)
                {
                    ( shop.heldItem as MeleeWeapon ).Name = "DN.SnS_ElysiumBlade Recipe";
                }
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Tool), "tilesAffected")]
    public static class ElysiumHoldUprageTiles
    {
        public static void Postfix(Tool __instance, Vector2 tileLocation, int power, Farmer who, ref List<Vector2> __result)
        {
            if (!__instance.Name.ContainsIgnoreCase("Blessed") || power < 6) return;

            int radius = power - 4;

            List<Vector2> tilesAffected = [];
            Vector2 center = Vector2.Zero;
            switch (who.FacingDirection)
            {
                case 0:
                    center = new(tileLocation.X, tileLocation.Y - radius);
                    break;
                case 1:
                    center = new(tileLocation.X + radius, tileLocation.Y);
                    break;
                case 2:
                    center = new(tileLocation.X, tileLocation.Y + radius);
                    break;
                case 3:
                    center = new(tileLocation.X - radius, tileLocation.Y);
                    break;
            }

            for (int x = (int)center.X - radius; x <= (int)center.X + radius; x++)
                for (int y = (int)center.Y - radius; y <= (int)center.Y + radius; y++)
                    tilesAffected.Add(new(x, y));

            __result = tilesAffected;
        }
    }

    [HarmonyPatch(typeof(Game1), "drawHUD")]
    public static class Game1DrawHealthBarAndArmorPointsInDD
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insns)
        {
            CodeMatcher match = new(insns);
            bool next = false;
            object operand = null;
            foreach (var ins in insns)
            {
                if (next)
                {
                    operand = ins.operand;
                    break;
                }
                if (ins.opcode == OpCodes.Isinst)
                    next = true;
            }
            if (operand != null)
                match.MatchEndForward([
                    new(OpCodes.Brtrue_S, operand)
                    ])
                .Advance(1)
                .Insert([
                    new(OpCodes.Call, AccessTools.Method(typeof(Game1DrawHealthBarAndArmorPointsInDD), nameof(ShouldShowHealth))),
                    new(OpCodes.Brtrue_S, operand)
                    ]);

            return match.Instructions();
        }

        public static bool ShouldShowHealth()
        {
            return Game1.currentLocation.NameOrUniqueName == "EastScarp_DuskspireLair" || Game1.currentLocation.GetDungeonExtData().spaceCoreDungeonId.Value != null;
        }
    }
}