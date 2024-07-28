using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwordAndSorcerySMAPI
{
    public class TeleportInfoMessage
    {
        public string LocationName { get; set; }
        public Vector2 Tile { get; set; }
        public string Error { get; set; }
    }

    public class ModTOP
    {
        public const string RequestTeleportInfoMessage = "KCC.SnS/RequestTeleportInfo";
        public const string TeleportInfoMessage = "KCC.SnS/TeleportInfo";

        public const string WitchcraftUnlock = "SnS.Ch4.Dandelion.6";

        public static ModTOP Instance;

        public static Texture2D SpellCircle;
        public static Texture2D Portal;
        public static Texture2D Grimoire;

        public static Dictionary<string, ResearchEntry> Research { get; private set; }

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
            ModSnS.State.PreCastFacingDirection = Game1.player.FacingDirection;
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

        // https://stackoverflow.com/a/57385008
        public static IEnumerable<Color> GetColorGradient(Color from, Color to, int totalNumberOfColors)
        {
            if (totalNumberOfColors < 2)
            {
                throw new ArgumentException("Gradient cannot have less than two colors.", nameof(totalNumberOfColors));
            }

            double diffA = to.A - from.A;
            double diffR = to.R - from.R;
            double diffG = to.G - from.G;
            double diffB = to.B - from.B;

            int steps = totalNumberOfColors - 1;

            double stepA = diffA / steps;
            double stepR = diffR / steps;
            double stepG = diffG / steps;
            double stepB = diffB / steps;

            yield return from;

            for (int i = 1; i < steps; ++i)
            {
                yield return new Color(
                    c(from.R, stepR),
                    c(from.G, stepG),
                    c(from.B, stepB),
                    c(from.A, stepA));

                int c(int fromC, double stepC)
                {
                    return (int)Math.Round(fromC + stepC * i);
                }
            }

            yield return to;
        }

        public void Entry()
        {
            SpellCircle = Helper.ModContent.Load<Texture2D>("assets/spellcircle.png");
            Portal = Helper.ModContent.Load<Texture2D>("assets/return-portal.png");
            Grimoire = Helper.ModContent.Load<Texture2D>("assets/grimoire.png");

            Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
            Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.World.TerrainFeatureListChanged += World_TerrainFeatureListChanged;
            Helper.Events.World.FurnitureListChanged += World_FurnitureListChanged;
            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.Display.RenderedStep += Display_RenderedStep;
            Helper.Events.Content.AssetRequested += Content_AssetRequested;
            Helper.Events.Content.AssetsInvalidated += Content_AssetInvalidated;

            Helper.ConsoleCommands.Add("sns_research", "Open the witchcraft research menu", (cmd, args) =>
            {
                Game1.activeClickableMenu = new ResearchMenu();
            });

            SpaceEvents.OnItemEaten += SpaceEvents_OnItemEaten;

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
                        overrideLocationDestroy = true,
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
                                @event.aboveMapSprites?.Remove(tas);
                            }
                        }
                    }, i);
                }

                @event.CurrentCommand++;
            });
            Event.RegisterCommand("sns_magiccircle", (Event @event, string[] args, EventContext context) =>
            {
                ArgUtility.TryGetVector2(args, 1, out Vector2 center, out string error);
                center -= new Vector2(1, 1);

                TemporaryAnimatedSprite circle = new();
                float drawLayer = Math.Max(0f, (float)(center.Y * Game1.tileSize - 3) / 10000f);
                Game1.player.currentLocation.TemporarySprites.Add(circle = new TemporaryAnimatedSprite(Instance.Helper.ModContent.GetInternalAssetName("assets/spellcircle.png").BaseName, new Rectangle(0, 0, 48, 48), 10000, 1, 0, center * Game1.tileSize + new Vector2(-96 / 4 * 4 + 4, -96 / 4 * 4), false, false, drawLayer, 0, Color.Red, 2 * 4, 0, 0, 0)
                {
                    alpha = 0,
                    alphaFade = -0.02f,
                    /*
                    light = true,
                    lightRadius = 0.5f * 4,
                    lightcolor = new Color(255 - Color.Red.R, 255 - Color.Red.G, 255 - Color.Red.B)
                    */
                });


                Color[] colsRaw = [Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Violet, Color.White];
                List<Color> cols = new();
                for (int i = 0; i < colsRaw.Length - 1; ++i)
                {
                    cols.AddRange(GetColorGradient(colsRaw[i], colsRaw[i + 1], 40));
                }

                int interval = 10000 / cols.Count;
                for (int i = 0; i < cols.Count; ++i)
                {
                    Color c = cols[Math.Min(i, cols.Count - 1)];
                    DelayedAction.functionAfterDelay(() =>
                    {
                        var l = Game1.currentLightSources.FirstOrDefault(l => l.Identifier == circle.lightID);
                        if (l != null)
                            l.color.Value = new Color(255 - c.R, 255 - c.G, 255 - c.B);
                        circle.color = c;
                        if (circle.alpha > 0.5f)
                            circle.alpha = 0.5f;
                        for (int i = 0; i < 12; ++i)
                        {
                            float drawLayer3 = Math.Max(0f, (float)(center.Y * Game1.tileSize + 3) / 10000f);
                            float rad = (float)Game1.random.NextDouble() * MathF.PI * 2;
                            float len = (float)Game1.random.NextDouble() * 96 / 2 * 4;
                            Vector2 pos = new Vector2(MathF.Cos(rad) + 0.5f, MathF.Sin(rad) + 0.5f);
                            Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(Instance.Helper.ModContent.GetInternalAssetName("assets/particle.png").BaseName, new Rectangle(0, 0, 5, 5), 1000, 1, 0, center * Game1.tileSize + pos * len, false, false, drawLayer3, 0, c * 0.75f, 3, 0, 0, 0)
                            {
                                motion = new Vector2(0, -2),
                                alphaFade = 0.05f,
                                xPeriodic = true,
                                xPeriodicLoopTime = 375,
                                xPeriodicRange = 8,
                                delayBeforeAnimationStart = i * interval,
                            });
                        }
                    }, i * interval);
                }

                DelayedAction.functionAfterDelay(() =>
                {
                    circle.alphaFade = 0.02f;
                }, 9500);

                @event.CurrentCommand++;
            });

            RegisterSpells();
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Research = Game1.content.Load<Dictionary<string, ResearchEntry>>("KCC.SnS/WitchcraftResearch");
        }

        private void Content_AssetInvalidated(object sender, StardewModdingAPI.Events.AssetsInvalidatedEventArgs e)
        {
            if (e.Names.Any( a => a.IsEquivalentTo("KCC.SnS/WitchcraftResearch")))
                Research = Game1.content.Load<Dictionary<string, ResearchEntry>>("KCC.SnS/WitchcraftResearch");
        }

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("KCC.SnS/WitchcraftResearch"))
            {
                e.LoadFrom(() => new Dictionary<string, ResearchEntry>(), StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
        }

        private void Display_RenderedStep(object sender, StardewModdingAPI.Events.RenderedStepEventArgs e)
        {
            if (e.Step == StardewValley.Mods.RenderSteps.World_Sorted)
            {
                if (ModSnS.State.ReturnPotionLocation != null && Game1.currentLocation == Game1.getFarm())
                {
                    if (!Game1.getFarm().TryGetMapPropertyAs("WarpTotemEntry", out Point warp_location, required: false))
                    {
                        warp_location = Game1.whichFarm switch
                        {
                            6 => new Point(82, 29),
                            5 => new Point(48, 39),
                            _ => new Point(48, 7),
                        };
                    }

                    Vector2 spot = warp_location.ToVector2() - new Vector2(0, 1);
                    spot += new Vector2(0, -1);
                    spot *= Game1.tileSize;
                    e.SpriteBatch.Draw(ModTOP.Portal, Game1.GlobalToLocal(Game1.viewport, spot), null, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, (spot.Y + Game1.tileSize * 2 + 1) / 10000f);
                }
            }

            if (e.Step == StardewValley.Mods.RenderSteps.World_Sorted)
            {
                if (ModSnS.State.PocketDimensionLocation != null && Game1.currentLocation.NameOrUniqueName == "EastScarp_PocketDimension")
                {
                    Vector2 spot = new Vector2(15, 6);
                    spot *= Game1.tileSize;
                    e.SpriteBatch.Draw(ModTOP.Portal, Game1.GlobalToLocal(Game1.viewport, spot), null, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, (spot.Y + Game1.tileSize * 2 + 1) / 10000f);
                }
            }
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            if (e.IsSuppressed())
                return;

            if (Game1.player.GetFarmerExtData().stasisTimer.Value > 0)
            {
                Helper.Input.Suppress(e.Button);
            }

            if (e.Button.IsActionButton() && Game1.currentLocation == Game1.getFarm() && ModSnS.State.ReturnPotionLocation != null)
            {
                if (!Game1.getFarm().TryGetMapPropertyAs("WarpTotemEntry", out Point warp_location, required: false))
                {
                    warp_location = Game1.whichFarm switch
                    {
                        6 => new Point(82, 29),
                        5 => new Point(48, 39),
                        _ => new Point(48, 7),
                    };
                }

                if (e.Cursor.GrabTile.ToPoint() == warp_location - new Point(0, 1))
                {
                    Game1.player.currentLocation.performTouchAction($"MagicWarp {ModSnS.State.ReturnPotionLocation} {ModSnS.State.ReturnPotionCoordinates.X} {ModSnS.State.ReturnPotionCoordinates.Y}", Game1.player.getStandingPosition());
                    ModSnS.State.ReturnPotionLocation = null;
                }
            }
            else if (e.Button.IsActionButton() && Game1.currentLocation.NameOrUniqueName == "EastScarp_PocketDimension" && ModSnS.State.PocketDimensionLocation != null)
            {
                if (e.Cursor.GrabTile.ToPoint() == new Point( 15, 7 ) )
                {
                    Game1.player.currentLocation.performTouchAction($"MagicWarp {ModSnS.State.PocketDimensionLocation} {ModSnS.State.PocketDimensionCoordinates.X} {ModSnS.State.PocketDimensionCoordinates.Y}", Game1.player.getStandingPosition());
                }
            }
        }

        private void SpaceEvents_OnItemEaten(object sender, EventArgs e)
        {
            if (Game1.player.itemToEat == null)
                return;

            if (Game1.player.itemToEat.ItemId == "DN.SnS_ReturnPotion")
            {
                if (!Game1.getFarm().TryGetMapPropertyAs("WarpTotemEntry", out Point warp_location, required: false))
                {
                    warp_location = Game1.whichFarm switch
                    {
                        6 => new Point(82, 29),
                        5 => new Point(48, 39),
                        _ => new Point(48, 7),
                    };
                }
                ModSnS.State.ReturnPotionLocation = Game1.player.currentLocation.NameOrUniqueName;
                ModSnS.State.ReturnPotionCoordinates = Game1.player.TilePoint;
                Game1.player.currentLocation.performTouchAction($"MagicWarp Farm {warp_location.X} {warp_location.Y}", Game1.player.getStandingPosition());
            }
        }

        public static TeleportInfoMessage ProcessTeleportRequest(TeleportInfoMessage msg)
        {
            TeleportInfoMessage retMsg = new();

            string key = $"{msg.LocationName}/{msg.Tile.X},{msg.Tile.Y}";
            string target = null;
            if (ModSnS.State.TeleportCircles.ContainsKey(key))
            {
                foreach (var kvp in ModSnS.State.TeleportCircles)
                {
                    if (kvp.Key != key && kvp.Value == ModSnS.State.TeleportCircles[key])
                    {
                        target = kvp.Key;
                        break;
                    }
                }

                if (target == null)
                {
                    retMsg.Error = "teleport-circle.error.no-match";
                }
                else
                {
                    // TODO: Check and consume essences, chests nearby and player inventory as backup

                    int slash = target.IndexOf('/');
                    int comma = target.IndexOf(',', slash + 1);
                    retMsg.LocationName = target.Substring(0, slash);
                    retMsg.Tile = new Vector2(float.Parse(target.Substring(slash + 1, comma - slash - 1)), float.Parse(target.Substring(comma + 1)));
                }
            }
            else
            {
                retMsg.Error = "teleport-circle.error.unknown";
            }

            return retMsg;
        }

        public static void ProcessTeleport(TeleportInfoMessage msg)
        {
            if (msg.Error != null)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.GetByKey(msg.Error)));
            }
            else
            {
                Game1.player.currentLocation.performTouchAction($"MagicWarp {msg.LocationName} {msg.Tile.X} {msg.Tile.Y}", Game1.player.getStandingPosition());
                ModSnS.State.LastWalkedTile = msg.Tile.ToPoint();
            }
        }

        private void Multiplayer_ModMessageReceived(object sender, StardewModdingAPI.Events.ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            if (e.Type == RequestTeleportInfoMessage)
            {
                var msg = e.ReadAs<TeleportInfoMessage>();

                var retMsg = ProcessTeleportRequest(msg);
                Helper.Multiplayer.SendMessage(retMsg, TeleportInfoMessage, [ModManifest.UniqueID], [e.FromPlayerID]);
            }
            else if (e.Type == TeleportInfoMessage)
            {
                var msg = e.ReadAs<TeleportInfoMessage>();
                ProcessTeleportRequest(msg);
            }
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            ModSnS.State.TeleportCircles.Clear();
            Utility.ForEachLocation((loc) =>
            {
                foreach (var tf in loc.terrainFeatures.Values)
                {
                    if (tf is Flooring f && f.whichFloor.Value == "DN.SnS_TeleportCircleFloor")
                    {
                        CacheTeleportCircle(f);
                    }
                }
                return true;
            }, includeInteriors: true, includeGenerated: false);
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            Game1.player.GetFarmerExtData().mageArmor = false;
        }

        private void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (Game1.locationRequest == null && !Game1.isWarping && Game1.player.CanMove)
            {
                var diff = ModSnS.State.LastWalkedTile - Game1.player.Tile.ToPoint();
                if (ModSnS.State.LastWalkedTile != new Point(-100, -100) && 
                    (Math.Abs(diff.X) > 1 || Math.Abs(diff.Y) > 1))
                {
                    ModSnS.State.LastWalkedTile = new(-100, -100);
                }
            }

            var extData = Game1.player.GetFarmerExtData();
            if (Game1.shouldTimePass() && extData.stasisTimer.Value > 0)
            {
                extData.stasisTimer.Value -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (extData.stasisTimer.Value <= 0)
                {
                    Game1.player.playNearbySoundLocal("coldSpell");
                }
            }
        }

        private void Player_Warped(object sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            var ext = Game1.player.GetFarmerExtData();
            if (ext.isGhost.Value)
            {
                ext.isGhost.Value = false;
                Game1.player.ignoreCollisions = false;
            }
        }

        private void World_TerrainFeatureListChanged(object sender, StardewModdingAPI.Events.TerrainFeatureListChangedEventArgs e)
        {
            foreach (var removed in e.Removed)
            {
                if (removed.Value is Flooring f && f.whichFloor.Value == "DN.SnS_TeleportCircleFloor")
                {
                    ModSnS.State.TeleportCircles.Remove($"{e.Location.NameOrUniqueName}/{f.Tile.X},{f.Tile.Y}");
                }
            }
            foreach (var added in e.Added)
            {
                if (added.Value is Flooring f && f.whichFloor.Value == "DN.SnS_TeleportCircleFloor")
                {
                    CacheTeleportCircle(f);
                }
            }
        }

        private void World_FurnitureListChanged(object sender, StardewModdingAPI.Events.FurnitureListChangedEventArgs e)
        {
            foreach (var removed in e.Removed)
            {
                Rectangle r = new Rectangle(removed.boundingBox.X / Game1.tileSize - 2, removed.boundingBox.Y / Game1.tileSize - 2, (removed.boundingBox.X + removed.boundingBox.Width) / Game1.tileSize + 2, (removed.boundingBox.Y + removed.boundingBox.Height) / Game1.tileSize + 2);

                for (int ix = r.Left; ix < r.Right; ++ix)
                {
                    for (int iy = r.Top; iy < r.Bottom; ++iy)
                    {
                        if (e.Location.terrainFeatures.TryGetValue(new Vector2(ix, iy), out var tf) &&
                             tf is Flooring f && f.whichFloor.Value == "DN.SnS_TeleportCircleFloor")
                        {
                            ModSnS.State.TeleportCircles.Remove($"{f.Location.NameOrUniqueName}/{f.Tile.X},{f.Tile.Y}");
                        }
                    }
                }
            }
            foreach (var added in e.Added)
            {
                Rectangle r = new Rectangle(added.boundingBox.X / Game1.tileSize - 2, added.boundingBox.Y / Game1.tileSize - 2, (added.boundingBox.X + added.boundingBox.Width) / Game1.tileSize + 2, (added.boundingBox.Y + added.boundingBox.Height) / Game1.tileSize + 2);

                for (int ix = r.Left; ix < r.Right; ++ix)
                {
                    for (int iy = r.Top; iy < r.Bottom; ++iy)
                    {
                        if (added.Location.terrainFeatures.TryGetValue(new Vector2(ix, iy), out var tf) &&
                             tf is Flooring f && f.whichFloor.Value == "DN.SnS_TeleportCircleFloor")
                        {
                            ModSnS.State.TeleportCircles.Remove($"{f.Location.NameOrUniqueName}/{f.Tile.X},{f.Tile.Y}");
                            CacheTeleportCircle(f);
                        }
                    }
                }
            }
        }

        private void CacheTeleportCircle(Flooring f)
        {
            Rectangle r = new Rectangle((int)(f.Tile.X - 2) * Game1.tileSize, (int)(f.Tile.Y - 2) * Game1.tileSize, 5 * Game1.tileSize, 5 * Game1.tileSize);

            List<Furniture> tables = new(), allTables = new();
            foreach (var furn in f.Location.furniture)
            {
                if (furn.IsTable() && furn.boundingBox.Value.Intersects(r))
                {
                    allTables.Add(furn);
                    if (furn.heldObject.Value != null)
                        tables.Add(furn);
                }
            }
            if (tables.Count >= 4)
            {
                tables.Sort((a, b) => (int)(a.TileLocation.Y * 1000 + a.TileLocation.X) - (int)(b.TileLocation.Y * 1000 + b.TileLocation.X));

                string comboKey = string.Concat(tables.Select(furn => furn.heldObject.Value.ItemId));
                ModSnS.State.TeleportCircles.Add($"{f.Location.NameOrUniqueName}/{f.Tile.X},{f.Tile.Y}", comboKey);
            }

            foreach (var table in allTables)
            {
                table.heldObject.fieldChangeEvent += (NetRef<StardewValley.Object> field, StardewValley.Object oldObj, StardewValley.Object newObj) =>
                {
                    if (f.Location == null)
                        return;
                    ModSnS.State.TeleportCircles.Remove($"{f.Location.NameOrUniqueName}/{f.Tile.X},{f.Tile.Y}");
                    if (f.Location.terrainFeatures.TryGetValue(f.Tile, out TerrainFeature tf) && tf == f)
                    {
                        ModSnS.State.TeleportCircles.Remove($"{f.Location.NameOrUniqueName}/{f.Tile.X},{f.Tile.Y}");
                        CacheTeleportCircle(f);
                    }
                };
            }
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
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_Haste",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.LimeGreen, () => Spells.Haste());
                }
            });

            Ability.Abilities.Add("spell_stasis", new Ability("spell_stasis")
            {
                Name = I18n.Witchcraft_Spell_Stasis_Name,
                Description = I18n.Witchcraft_Spell_Stasis_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 4,
                ManaCost = () => 10,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_Stasis",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Orange, () => Spells.Stasis());
                }
            });

            Ability.Abilities.Add("spell_magearmor", new Ability("spell_magearmor")
            {
                Name = I18n.Witchcraft_Spell_MageArmor_Name,
                Description = I18n.Witchcraft_Spell_MageArmor_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 5,
                ManaCost = () => 5,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_MageArmor",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Orange, () => Spells.MageArmor());
                }
            });

            Ability.Abilities.Add("spell_wallofforce", new Ability("spell_wallofforce")
            {
                Name = I18n.Witchcraft_Spell_WallOfForce_Name,
                Description = I18n.Witchcraft_Spell_WallOfForce_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 6,
                ManaCost = () => 5,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_WallOfForce",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Orange, () => Spells.WallOfForce());
                }
            });

            Ability.Abilities.Add("spell_reviveplant", new Ability("spell_reviveplant")
            {
                Name = I18n.Witchcraft_Spell_RevivePlant_Name,
                Description = I18n.Witchcraft_Spell_RevivePlant_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 13,
                ManaCost = () => 10,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_RevivePlant",
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
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_MirrorImage",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Yellow, () => Spells.MirrorImage());
                }
            });

            Ability.Abilities.Add("spell_ghostlyprojection", new Ability("spell_ghostlyprojection")
            {
                Name = I18n.Witchcraft_Spell_GhostlyProjection_Name,
                Description = I18n.Witchcraft_Spell_GhostlyProjection_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 16,
                ManaCost = () => 7,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_GhostlyProjection",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                CanUse = () => !Game1.player.GetFarmerExtData().isGhost.Value,
                Function = () =>
                {
                    CastSpell(Color.Aqua, () => Spells.GhostlyProjection());
                }
            });

            Ability.Abilities.Add("spell_pocketchest", new Ability("spell_pocketchest")
            {
                Name = I18n.Witchcraft_Spell_PocketChest_Name,
                Description = I18n.Witchcraft_Spell_PocketChest_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 18,
                ManaCost = () => 0,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_PocketChest",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Aqua, () => Spells.PocketChest());
                }
            });

            Ability.Abilities.Add("spell_pocketdimension", new Ability("spell_pocketdimension")
            {
                Name = I18n.Witchcraft_Spell_PocketDimension_Name,
                Description = I18n.Witchcraft_Spell_PocketDimension_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 19,
                ManaCost = () => 0,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_PocketDimension",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    CastSpell(Color.Aqua, () => Spells.PocketDimension());
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
            {
                Game1.createObjectDebris("(O)DN.SnS_EarthEssence", x, y, farmerId, __instance);
                Game1.createObjectDebris("(O)DN.SnS_EarthEssence", x, y, farmerId, __instance);
            }
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
            {
                Game1.createObjectDebris("(O)DN.SnS_WaterEssence", (int)who.Tile.X, (int)who.Tile.Y, farmerId, who.currentLocation);
                Game1.createObjectDebris("(O)DN.SnS_WaterEssence", (int)who.Tile.X, (int)who.Tile.Y, farmerId, who.currentLocation);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    public static class DropVariousMonsterEssencesPatch
    {
        public static void Postfix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;

            if (monster.isGlider.Value && Game1.random.NextDouble() < 0.25)
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_AirEssence"), new(x, y), 2, __instance);
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
                Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_FireEssence"), new(x, y), 2, __instance);
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
                ext.mirrorImages.Value <= 0)
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

    [HarmonyPatch(typeof(Flooring), nameof(Flooring.doCollisionAction))]
    public static class FlooringTeleportCirclePatch
    {
        public static void Postfix(Flooring __instance, Character who)
        {
            if (who != Game1.player || Game1.locationRequest != null)
                return;
            if (__instance.whichFloor.Value != "DN.SnS_TeleportCircleFloor")
                return;

            var diff = ModSnS.State.LastWalkedTile - __instance.Tile.ToPoint();
            if (Math.Abs(diff.X) > 1 && Math.Abs(diff.Y) > 1)
            {
                ModSnS.State.LastWalkedTile = __instance.Tile.ToPoint();

                var req = new TeleportInfoMessage()
                {
                    LocationName = __instance.Location.NameOrUniqueName,
                    Tile = __instance.Tile,
                };

                if (Game1.IsMasterGame)
                {
                    var ret = ModTOP.ProcessTeleportRequest(req);
                    ModTOP.ProcessTeleport(ret);
                }
                else
                {
                    ModTOP.Instance.Helper.Multiplayer.SendMessage(req, ModTOP.RequestTeleportInfoMessage);
                }
            }
        }
    }

    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.minutesElapsed))]
    public static class ObjectWallOfForceExpirePatch
    {
        public static void Postfix(StardewValley.Object __instance, int minutes, ref bool __result)
        {
            if (__instance.ItemId == "DN.SnS_WallOfForce")
            {
                __instance.MinutesUntilReady -= minutes;
                if (__instance.MinutesUntilReady <= 0)
                    __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public static class FarmerStasisNoDamagePatch
    {
        public static bool Prefix(Farmer __instance)
        {
            if (__instance.GetFarmerExtData().stasisTimer.Value > 0)
            {
                return false;
            }

            return true;
        }
    }
}
