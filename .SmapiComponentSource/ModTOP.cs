using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Companions;
using StardewValley.GameData.Pets;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.Emit;
using StardewValley.Menus;
using SpaceCore.Spawnables;
using StardewValley.Inventories;
using System.Xml;
using StardewValley.Projectiles;

namespace SwordAndSorcerySMAPI
{
    public class MonsterExtensionData
    {
        public bool CanPolymorph { get; set; } = true;
        public bool CanBanish { get; set; } = true;
    }

    public class FamiliarCompanion : Companion
    {
        public AnimatedSprite spr;
        private PetData pet;
        private PetBreed breed;
        private double idleTimer = 0;

        public FamiliarCompanion()
        {
        }

        public override void InitNetFields()
        {
            base.InitNetFields();
            direction.fieldChangeVisibleEvent += (field, oldVal, newVal) => { if (spr != null) spr.faceDirection(newVal); };
        }

        public override void InitializeCompanion(Farmer farmer)
        {
            base.InitializeCompanion(farmer);

            var petData = DataLoader.Pets(Game1.content);
            pet = petData[farmer.whichPetType];
            breed = pet.GetBreedById(farmer.whichPetBreed);
            spr = new(breed.Texture, 0, 32, 32);
        }

        private static List<FarmerSprite.AnimationFrame> ToAnim(List<PetAnimationFrame> frames)
        {
            return frames.Select(f => new FarmerSprite.AnimationFrame(f.Frame, f.Duration)
            {
                frameStartBehavior = (_) => { if (f.Sound != null) Game1.playSound(f.Sound); }
            }).ToList();
        }

        public override void Update(GameTime time, GameLocation location)
        {
            var oldPos = Position;

            base.Update(time, location);

            if (oldPos == Position)
            {
                var oldTimer = idleTimer;
                idleTimer += time.ElapsedGameTime.TotalSeconds;
                if (idleTimer > 4)
                {
                    if (Owner.whichPetType == "Cat" && oldTimer <= 4)
                    {
                        spr.setCurrentAnimation(ToAnim(pet.Behaviors.First(b => b.Id == "BeginSitDown").Animation));
                        spr.loop = false;
                        var tmp1 = spr.currentAnimation.Last();
                        tmp1.AddFrameEndAction( (_) =>
                        {
                            spr.setCurrentAnimation(ToAnim(pet.Behaviors.First(b => b.Id == "SitDownLick").Animation));
                            spr.loop = false;
                            var tmp2 = spr.currentAnimation.Last();
                            tmp2.AddFrameEndAction( (_) =>
                            {
                                spr.setCurrentAnimation(ToAnim(pet.Behaviors.First(b => b.Id == "SitDownLickRepeat").Animation));
                                spr.loop = true;
                            });
                            spr.currentAnimation[spr.currentAnimation.Count - 1] = tmp2;
                        } );
                        spr.currentAnimation[spr.currentAnimation.Count - 1] = tmp1;
                    }
                    else if (oldTimer <= 4)
                    {
                        spr.setCurrentAnimation(ToAnim(pet.Behaviors.First(b => b.Id == "BeginSitDown").Animation));
                        spr.loop = false;
                        var tmp1 = spr.currentAnimation.Last();
                        tmp1.frameEndBehavior += (_) =>
                        {
                            spr.setCurrentAnimation(ToAnim(pet.Behaviors.First(b => b.Id == (Owner.whichPetType == "Dog" ? "SitDownPant" : "SitDown")).Animation));
                            spr.loop = true;
                        };
                        spr.currentAnimation[spr.currentAnimation.Count - 1] = tmp1;
                    }
                    else
                    {
                        spr.animateOnce(time);
                    }
                }
            }
            else
            {
                idleTimer = 0;

                var diff = Position - oldPos;
                int dir = diff.Y < 0 ? Game1.up : Game1.down;
                if (Math.Abs(diff.X) > Math.Abs(diff.Y))
                {
                    dir = diff.X < 0 ? Game1.left : Game1.right;
                }

                spr.loop = true;
                switch (dir)
                {
                    case Game1.down: spr.AnimateDown(time); break;
                    case Game1.right: spr.AnimateRight(time); break;
                    case Game1.up: spr.AnimateUp(time); break;
                    case Game1.left: spr.AnimateLeft(time); break;
                }
            }

            if (Game1.random.NextDouble() < 0.0005)
            {
                Game1.playSound(breed.BarkOverride ?? pet.BarkSound, breed.VoicePitch != 1 ? null : (int)(1200 * breed.VoicePitch) );
            }
        }

        public override void Draw(SpriteBatch b)
        {
            if (base.Owner == null || base.Owner.currentLocation == null || (base.Owner.currentLocation.DisplayName == "Temp" && !Game1.isFestival()))
            {
                return;
            }

            //b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(base.Position + base.Owner.drawOffset), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f, SpriteEffects.None, 0f);
            spr.draw(b, Game1.GlobalToLocal(base.Position + base.Owner.drawOffset) - new Vector2(64, 128), (base._position.Y - 12f) / 10000f);
        }

        public override void OnOwnerWarp()
        {
            base.OnOwnerWarp();
            //spr = new AnimatedSprite(Mod.instance.Helper.ModContent.GetInternalAssetName("assets/sprite.png").BaseName, 0, 16, 32);
        }
    }

    public class TeleportInfoMessage
    {
        public string LocationName { get; set; }
        public Vector2 Tile { get; set; }
        public string Error { get; set; }
    }

    public class ModTOP
    {
        public const string MultiplayerMessage_Polymorph = "KCC.SnS/Polymorph";
        public const string MultiplayerMessage_Banish = "KCC.SnS/Banish";
        public const string RequestTeleportInfoMessage = "KCC.SnS/RequestTeleportInfo";
        public const string TeleportInfoMessage = "KCC.SnS/TeleportInfo";

        public const string WitchcraftUnlock = "SnS.Ch4.Dandelion.6";

        public static ModTOP Instance;

        public static Texture2D SpellCircle;
        public static Texture2D Portal;
        public static Texture2D Grimoire;
        public static Texture2D StuffTexture;

        public static WitchcraftSkill Skill;
        public static PaladinSkill PaladinSkill;

        internal static bool drawingBanished = false;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SpellDamageMultiplier()
        {
            float ret = ModSnS.AetherDamageMultiplier();
            if (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage))
                ret += 0.25f;
            if (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage2))
                ret += 0.25f;
            return ret;
        }

        private static void CastSpell(Color spellColor, Action onCast)
        {
            ModSnS.State.PreCastFacingDirection = Game1.player.FacingDirection;
            Game1.player.completelyStopAnimatingOrDoingAction();
            Game1.player.faceDirection(Game1.down);
            Game1.player.canMove = false;
            if (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage2))
            {
                Game1.player.temporarilyInvincible = true;
                Game1.player.flashDuringThisTemporaryInvincibility = true;
                Game1.player.temporaryInvincibilityTimer = 0;
                Game1.player.currentTemporaryInvincibilityDuration = 2000;
            }
            Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[]
            {
                new FarmerSprite.AnimationFrame(57, 0),
                new FarmerSprite.AnimationFrame(57, 500, false, false),
                new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 100, false, false, player => { onCast(); player.CanMove = true; })
            });
            float drawLayer = Math.Max(0f, (float)(Game1.player.StandingPixel.Y + 3) / 10000f);
            float drawLayer2 = Math.Max(0f, (float)(Game1.player.StandingPixel.Y - 3) / 10000f);
            Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites/Cursors_1_6", new Rectangle(304, 397, 11, 11), 30, 11, 0, Game1.player.Position + new Vector2(8, Game1.tileSize * -1.8f), false, false, drawLayer, 0, spellColor, Game1.pixelZoom, 0, 0, 0)
            {
                lightId = "spellcast1",
                lightRadius = 0.5f,
                lightcolor = new Color(255 - spellColor.R, 255 - spellColor.G, 255 - spellColor.B)
            });
            Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(Instance.Helper.ModContent.GetInternalAssetName("assets/spellcircle.png").BaseName, new Rectangle(0, 0, 48, 48), 1000, 1, 0, Game1.player.Position + new Vector2(-96 / 4 + 4, -96 / 4), false, false, drawLayer2, 0, spellColor * 0.75f, 2, 0, 0, 0)
            {
                alpha = 0,
                alphaFade = -0.035f * 3,
                alphaFadeFade = -0.002f * 4,
                lightId = "spellcast2",
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
            Portal = Helper.ModContent.Load<Texture2D>("assets/portal.png");
            Grimoire = Helper.ModContent.Load<Texture2D>("assets/grimoire.png");
            StuffTexture = Helper.ModContent.Load<Texture2D>("assets/witchcraft/stuff.png");

            Skill = new WitchcraftSkill();
            PaladinSkill = new();

            Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
            Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.World.TerrainFeatureListChanged += World_TerrainFeatureListChanged;
            Helper.Events.World.FurnitureListChanged += World_FurnitureListChanged;
            Helper.Events.World.NpcListChanged += World_NpcListChanged;
            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.Display.RenderedStep += Display_RenderedStep;
            Helper.Events.Content.AssetRequested += Content_AssetRequested;
            Helper.Events.Content.AssetsInvalidated += Content_AssetInvalidated;

            Helper.ConsoleCommands.Add("sns_research", "Open the witchcraft research menu", (cmd, args) =>
            {
                Game1.activeClickableMenu = new ResearchMenu();
            });
            Helper.ConsoleCommands.Add("sns_shieldmenu", "Open the shield sigil menu", (cmd, args) =>
            {
                Game1.activeClickableMenu = new ShieldSigilMenu();
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

                Texture2D texture = Game1.content.Load<Texture2D>("SMAPI/DN.SnS/assets/Items & Crops/SnSObjects.png");

                Rectangle[] srcRects =
                [
                    new(96, 128, 16, 16),
                    new(112, 128, 16, 16),
                    new(128, 128, 16, 16),
                    new(144, 128, 16, 16),
                    new(96, 128, 16, 16),
                    new(112, 128, 16, 16),
                    new(128, 128, 16, 16),
                    new(144, 128, 16, 16)
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
                        var l = Game1.currentLightSources.FirstOrDefault(l => l.Key == circle.lightId).Value;
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

        private void World_NpcListChanged(object sender, StardewModdingAPI.Events.NpcListChangedEventArgs e)
        {
            foreach (var npc in e.Removed)
            {
                if (npc is GreenSlime slime && slime.Health <= 0)
                {
                    if (ModSnS.State.Polymorphed.ContainsKey(slime))
                    {
                        var data = ModSnS.State.Polymorphed[slime];
                        data.Original.Position = slime.Position;
                        ModSnS.State.Polymorphed.Remove(slime);
                        e.Location.characters.Add(data.Original);
                    }
                }
            }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Skills.RegisterSkill(Skill);
            Skills.RegisterSkill(PaladinSkill);
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
            else if (e.NameWithoutLocale.IsEquivalentTo("KCC.SnS/MonsterExtensionData"))
            {
                e.LoadFrom(() => new Dictionary<string, MonsterExtensionData>()
                {
                    { "Duskspire Behemoth", new() { CanBanish = false, CanPolymorph = false } },
                    { "Duskspire Remnant", new() { CanBanish = false, CanPolymorph = false } },
                }, StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
        }

        private void Display_RenderedStep(object sender, StardewModdingAPI.Events.RenderedStepEventArgs e)
        {
            if (e.Step == StardewValley.Mods.RenderSteps.World_Sorted)
            {
                drawingBanished = true;
                foreach (var monster in ModSnS.State.Banished)
                {
                    if (monster.Value.Location == Game1.currentLocation)
                    {
                        monster.Key.draw(e.SpriteBatch);
                    }
                }
                drawingBanished = false;
            }
            
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
                    spot.X -= 32;
                    int frame = ((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % (125*6)) / 125;
                    e.SpriteBatch.Draw(ModTOP.Portal, Game1.GlobalToLocal(Game1.viewport, spot), new Rectangle(frame % 3 * 32, frame / 3 * 32, 32, 32), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, (spot.Y + Game1.tileSize * 2 + 1) / 10000f);
                }
            }

            if (e.Step == StardewValley.Mods.RenderSteps.World_Sorted)
            {
                if (ModSnS.State.PocketDimensionLocation != null && Game1.currentLocation.NameOrUniqueName == "EastScarp_PocketDimension")
                {
                    Vector2 spot = new Vector2(15, 6);
                    spot *= Game1.tileSize;
                    spot.X -= 32;
                    int frame = ((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % (125 * 6)) / 125;
                    e.SpriteBatch.Draw(ModTOP.Portal, Game1.GlobalToLocal(Game1.viewport, spot), new Rectangle(frame % 3 * 32, frame / 3 * 32, 32, 32), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, (spot.Y + Game1.tileSize * 2 + 1) / 10000f);
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

        public static TeleportInfoMessage ProcessTeleportRequest(TeleportInfoMessage msg, long from)
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
                    var loc = Game1.getLocationFromName(msg.LocationName);

                    int totalFound = 0;
                    List<(IInventory inv, Item item)> essencesFound = new();
                    for (int ix = -2; ix <= 2; ++ix)
                    {
                        for (int iy = -2; iy <= 2; ++iy)
                        {
                            if (totalFound >= 3)
                                continue;

                            Vector2 pos = msg.Tile + new Vector2(ix, iy);
                            if (loc.Objects.TryGetValue( pos, out var obj ) && obj is Chest chest)
                            {
                                // TODO: Make this use the chest mutex right
                                var inv = chest.GetItemsForPlayer(from);
                                for (int i = 0; i < chest.GetActualCapacity(); ++i)
                                {
                                    if (inv[i]?.HasContextTag("essence_item") ?? false)
                                    {
                                        essencesFound.Add(new(inv, inv[i]));
                                        totalFound += inv[i].Stack;
                                        if (totalFound >= 3)
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    bool skipCost = Game1.GetPlayer(from).HasCustomProfession(WitchcraftSkill.ProfessionNoTeleportCost);

                    if (totalFound < 3 && !skipCost)
                    {
                        retMsg.Error = "teleport-circle.error.missing-essence";
                    }
                    else
                    {
                        if (!skipCost)
                        {
                            int left = 3;
                            foreach (var (inv, item) in essencesFound)
                            {
                                left -= inv.ReduceId(item.QualifiedItemId, left);
                                if (left <= 0)
                                    break;
                            }
                        }

                        int slash = target.IndexOf('/');
                        int comma = target.IndexOf(',', slash + 1);
                        retMsg.LocationName = target[..slash];
                        retMsg.Tile = new Vector2(float.Parse(target.Substring(slash + 1, comma - slash - 1)), float.Parse(target[(comma + 1)..]));
                    }
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

                var retMsg = ProcessTeleportRequest(msg, e.FromPlayerID);
                Helper.Multiplayer.SendMessage(retMsg, TeleportInfoMessage, [ModManifest.UniqueID], [e.FromPlayerID]);
            }
            else if (e.Type == TeleportInfoMessage)
            {
                var msg = e.ReadAs<TeleportInfoMessage>();
                ProcessTeleport(msg);
            }
            else if (e.Type == MultiplayerMessage_Polymorph)
            {
                var msg = e.ReadAs<Vector2>();
                Spells.PolymorphImpl(Game1.GetPlayer(e.FromPlayerID), msg);
            }
            else if (e.Type == MultiplayerMessage_Banish)
            {
                var msg = e.ReadAs<Vector2>();
                Spells.BanishImpl(Game1.GetPlayer(e.FromPlayerID), msg);
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

            var familiar = Game1.player.companions.FirstOrDefault(c => c is FamiliarCompanion);
            if (familiar != null)
                Game1.player.RemoveCompanion(familiar);
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

            foreach (var polymorphed in ModSnS.State.Polymorphed.ToArray())
            {
                if (ModSnS.State.Banished.ContainsKey(polymorphed.Key))
                    continue;

                polymorphed.Value.Timer -= (float) Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (polymorphed.Value.Timer <= 0)
                {
                    ModSnS.State.Polymorphed.Remove(polymorphed.Key);
                    if (polymorphed.Key.currentLocation != null)
                    {
                        polymorphed.Value.Original.Position = polymorphed.Key.Position;
                        polymorphed.Key.currentLocation.characters.Add(polymorphed.Value.Original);
                        polymorphed.Key.currentLocation.characters.Remove(polymorphed.Key);
                    }
                }
            }

            foreach (var banished in ModSnS.State.Banished.ToArray())
            {
                banished.Value.Timer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (banished.Value.Timer <= 0)
                {
                    ModSnS.State.Banished.Remove(banished.Key);
                    if (banished.Key.currentLocation != null)
                        banished.Key.currentLocation.characters.Add(banished.Key);
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

        internal const int WitchcraftExpMultiplier = 3;

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
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 3 * WitchcraftExpMultiplier);
                    CastSpell(Color.LimeGreen, () => Spells.Haste());
                }
            });

            Ability.Abilities.Add("spell_polymorph", new Ability("spell_polymorph")
            {
                Name = I18n.Witchcraft_Spell_Polymorph_Name,
                Description = I18n.Witchcraft_Spell_Polymorph_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 3,
                ManaCost = () => 5,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_Polymorph",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 5 * WitchcraftExpMultiplier);
                    CastSpell(Color.LimeGreen, () => Spells.Polymorph());
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
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
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
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 5 * WitchcraftExpMultiplier);
                    CastSpell(Color.Orange, () => Spells.MageArmor());
                }
            });

            /*
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
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 5 * WitchcraftExpMultiplier);
                    CastSpell(Color.Orange, () => Spells.WallOfForce());
                }
            });
            */

            Ability.Abilities.Add("spell_banishment", new Ability("spell_banishment")
            {
                Name = I18n.Witchcraft_Spell_Banishment_Name,
                Description = I18n.Witchcraft_Spell_Banishment_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 7,
                ManaCost = () => 8,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_Banishment",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 8 * WitchcraftExpMultiplier);
                    CastSpell(Color.Orange, () => Spells.Banish());
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
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
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
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 15 * WitchcraftExpMultiplier);
                    CastSpell(Color.Yellow, () => Spells.MirrorImage());
                }
            });

            Ability.Abilities.Add("spell_findfamiliar", new Ability("spell_findfamiliar")
            {
                Name = I18n.Witchcraft_Spell_FindFamiliar_Name,
                Description = I18n.Witchcraft_Spell_FindFamiliar_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 15,
                ManaCost = () => 10,
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_FindFamiliar",
                UnlockHint = () => I18n.Ability_Witchcraft_SpellUnlockHint(),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
                    CastSpell(Color.Aqua, () => Spells.FindFamiliar());
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
                CanUse = () => !Game1.player.companions.Any(c => c is FamiliarCompanion),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 7 * WitchcraftExpMultiplier);
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
                    //Game1.player.AddCustomSkillExperience(ModTOP.Skill, 0 * WitchcraftExpMultiplier);
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
                    //Game1.player.AddCustomSkillExperience(ModTOP.Skill, 0 * WitchcraftExpMultiplier);
                    CastSpell(Color.Aqua, () => Spells.PocketDimension());
                }
            });

            Ability.Abilities.Add("spell_fireball", new Ability("spell_fireball")
            {
                Name = I18n.Witchcraft_Spell_Fireball_Name,
                Description = I18n.Witchcraft_Spell_Fireball_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 9,
                ManaCost = () => { return 10 - (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage) ? 2 : 0); },
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_Fireball",
                UnlockHint = I18n.Ability_Witchcraft_SpellUnlockHint,
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
                    var mousepos = Utility.PointToVector2(Game1.getMousePosition());
                    CastSpell(Color.Red, () => Spells.Fireball(mousepos));
                }
            });

            Ability.Abilities.Add("spell_icebolt", new Ability("spell_icebolt")
            {
                Name = I18n.Witchcraft_Spell_IceBolt_Name,
                Description = I18n.Witchcraft_Spell_IceBolt_Description,
                TexturePath = Projectile.projectileSheetName,
                SpriteIndex = 17,
                ManaCost = () => { return 10 - (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage) ? 2 : 0); },
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_Icebolt",
                UnlockHint = I18n.Ability_Witchcraft_SpellUnlockHint,
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
                    var mousepos = Utility.PointToVector2(Game1.getMousePosition());
                    CastSpell(Color.Blue, () => Spells.Icebolt(mousepos));
                }
            });

            Ability.Abilities.Add("spell_magicmissle", new Ability("spell_magicmissle")
            {
                Name = I18n.Witchcraft_Spell_MagicMissle_Name,
                Description = I18n.Witchcraft_Spell_MagicMissle_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 10,
                ManaCost = () => { return 10 - (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage) ? 2 : 0); },
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_MagicMissle",
                UnlockHint = I18n.Ability_Witchcraft_SpellUnlockHint,
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
                    CastSpell(Color.White, Spells.MagicMissle);
                }
                });

            Ability.Abilities.Add("spell_lightningbolt", new Ability("spell_lightningbolt")
            {
                Name = I18n.Witchcraft_Spell_LightningBolt_Name,
                Description = I18n.Witchcraft_Spell_LightningBolt_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/spells.png").Name,
                SpriteIndex = 8,
                ManaCost = () => { return 10 - (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage) ? 2 : 0); },
                KnownCondition = $"PLAYER_HAS_MAIL Current WitchcraftResearch_DN.SnS_Spell_LightningBolt",
                UnlockHint = I18n.Ability_Witchcraft_SpellUnlockHint,
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModTOP.Skill, 10 * WitchcraftExpMultiplier);
                    CastSpell(Color.Aquamarine, Spells.LightningBolt);
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
            int mult = Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionEssenceDrops) ? 2 : 1;

            long farmerId = who?.UniqueMultiplayerID ?? 0;
            if (who != null && Game1.random.NextDouble() < 0.15 * mult)
            {
                Game1.createObjectDebris("(O)DN.SnS_EarthEssence", x, y, farmerId, __instance);
                Game1.createObjectDebris("(O)DN.SnS_EarthEssence", x, y, farmerId, __instance);
            }
        }
    }

    [HarmonyPatch(typeof(FishingRod), "doneFishing")]
    public static class DropWaterEssencePatch1
    {
        public static void Postfix(Farmer who, bool consumeBaitAndTackle)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;
            int mult = Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionEssenceDrops) ? 2 : 1;

            long farmerId = who?.UniqueMultiplayerID ?? 0;
            if (Game1.random.NextDouble() < .35 * mult)
            {
                Game1.createObjectDebris("(O)DN.SnS_WaterEssence", (int)who.Tile.X, (int)who.Tile.Y, farmerId, who.currentLocation);
                Game1.createObjectDebris("(O)DN.SnS_WaterEssence", (int)who.Tile.X, (int)who.Tile.Y, farmerId, who.currentLocation);
            }
        }
    }

    [HarmonyPatch(typeof(Pan), nameof(Pan.getPanItems))]
    public static class DropWaterEssencePatch2
    {
        public static void Postfix(List<Item> __result)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;
            int mult = Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionEssenceDrops) ? 2 : 1;

            __result.Add(ItemRegistry.Create("(O)DN.SnS_WaterEssence", mult * 3));
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    public static class DropVariousMonsterEssencesPatch
    {
        public static void Postfix(GameLocation __instance, Monster monster, int x, int y)
        {
            if (!Game1.player.eventsSeen.Contains("SnS.Ch4.Roslin.1"))
                return;
            int mult = Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionEssenceDrops) ? 2 : 1;

            if (monster.isGlider.Value && Game1.random.NextDouble() < 0.25 * mult)
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
            if (__instance is VolcanoDungeon && Game1.random.NextDouble() < 0.25 * mult)
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_FireEssence"), new(x, y), 2, __instance);
            }
        }
    }

    // NOTE: Transparency for mirror image rendering is in the Shadowstep patch (since they share the transparency variable)

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public static class FarmerMirrorImageDamagePatch
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        public static bool Prefix(Farmer __instance, ref int damage, bool overrideParry, Monster damager)
        {
            var ext = Game1.player.GetFarmerExtData();
            if (__instance != Game1.player || overrideParry || !Game1.player.CanBeDamaged() ||
                ext.mirrorImages.Value <= 0)
                return true;

            bool flag = (damager == null || !damager.isInvincible()) && (damager == null || (damager is not GreenSlime && damager is not BigSlime) || !__instance.isWearingRing("520"));
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
                    var ret = ModTOP.ProcessTeleportRequest(req, Game1.player.UniqueMultiplayerID);
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
        [HarmonyPriority(Priority.HigherThanNormal)]
        public static bool Prefix(Farmer __instance)
        {
            if (__instance.GetFarmerExtData().stasisTimer.Value > 0)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.drawAboveAlwaysFrontLayer))]
    public static class GameLocationDrawBanishedPart2Patch
    {
        public static void Postfix(GameLocation __instance, SpriteBatch b)
        {
            try
            {
                ModTOP.drawingBanished = true;
                foreach (var monster in ModSnS.State.Banished.Where(kvp => kvp.Value.Location == __instance))
                {
                    monster.Key.drawAboveAlwaysFrontLayer(b);
                    monster.Key.drawAboveAllLayers(b);
                }
            }
            finally
            {
                ModTOP.drawingBanished = false;
            }
        }
    }
}
