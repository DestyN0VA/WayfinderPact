using CircleOfThornsSMAPI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using NeverEndingAdventure;
using SpaceCore;
using SpaceCore.VanillaAssetExpansion;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.SpecialOrders;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    }

    [HarmonyPatch(typeof(Farmer), "initNetFields")]
    public static class AddLoreWeaponField
    {
        public static void Postfix(Farmer __instance)
        {
            __instance.NetFields.AddField(__instance.GetFarmerExtData().hasTakenLoreWeapon);
        }
    }

    public static partial class Extensions
    {
        public static FarmerExtData GetFarmerExtData(this Farmer instance)
        {
            return ModSnS.farmerData.GetOrCreateValue(instance);
        }
    }

    public class State
    {
        public ThrownShield MyThrown { get; set; }
        public float ThrowCooldown { get; set; } = 0;

        public float BlockCooldown { get; set; } = 0;

        public bool InShadows { get; set; } = false;
    }

    public class Configuration
    {
        public KeybindList ThrowShieldKey = new(SButton.R);
    }

    public class ModSnS : StardewModdingAPI.Mod
    {
        public static ModSnS instance;
        public static Configuration Config { get; set; }
        private static PerScreen<State> _state = new(() => new State());
        public static State State => _state.Value;

        public static Texture2D ShieldSlotBackground;
        public static Texture2D ShieldItemTexture;
        public static Texture2D SwordOverlay;

        public static ConditionalWeakTable<Farmer, FarmerExtData> farmerData = new();

        public const string ShadowstepEventReq = "SnS.Ch1.Mateo.18";

        public override void Entry(IModHelper helper)
        {
            instance = this;
            I18n.Init(Helper.Translation);
            Config = Helper.ReadConfig<Configuration>();

            ShieldSlotBackground = Helper.ModContent.Load<Texture2D>("assets/shield-bg.png");
            ShieldItemTexture = Helper.ModContent.Load<Texture2D>("assets/shield-item.png");
            SwordOverlay = Helper.ModContent.Load<Texture2D>("assets/SwordOverlay.png");

            Helper.Events.Content.AssetRequested += Content_AssetRequested;
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
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

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll( Assembly.GetExecutingAssembly() );

            new ModCoT(Monitor, ModManifest, Helper).Entry();
            new ModNEA(Monitor, ModManifest, Helper).Entry(harmony);
            new ModUP(Monitor, ModManifest, Helper).Entry();
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button.IsActionButton() && Game1.currentLocation is Farm farm &&
                e.Cursor.Tile == farm.GetGrandpaShrinePosition().ToVector2() &&
                farm.grandpaScore.Value == 4 && !Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value)
            {
                Game1.player.addItemByMenuIfNecessaryElseHoldUp(new MeleeWeapon("swordandsorcery.longlivetheking"));
                Game1.player.GetFarmerExtData().hasTakenLoreWeapon.Value = true;
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
            var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(ThrownShield));
            sc.RegisterCustomProperty(typeof(Farmer), "shieldSlot", typeof(NetRef<Item>), AccessTools.Method(typeof(Farmer_ShieldSlot), nameof(Farmer_ShieldSlot.get_shieldSlot)), AccessTools.Method(typeof(Farmer_ShieldSlot), nameof(Farmer_ShieldSlot.set_shieldSlot)));
            sc.RegisterCustomProperty(typeof(Farmer), "takenLoreWeapon", typeof(NetBool), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.HasTakenLoreWeapon)), AccessTools.Method(typeof(FarmerExtData), nameof(FarmerExtData.SetHasTakenLoreWeapon)));
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

                if (Game1.player.get_shieldSlot().Value?.QualifiedItemId == "(O)DestyNova.SwordAndSorcery_LegendaryHeroRelic")
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


            if (Game1.player.eventsSeen.Contains(ModSnS.ShadowstepEventReq))
            {
                ModSnS.State.InShadows = true;

                var b = Game1.player.buffs.AppliedBuffs.FirstOrDefault(pair => pair.Key == "shadowstep").Value;
                if (b == null)
                {
                    b = new Buff(
                        "shadowstep",
                        duration: 100,
                        displayName: I18n.Shadowstep(),
                        effects: new() { CriticalChanceMultiplier = { 999f } },
                        iconTexture: ModCoT.Skill.Icon,
                        iconSheetIndex: 0);
                    Game1.player.applyBuff(b);
                }
                else
                {
                    b.millisecondsDuration = 100;
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
                ModSnS.State.InShadows = true;
            }
        }

        private void Display_RenderedHud(object sender, StardewModdingAPI.Events.RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady)
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
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.withinPlayerThreshold), new Type[] { typeof( int ) } )]
    public static class NpcShadowstepPatch
    {
        public static void Postfix(NPC __instance, int threshold, ref bool __result )
        {
            if ( __instance is Monster && ModSnS.State.InShadows )
                __result = false;
        }
    }

    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.leftClick))]
    public static class WeaponSwingShadowstepPatch
    {
        public static void Postfix(NPC __instance)
        {
            ModSnS.State.InShadows = false;
            if (Game1.player.buffs.HasBuffWithNameContaining("shadowstep"))
                Game1.player.buffs.Remove("shadowstep");
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
    public static class SpecialOrderUntimedQuestsPatch
    {
        public static void Postfix(SpecialOrder __instance, ref bool __result)
        {
            if (__instance.questKey.Value.StartsWith("CAGQuest.UntimedSpecialOrder") || __instance.questKey.Value == "Mateo.SpecialOrders.BuildGuild")
            {
                __result = false;
            }
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
}