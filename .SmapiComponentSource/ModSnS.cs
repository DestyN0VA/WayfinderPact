using CircleOfThornsSMAPI;
using ContentPatcher;
using HarmonyLib;
using MageDelve.Mercenaries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using NeverEndingAdventure;
using NeverEndingAdventure.Utils;
using RadialMenu;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.GameData.SpecialOrders;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.Tools;
using SwordAndSorcerySMAPI.Alchemy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SwordAndSorcerySMAPI
{
    public class SteelShieldRecipe : CustomCraftingRecipe
    {
        public override string Description => ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_SteelShield").Description;

        public override Texture2D IconTexture => ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_SteelShield").GetTexture();

        public override Rectangle? IconSubrect => ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_SteelShield").GetSourceRect();

        private IngredientMatcher[] ingreds = [new ObjectIngredientMatcher("(O)335", 5), new ObjectIngredientMatcher("(O)388", 25)];
        public override IngredientMatcher[] Ingredients => ingreds;

        public override Item CreateResult()
        {
            return ItemRegistry.Create("(W)DN.SnS_SteelShield");
        }
    }

    public class FinalePartnerInfo
    {
        public string IntermissionEventId { get; set; }
        public string VictoryEventId { get; set; }
    }

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
        public readonly NetFloat stasisTimer = new( -1 );
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

        public static bool IsShieldItem(this MeleeWeapon mw)
        {
            return (mw.GetData()?.CustomFields?.ContainsKey("DN.SnS_Shield") ?? false);
        }

        public static int? GetArmorAmount(this Item item, bool includeMageArmor = true)
        {
            int mageArmor = Game1.player.GetFarmerExtData().mageArmor ? 50 : 0;

            int shields = 0;
            if (Game1.player.CurrentTool is MeleeWeapon mw1 && mw1.IsShieldItem())
                ++shields;
            if (Game1.player.GetOffhand() is MeleeWeapon mw2 && mw2.IsShieldItem())
                ++shields;

            int armorAmt = 25;
            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldArmor1))
                armorAmt += 25;
            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldArmor2))
                armorAmt += 50;
            mageArmor += armorAmt * shields;

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
        public List<ThrownShield> MyThrown { get; set; } = new();
        public float ThrowCooldown { get; set; } = 0;

        public float BlockCooldown { get; set; } = 0;
        public bool CanRepairArmor { get; set; } = true;
        public bool HasCraftedFree { get; set; } = false;

        public Monster LastAttacked { get; set; } = null;
        public int LastAttackedCounter { get; set; } = 0;

        public Point LastWalkedTile { get; set; } = new();
        public Dictionary<string, string> TeleportCircles { get; set; } = new();

        public string ReturnPotionLocation { get; set; }
        public Point ReturnPotionCoordinates { get; set; }

        public string PocketDimensionLocation { get; set; } = "FarmHouse";
        public Point PocketDimensionCoordinates { get; set; } = new Point(5, 7);

        public int PreCastFacingDirection { get; set; }

        public bool DoFinale { get; set; } = false;
        public Monster FinaleBoss { get; set; }
        public bool DoingBossDeathAnim { get; set; } = false;
        public bool FinishedBoxxDeathAnim { get; set; } = false;
        public int DeathAnimTimer { get; set; } = 6300;

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

        public Dictionary<GreenSlime, PolymorphData> Polymorphed { get; set; } = new();
        public Dictionary<Monster, BanishData> Banished { get; set; } = new();
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

        public bool LltkToggleRightClick = false;
        public KeybindList LltkToggleKeybind = new(new Keybind(SButton.None));

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
        public static ModSnS instance;
        public static Configuration Config { get; set; }
        private static PerScreen<State> _state = new(() => new State());
        public static State State => _state.Value;

        public static Texture2D ShieldItemTexture;
        public static Texture2D SwordOverlay;

        public static ConditionalWeakTable<Farmer, FarmerExtData> farmerData = new();

        public static RogueSkill RogueSkill;

        public const string ShadowstepEventReq = "SnS.Ch1.Mateo.18";

        public static ISpaceCoreApi sc;
        public static IRadialMenuApi radial;

        public static Vector2 DuskspireDeathPos;
        public static int AetherRestoreTimer = 0;

        private Harmony harmony;

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
            Helper.Events.Content.AssetsInvalidated += Content_AssetsInvalidated;
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged;
            Helper.Events.GameLoop.SaveCreated += GameLoop_SaveCreated;
            Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.Display.RenderedHud += Display_RenderedHud;
            Helper.Events.Display.RenderedWorld += Display_RenderedWorld;
            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.World.NpcListChanged += World_NpcListChanged;
            Helper.Events.World.LocationListChanged += World_LocationListChanged; ;

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
                {
                    Game1.activeClickableMenu = new ShieldSigilMenu();
                }
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

            Ability.Abilities.Add("swaplltk", new Ability("swaplltk")
            {
                Name = I18n.Ability_LltkToggle_Name,
                Description = I18n.Ability_LltkToggle_Description,
                TexturePath = "SMAPI/dn.sns/assets/Items & Crops/SnSObjects.png",
                SpriteIndex = 45,
                KnownCondition = "PLAYER_HAS_MAIL Current DN.SnS_ObtainedLLTK",
                HiddenIfLocked = true,
                ManaCost = () => 0,
                Function = () => SwapLltk(),
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
            Helper.ConsoleCommands.Add("sns_startfinalephase2", "...", (cmd, args) =>
            {
                if (!Context.IsPlayerFree || Game1.currentLocation.NameOrUniqueName != "EastScarp_DuskspireLair")
                {
                    Log.Info("Invalid situation");
                    return;
                }
                State.DoFinale = true;
            });
            Helper.ConsoleCommands.Add("sns_transmute", "...", (cmd, args) =>
            {
                if (Context.IsPlayerFree)
                    Game1.activeClickableMenu = new TransmuteMenu();
            });

            harmony = new Harmony(ModManifest.UniqueID);

            new ModCoT(Monitor, ModManifest, Helper).Entry();
            new ModNEA(Monitor, ModManifest, Helper).Entry(harmony);
            new ModUP(Monitor, ModManifest, Helper).Entry();
            new ModTOP(Monitor, ModManifest, Helper).Entry();
            new MercenaryEngine();
            new AlchemyEngine();

            InitArsenal();
        }

        private void Content_AssetsInvalidated(object sender, StardewModdingAPI.Events.AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(l => l.BaseName.EqualsIgnoreCase("DN.SnS/AlchemyRecipe")))
            {
                AlchemyRecipes._RecipeData = null;
            }
        }

        private void World_LocationListChanged(object sender, StardewModdingAPI.Events.LocationListChangedEventArgs e)
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
                        monster.modData.Add($"{ModManifest.UniqueID}_BuffedHealth", "meow");
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
                    monster.modData.Add($"{ModManifest.UniqueID}_BuffedHealth", "meow");
                }
            }
        }

        private void GameLoop_SaveCreated(object sender, StardewModdingAPI.Events.SaveCreatedEventArgs e)
        {
            Game1.player.mailReceived.OnValueAdded += OnMailReceived;
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            Game1.player.mailReceived.OnValueAdded += OnMailReceived;

            // Remove Seasonal Offerings Special Orders
            Game1.player.team.specialOrders.RemoveWhere(o => o.questKey.Value.StartsWithIgnoreCase("CAGQuest.UntimedSpecialOrder.Pentacle"));
            
            // Legacy armor slot migration

            foreach (var player in Game1.getAllFarmers())
            {
                var armorSlot = player.get_armorSlot();
                if (armorSlot.Value != null && sc.GetItemInEquipmentSlot(player, $"{ModManifest.UniqueID}_Armor") == null)
                {
                    sc.SetItemInEquipmentSlot(player, $"{ModManifest.UniqueID}_Armor", armorSlot.Value);
                    armorSlot.Value = null;
                }
            }
        }

        private void OnMailReceived(string value)
        {
            // I hope this doesn't multi trigger...
            if (value == "JojaMember" && Game1.IsMasterGame)
            {
                string[] toRemove =
                [
                    "CAGQuest.UntimedSpecialOrder.Pentacle1",
                    "CAGQuest.UntimedSpecialOrder.Pentacle2",
                    "CAGQuest.UntimedSpecialOrder.Pentacle3",
                    "CAGQuest.UntimedSpecialOrder.Pentacle4",
                ];

                Game1.player.team.specialOrders.RemoveWhere(s => toRemove.Contains(s.questKey.Value));
            }
        }

        private void GameLoop_TimeChanged(object sender, StardewModdingAPI.Events.TimeChangedEventArgs e)
        {
            if (e.NewTime % 100 == 0)
            {
                State.CanRepairArmor = true;
            }
        }

        private static void RecalculateAether()
        {var ext = Game1.player.GetFarmerExtData();

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

            if (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionAetherBuff))
            {
                maxMana += 75;
            }

            if (Game1.player.mailReceived.Any(m => m.StartsWith("DN.SnS.MaxMana_")) && int.TryParse(Game1.player.mailReceived.First(m => m.StartsWith("DN.SnS.MaxMana_")).Split('_')[1], out int OverridenMaxMana))
                maxMana = OverridenMaxMana;              

            ext.maxMana.Value = maxMana;
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
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
                e.Cursor.GrabTile == farm.GetGrandpaShrinePosition().ToVector2() &&
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
        }

        private bool SwapLltk()
        {
            if (Game1.player.ActiveItem == null)
                return false;

            if (Game1.player.ActiveItem.QualifiedItemId == "(W)DN.SnS_longlivetheking")
            {
                var w = new Slingshot("DN.SnS_longlivetheking_gun");
                Game1.player.CurrentTool.CopyEnchantments(Game1.player.CurrentTool, w);
                w.modData.Set(Game1.player.CurrentTool.modData.Pairs);
                if (Game1.player.CurrentTool.attachments.Count > 0 && Game1.player.CurrentTool.attachments[0] != null)
                {
                    w.attachments.SetCount(2);
                    w.attachments[0] = (StardewValley.Object)Game1.player.CurrentTool.attachments[0].getOne();
                    w.attachments[0].Stack = Game1.player.CurrentTool.attachments[0].Stack;
                    if (Game1.player.CurrentTool.attachments.Count > 1 && Game1.player.CurrentTool.attachments[1] != null)
                    {
                        w.attachments[1] = (StardewValley.Object)Game1.player.CurrentTool.attachments[1].getOne();
                        w.attachments[1].Stack = Game1.player.CurrentTool.attachments[1].Stack;
                    }
                }
                Game1.player.Items[Game1.player.CurrentToolIndex] = w;
                return true;
            }
            else if (Game1.player.ActiveItem.QualifiedItemId == "(W)DN.SnS_longlivetheking_gun")
            {
                var w = new MeleeWeapon("DN.SnS_longlivetheking");
                Game1.player.CurrentTool.CopyEnchantments(Game1.player.CurrentTool, w);
                w.modData.Set(Game1.player.CurrentTool.modData.Pairs);
                if (Game1.player.CurrentTool.attachments.Count > 0 && Game1.player.CurrentTool.attachments[0] != null)
                {
                    w.attachments.SetCount(2);
                    w.attachments[0] = (StardewValley.Object)Game1.player.CurrentTool.attachments[0].getOne();
                    w.attachments[0].Stack = Game1.player.CurrentTool.attachments[0].Stack;
                    if (Game1.player.CurrentTool.attachments.Count > 1 && Game1.player.CurrentTool.attachments[1] != null)
                    {
                        w.attachments[1] = (StardewValley.Object)Game1.player.CurrentTool.attachments[1].getOne();
                        w.attachments[1].Stack = Game1.player.CurrentTool.attachments[1].Stack;
                    }
                }
                Game1.player.Items[Game1.player.CurrentToolIndex] = w;
                return true;
            }

            return false;
        }

        private void Display_RenderedWorld(object sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch1.Mateo.12") ||
                 Game1.player.team.acceptedSpecialOrderTypes.Contains("SwordSorcery")  ||
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

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/AlchemyRecipes"))
                e.LoadFrom(() => new Dictionary<string, AlchemyData>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/FinalePartners"))
                e.LoadFrom(() => new Dictionary<string, FinalePartnerInfo>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
         
            string[] recolors =
            [
                "daisyniko.earthyinterface",
                "shinchan.cppurpleinterface",
                "enteis.woodeninterfeis",
                "thefrenchdodo.sakurainterfaceredux",
                "nom0ri.vintageuifix",
                "Sqbr.StarryBlueUI",
                "Bos.UIInterface",
            ];

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/ArmorSlot"))
            {
                string ArmorSlot = "assets/armor-bg.png";
                foreach (var recolor in recolors)
                {
                    if (Helper.ModRegistry.IsLoaded(recolor) && File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "armor-bg", recolor + ".png")))
                    {
                        ArmorSlot = $"assets/armor-bg/{recolor}.png";
                    }
                }
                e.LoadFromModFile<Texture2D>(ArmorSlot, StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/OffhandSlot"))
            {
                string OffhandSlot = "assets/offhand-bg.png";
                foreach (var recolor in recolors)
                {
                    if (Helper.ModRegistry.IsLoaded(recolor) && File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "armor-bg", recolor + "_offhand.png")))
                    {
                        OffhandSlot = $"assets/armor-bg/{recolor}_offhand.png";
                    }
                }
                e.LoadFromModFile<Texture2D>(OffhandSlot, StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }

            /*
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
            */
        }

        private double Perc;
        private double wait = 0;

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var gmcm = Helper.ModRegistry.GetApi<SpaceShared.APIs.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
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
                gmcm.AddComplexOption(ModManifest, I18n.String_ManabarPeview, (b, pos) => {
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
                    string manaStr = $"{(int)(10*x)}/10";
                    IClickableMenu.drawTextureBox(b, (int)pos.X, (int)pos.Y, 64 * 4 + 24, 32 + 12 + 12, Color.White);
                    b.Draw(Game1.staminaRect, new Rectangle((int)pos.X + 12, (int)pos.Y + 12, (int)(64 * 4 * perc), 32), Utility.StringToColor($"{ModSnS.Config.Red} {ModSnS.Config.Green} {ModSnS.Config.Blue}") ?? Color.Aqua);
                    b.DrawString(Game1.smallFont, manaStr, new Vector2(pos.X + 12 + 64 * 4 / 2 - Game1.smallFont.MeasureString(manaStr).X / 2, (int)pos.Y + 12), Utility.StringToColor($"{ModSnS.Config.TextRed} {ModSnS.Config.TextGreen} {ModSnS.Config.TextBlue}") ?? Color.Black);
                }, height: () => 56);

                gmcm.AddSectionTitle(ModManifest, I18n.Config_Section_Lltk);
                gmcm.AddParagraph(ModManifest, I18n.Config_Section_Lltk_Text);
                gmcm.AddBoolOption(ModManifest, () => Config.LltkToggleRightClick, (val) => Config.LltkToggleRightClick = val, I18n.Config_LltkToggleRightClick_Name, I18n.Config_LltkToggleRightClick_Description);
                gmcm.AddKeybindList(ModManifest, () => Config.LltkToggleKeybind, (val) => Config.LltkToggleKeybind = val, I18n.Config_LltkToggleKeybind_Name, I18n.Config_LltkToggleKeybind_Description);
                
                gmcm.AddSectionTitle(ModManifest, I18n.Section_Keybinds_Name, I18n.Section_Keybinds_Description);
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

            radial = Helper.ModRegistry.GetApi<IRadialMenuApi>("focustense.RadialMenu");
            if (radial != null)
            {
                radial.RegisterCustomMenuPage(ModManifest, "AdventureBar", new AdventureBarRadialMenuPageFactory());
            }

            Skills.RegisterSkill(RogueSkill = new RogueSkill());
            CustomCraftingRecipe.CraftingRecipes.Add("DN.SnS_Bow", new BowCraftingRecipe());
            CustomCraftingRecipe.CraftingRecipes.Add("DN.SnS_SteelShield", new SteelShieldRecipe());

            sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(ThrownShield));
            sc.RegisterCustomProperty(typeof(Farmer), "shieldSlot", typeof(NetRef<Item>), AccessTools.Method(typeof(Farmer_ArmorSlot), nameof(Farmer_ArmorSlot.get_armorSlot)), AccessTools.Method(typeof(Farmer_ArmorSlot), nameof(Farmer_ArmorSlot.set_armorSlot)));
            sc.RegisterCustomProperty(typeof(Farmer), "takenLoreWeapon", typeof(NetBool), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.HasTakenLoreWeapon)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetHasTakenLoreWeapon)));
            sc.RegisterCustomProperty(typeof(Farmer), "adventureBar", typeof(NetArray<string,NetString>), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.GetAdventureBar)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetAdventureBar)));
            sc.RegisterCustomProperty(typeof(Farmer), "maxMana", typeof(NetInt), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.GetMaxMana)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetMaxMana)));
            sc.RegisterCustomProperty(typeof(Farmer), "expRemainderRogue", typeof(NetFloat), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderRogueGetter)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.ExpRemainderRogueSetter)));

            sc.RegisterEquipmentSlot(ModManifest,
                $"{ModManifest.UniqueID}_Armor",
                item => item == null || item.IsArmorItem(),
                I18n.UiSlot_Armor,
                Game1.content.Load<Texture2D>("DN.SnS/ArmorSlot"));

            sc.RegisterEquipmentSlot(ModManifest,
                $"{ModManifest.UniqueID}_Offhand",
                item => item == null || item is MeleeWeapon,
                I18n.UiSlot_Offhand,
                Game1.content.Load<Texture2D>("DN.SnS/OffhandSlot"));

            sc.RegisterSpawnableMonster("Skull", (pos, data) =>
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

            sc.RegisterSpawnableMonster("MagmaSprite", (pos, data) =>
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

                    if (Game1.stats.Get("CirrusCooldown") == 0 && Game1.random.NextBool(1/3))
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
            }

            // This late because of accessing SpaceCore's local variable API
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void GameLoop_UpdateTicking(object sender, StardewModdingAPI.Events.UpdateTickingEventArgs e)
        {

            if (!Context.IsWorldReady)
                return;

            if (!Game1.player.mailReceived.Contains("DN.SnS_IntermissionShield") && Game1.player.eventsSeen.Any(m => m.StartsWith("SnS.Ch4.Victory")))
            {
                Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(W)DN.SnS_PaladinShield", 1, 0, false));
                Game1.addMail("DN.SnS_IntermissionShield", true, false);
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
                    Game1.currentLocation.characters.Add(State.FinaleBoss = new DuskspireMonster(new Vector2( 18, 13 ) * Game1.tileSize));
                    Game1.changeMusicTrack("SnS.DuskspirePhase2");

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

                    if (partner == null)
                    {
                        if (Game1.player.hasOrWillReceiveMail("FarmerGuildmasterBattle"))
                        {
                            partner = "Mateo";
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

                        if (!Game1.currentLocation.characters.Contains(State.FinaleBoss))
                        {
                            if (!State.FinishedBoxxDeathAnim)
                            {
                                State.FinishedBoxxDeathAnim = true;
                                Game1.screenGlowOnce(Color.White, false);
                            }

                            if (Game1.getAllFarmers().Any(f => f.currentLocation == Game1.getLocationFromName("EastScarp_DuskspireLair") && f.Items.Count > 0 && f.Items.Any(f => f.QualifiedItemId == "(O)DN.SnS_DuskspireHeart")))
                            {
                                Game1.player.GetCurrentMercenaries().Clear();
                                var partnerInfos = Game1.content.Load<Dictionary<string, FinalePartnerInfo>>("DN.SnS/FinalePartners");

                                FinalePartnerInfo partnerInfo = partnerInfos["default"];

                                foreach (string key in partnerInfos.Keys)
                                {
                                    if (Game1.player.friendshipData.TryGetValue(key, out var data) && data.IsDating())
                                    {
                                        partnerInfo = partnerInfos[key];
                                        break;
                                    }
                                }
                                Game1.PlayEvent(partnerInfo.VictoryEventId, checkPreconditions: false, checkSeen: false);

                                State.FinaleBoss = null;
                            }
                        }
                    }
                }
            }

            if (State.MyThrown.Count > 0)
            {
                foreach (var thrown in State.MyThrown.ToList())
                {
                    if ((thrown.GetPosition() - thrown.Target.Value).Length() < 16 && (thrown.Bounces.Value <= 0 || thrown.TargetMonster.Get(Game1.currentLocation) == null))
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
            else
            {
                /*
                if (Game1.player.GetArmorItem()?.QualifiedItemId == "(O)DestyNova.SwordAndSorcery_LegendaryHeroRelic")
                {
                    if (Config.ThrowShieldKey.JustPressed())
                    {
                        State.MyThrown = new ThrownShield(Game1.player, 30, Helper.Input.GetCursorPosition().AbsolutePixels, 10);
                        Game1.currentLocation.projectiles.Add(State.MyThrown);
                    }
                }*/
            }
            
            if (State.ThrowCooldown > 0)
                State.ThrowCooldown = MathF.Max(0, State.ThrowCooldown - (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds);

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
            else if ( Config.ConfigureAdventureBar.JustPressed() && Game1.activeClickableMenu == null && !Game1.IsChatting &&
                 Game1.player.hasOrWillReceiveMail("SnS_AdventureBar") )
            {
                Game1.activeClickableMenu = new AdventureBarConfigureMenu();
            }
            if ( e.IsOneSecond && !hasBar && !AdventureBar.Hide && Game1.player.hasOrWillReceiveMail( "SnS_AdventureBar" ) )
            {
                Game1.onScreenMenus.Add(new AdventureBar(editing: false));
            }
            else if (hasBar && Game1.activeClickableMenu == null && !Game1.IsChatting)
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

                for (int islot = 0; islot < 8; ++islot)
                {
                    string abilId = null;
                    if (binds[islot][1].JustPressed())
                    {
                        Helper.Input.SuppressActiveKeybinds(binds[islot][1]);
                        abilId = ext.adventureBar[8 + islot];
                    }
                    else if (binds[islot][0].JustPressed())
                    {
                        Helper.Input.SuppressActiveKeybinds(binds[islot][0]);
                        abilId = ext.adventureBar[islot];
                    }

                    if (abilId != null && Ability.Abilities.TryGetValue(abilId ?? "", out var abil) && abil.ManaCost() <= ext.mana.Value && abil.CanUse())
                    {
                        ext.mana.Value -= abil.ManaCost();
                        CastAbility(abil);
                    }
                }
            }

            sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");

            if (Game1.player.eventsSeen.Contains("SnS.Ch1.Mateo.18") && !Game1.player.hasOrWillReceiveMail("GaveArtificerLvl1"))
            {
                sc.AddExperienceForCustomSkill(Game1.player, RogueSkill.Id, 100);
                Game1.addMail("GaveArtificerLvl1", true);
            }

            if (Game1.player.eventsSeen.Contains("SnS.Ch2.Hector.16") && !Game1.player.hasOrWillReceiveMail("GaveDruidicsLvl1"))
            {
                sc.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Druidics", 100);
                Game1.addMail("GaveDruidicsLvl1", true);
            }

            if (Game1.player.eventsSeen.Contains("SnS.Ch3.Cirrus.14") && !Game1.player.hasOrWillReceiveMail("GaveBardicsLvl1"))
            {
                sc.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Bardics", 100);
                Game1.addMail("GaveBardicsLvl1", true);
            }

            if (Game1.player.eventsSeen.Contains(ModTOP.WitchcraftUnlock) && !Game1.player.hasOrWillReceiveMail("GaveWitchCraftLvl1"))
            {
                sc.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Witchcraft", 100);
                Game1.addMail("GaveWitchCraftLvl1", true);
            }

            if (Game1.player.eventsSeen.Any(l => l.StartsWith("SnS.Ch4.Victory")) && !Game1.player.hasOrWillReceiveMail("GavePaladinLvl1"))
            {
                sc.AddExperienceForCustomSkill(Game1.player, "DestyNova.SwordAndSorcery.Paladin", 100);
                Game1.addMail("GavePaladinLvl1", true);
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

        private void Player_Warped(object sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            if (State.MyThrown.Count > 0)
            {
                foreach (var entry in State.MyThrown)
                    entry.Dead = true;
                State.MyThrown.Clear();
            }

            if (Game1.player.eventsSeen.Contains(ModSnS.ShadowstepEventReq))
            {
                //ModSnS.State.InShadows = true;
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

            var armorAmt = Game1.player.GetArmorItem().GetArmorAmount();
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

            if (__instance.HasCustomProfession(WitchcraftSkill.ProfessionAetherBuff) && __instance.CanBeDamaged() && __instance.GetFarmerExtData().maxMana.Value > __instance.GetFarmerExtData().mana.Value)
            {
                __instance.GetFarmerExtData().mana.Value += (int)MathF.Min(Game1.random.Next(5,10), __instance.GetFarmerExtData().maxMana.Value - __instance.GetFarmerExtData().mana.Value);
            }

            var ext = Game1.player.GetFarmerExtData();
            if (__instance != Game1.player || overrideParry || !Game1.player.CanBeDamaged() ||
                Game1.player.GetArmorItem() == null ||
                ext.armorUsed.Value >= (Game1.player.GetArmorItem().GetArmorAmount() ?? -1))
                return true;

            bool flag = (damager == null || !damager.isInvincible()) && (damager == null || (damager is not GreenSlime && damager is not BigSlime) || !__instance.isWearingRing("520"));
            if (!flag) return true;

            __instance.playNearbySoundAll("parry");

            ext.armorUsed.Value = Math.Min(Game1.player.GetArmorItem().GetArmorAmount().Value, ext.armorUsed.Value + damage);
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
                { "Mateo", new( "Armor_Mateo", "Armor_Mateo", "I'm worried, but we've made it this far. Nothing can stop us as long as we stick together.$80" ) },
                { "Hector", new( "Hector_HoodDown", "Hector_HoodDown", "I feel sick...$19" ) },
                { "Cirrus", new( "Cirrus_Glamrock", "Cirrus_Glamrock", "I'm not sure what's going to happen, @, but with you here we'll be fine!$17" ) },
                { "Dandelion", new( "Dandelion_armored", "Dandelion_armored", "I have big plans for when this is over, @. Just you wait.$18" ) },
                { "Roslin", new( "Roslin_armored", "Roslin_armored", "This place is thick with Void magic. It's all-consuming...$16" ) },
            };

            foreach (var entry in data)
            {
                NPC npc = new(new AnimatedSprite($"Characters\\{entry.Value.sprite}", 0, 16, 32), basePoint.ToVector2() * Game1.tileSize, "EastScarp_TNPCWaitingWarpRoom", Game1.down, $"{entry.Key}Mine", false, Game1.content.Load<Texture2D>($"Portraits\\{entry.Value.portrait}"))
                {
                    displayName = NPC.GetDisplayName(entry.Key)
                };
                npc.setNewDialogue(new Dialogue(npc, "deepdarkdialogue", entry.Value.dialogue));
                if (entry.Key == "Mateo")
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

    [HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawTextureBox), [typeof(SpriteBatch), typeof(Texture2D), typeof(Rectangle), typeof(int), typeof(int), typeof(int), typeof(int), typeof(Color), typeof(float), typeof(bool), typeof(float) ])]
    public static class ShopTextureBoxForVioletMoonHackPatch
    {
        public static bool Prefix(SpriteBatch b, Texture2D texture, Rectangle sourceRect, int x, int y, int width, int height, Color color, float scale, bool drawShadow, float draw_layer)
        {
            if (Game1.activeClickableMenu is ShopMenu s && s.VisualTheme.WindowBorderTexture == texture && s.VisualTheme.WindowBorderSourceRect == sourceRect &&
                 sourceRect == new Rectangle(0, 0, 270, 115) && Game1.CurrentEvent.isFestival && Game1.CurrentEvent.id == "festival_fall1")
            {
                int cornerSizeX = sourceRect.Width / 3;
                int cornerSizeY = sourceRect.Height/ 3;
                float shadow_layer = draw_layer - 0.03f;
                if (draw_layer < 0f)
                {
                    draw_layer = 0.8f - (float)y * 1E-06f;
                    shadow_layer = 0.77f;
                }
                if (drawShadow)
                {
                    b.Draw(texture, new Vector2(x + width - (int)((float)cornerSizeX * scale) - 8, y + 8), new Rectangle(sourceRect.X + cornerSizeX * 2, sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Vector2(x - 8, y + height - (int)((float)cornerSizeY * scale) + 8), new Rectangle(sourceRect.X, cornerSizeY * 2 + sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Vector2(x + width - (int)((float)cornerSizeX * scale) - 8, y + height - (int)((float)cornerSizeY * scale) + 8), new Rectangle(sourceRect.X + cornerSizeX * 2, cornerSizeY * 2 + sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Rectangle(x + (int)((float)cornerSizeX * scale) - 8, y + 8, width - (int)((float)cornerSizeX * scale) * 2, (int)((float)cornerSizeY * scale)), new Rectangle(sourceRect.X + cornerSizeX, sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Rectangle(x + (int)((float)cornerSizeX * scale) - 8, y + height - (int)((float)cornerSizeY * scale) + 8, width - (int)((float)cornerSizeX * scale) * 2, (int)((float)cornerSizeY * scale)), new Rectangle(sourceRect.X + cornerSizeX, cornerSizeY * 2 + sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Rectangle(x - 8, y + (int)((float)cornerSizeY * scale) + 8, (int)((float)cornerSizeX * scale), height - (int)((float)cornerSizeY * scale) * 2), new Rectangle(sourceRect.X, cornerSizeY + sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Rectangle(x + width - (int)((float)cornerSizeX * scale) - 8, y + (int)((float)cornerSizeY * scale) + 8, (int)((float)cornerSizeX * scale), height - (int)((float)cornerSizeY * scale) * 2), new Rectangle(sourceRect.X + cornerSizeX * 2, cornerSizeY + sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
                    b.Draw(texture, new Rectangle((int)((float)cornerSizeX * scale / 2f) + x - 8, (int)((float)cornerSizeY * scale / 2f) + y + 8, width - (int)((float)cornerSizeX * scale), height - (int)((float)cornerSizeY * scale)), new Rectangle(cornerSizeX + sourceRect.X, cornerSizeY + sourceRect.Y, cornerSizeX, cornerSizeY), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
                }
                b.Draw(texture, new Rectangle((int)((float)cornerSizeX * scale) + x, (int)((float)cornerSizeY * scale) + y, width - (int)((float)cornerSizeX * scale * 2f), height - (int)((float)cornerSizeY * scale * 2f)), new Rectangle(cornerSizeX + sourceRect.X, cornerSizeY + sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Vector2(x, y), new Rectangle(sourceRect.X, sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Vector2(x + width - (int)((float)cornerSizeX * scale), y), new Rectangle(sourceRect.X + cornerSizeX * 2, sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Vector2(x, y + height - (int)((float)cornerSizeY * scale)), new Rectangle(sourceRect.X, cornerSizeY * 2 + sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Vector2(x + width - (int)((float)cornerSizeX * scale), y + height - (int)((float)cornerSizeY * scale)), new Rectangle(sourceRect.X + cornerSizeX * 2, cornerSizeY * 2 + sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Rectangle(x + (int)((float)cornerSizeX * scale), y, width - (int)((float)cornerSizeX * scale) * 2, (int)((float)cornerSizeY * scale)), new Rectangle(sourceRect.X + cornerSizeX, sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Rectangle(x + (int)((float)cornerSizeX * scale), y + height - (int)((float)cornerSizeY * scale), width - (int)((float)cornerSizeX * scale) * 2, (int)((float)cornerSizeY * scale)), new Rectangle(sourceRect.X + cornerSizeX, cornerSizeY * 2 + sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Rectangle(x, y + (int)((float)cornerSizeY * scale), (int)((float)cornerSizeX * scale), height - (int)((float)cornerSizeY * scale) * 2), new Rectangle(sourceRect.X, cornerSizeY + sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
                b.Draw(texture, new Rectangle(x + width - (int)((float)cornerSizeX * scale), y + (int)((float)cornerSizeY * scale), (int)((float)cornerSizeX * scale), height - (int)((float)cornerSizeY * scale) * 2), new Rectangle(sourceRect.X + cornerSizeX * 2, cornerSizeY + sourceRect.Y, cornerSizeX, cornerSizeY), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
                return false;
            }

            return true;
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

}