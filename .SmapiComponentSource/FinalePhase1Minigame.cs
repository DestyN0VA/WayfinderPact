using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NeverEndingAdventure.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwordAndSorcerySMAPI
{
    [HarmonyPatch(typeof(GameLocation), "drawFarmers")]
    public class GameLocationDrawFarmerInFinalePatch
    {
        public static void Postfix(GameLocation __instance, SpriteBatch b)
        {
            if (Game1.currentMinigame is FinalePhase1Minigame)
                Game1.player.draw(b);
        }
    }

    public class BattlerInfo
    {
        public int Health { get; set; } = 100;
        public int BaseDefense { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }

        public Action<float> AbilityFunc { get; set; } = (_) => { };
        public Func<string> AbilityName { get; set; } = () => "todo";
        public Func<string> AbilityDescription { get; set; } = () => "not implemented";
        public int AbilityManaCost { get; set; } = 10;

        public int Defense => BaseDefense * (Guarding ? 1 : 2) + (Guarding ? 10 : 0);
        public bool InShadowstep { get; set; } = false;
        public bool Guarding { get; set; } = false;
        public int TransformedCounter { get; set; } = 0;

        public Vector2 BasePosition { get; set; }
        public string OrigTexture { get; set; }
    }

    public class BattleProjectile
    {
        public Texture2D Texture { get; set; }
        public Rectangle SourceRect { get; set; }

        public Vector2 Position { get; set; }
        public float Rotation { get; set; }

        public Vector2 Target { get; set; }
        public float MovementSpeed { get; set; } = 4;
        public float RotationSpeed { get; set; } = 10 * MathF.PI / 180;
        public Vector2? ReturnTo { get; set; }

        public Action OnHit { get; set; }
    }

    internal class FinalePhase1Minigame : IMinigame
    {
        public int TurnCounter = 0;
        public bool Finished { get; set; } = false;
        public Event Event { get; }
        public EventContext Context { get; }

        public Texture2D BossSprite { get; set; }

        public Character CurrentTurn { get; set; }
        public BattlerInfo CurrentBattler => BattlerData.TryGetValue(CurrentTurn.Name, out var info) ? info : DuskspireDummyInfo;

        public List<Character> Battlers { get; set; } = new();
        public Dictionary<string, BattlerInfo> BattlerData { get; set; } = new();

        public List<BattleProjectile> Projectiles { get; set; } = new();

        private NPC DuskspireActor { get; set; }
        private BattlerInfo DuskspireDummyInfo { get; set; } = new();
        private int DuskspireHealth { get; set; } = 1000;
        public float DuskspireFrameTimer { get; set; } = 0;
        public int? DuskspireFrameOverride { get; set; }
        public bool DuskspireStartedTurn { get; set; } = false;

        private int CurrentChoice { get; set; } = 0;
        private float CurrentForward { get; set; }
        private int MovingActorForTurn { get; set; } = -2; // -1 = backward, 0 = doing action, 1 = forward, anything else = not doing anything
        private Action PendingAction { get; set; }
        private int PotionCount { get; set; } = 3;

        private BattlerInfo ChooseNonFarmerAlly()
        {
            List<BattlerInfo> choices = new(BattlerData.Where( kvp => kvp.Key != Game1.player.Name ).Select( kvp => kvp.Value ) );
            return choices[new Random((int)(Game1.player.UniqueMultiplayerID + TurnCounter)).Next(choices.Count)];

        }

        public FinalePhase1Minigame(Event @event, EventContext context)
        {
            Event = @event;
            Context = context;

            BossSprite = Game1.content.Load<Texture2D>("Characters/Monsters/Angry Roger");

            CurrentTurn = @event.actors.First();

            Game1.fadeToBlack = false; // WHY WAS THIS KEEPING THINGS FROM TICKING

            Battlers.AddRange(@event.actors);
            Battlers.Insert(1, Game1.player);

            BattlerData = new()
            {
                { "Mateo", new BattlerInfo()
                    {
                        BaseDefense = 8,
                        Mana = 30,
                        MaxMana = 30,
                        AbilityFunc = (_) =>
                        {
                            CurrentBattler.InShadowstep = true;
                            CurrentTurn.startGlowing( Color.Black, false, 0.05f );
                            --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_Shadowstep_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_Shadowstep_Description,
                        AbilityManaCost = 10,
                    }
                },
                { Game1.player.Name, new BattlerInfo()
                    {
                        BaseDefense = Game1.player.buffs.Defense + (Game1.player.GetArmorItem()?.GetArmorAmount() ?? 0) / 25,
                        Mana = Game1.player.GetFarmerExtData().mana.Value,
                        MaxMana = Game1.player.GetFarmerExtData().maxMana.Value,
                        AbilityFunc = (time) =>
                        {
                            ChooseNonFarmerAlly().AbilityFunc(time);
                        },
                        AbilityName = () => ChooseNonFarmerAlly().AbilityName(),
                        AbilityDescription = () => ChooseNonFarmerAlly().AbilityDescription(),
                        AbilityManaCost = 15,
                    }
                },
                { "Dandelion", new BattlerInfo()
                    {
                        BaseDefense = 12,
                        Mana = 40,
                        MaxMana = 40,
                        AbilityFunc = (_) =>
                        {
                            Projectiles.Add( new BattleProjectile()
                            {
                                Texture = ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_PaladinShield").GetTexture(),
                                SourceRect = ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_PaladinShield").GetSourceRect(),
                                Position = CurrentTurn.StandingPixel.ToVector2() - new Vector2( 0, 48 ),
                                Target = Battlers.First( c => c.Name == "Duskspire" ).StandingPixel.ToVector2() - new Vector2( 0, 128 ),
                                ReturnTo = CurrentTurn.StandingPixel.ToVector2() - new Vector2( 0, 48 ),
                                OnHit = () =>
                                {
                                    DuskspireHealth -= 30;
                                    DuskspireFrameOverride = 25;
                                    Game1.playSound("serpentHit");
                                }
                            } );
                             --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_ShieldThrow_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_ShieldThrow_Description,
                        AbilityManaCost = 10,
                    }
                },
                { "Hector", new BattlerInfo()
                    {
                        BaseDefense = 3,
                        Mana = 75,
                        MaxMana = 75,
                        AbilityFunc = (_) =>
                        {
                            CurrentBattler.TransformedCounter = 3;
                            CurrentBattler.BaseDefense += 10;

                            if (CurrentTurn == Game1.player)
                            {
                                CircleOfThornsSMAPI.ModCoT.farmerData.GetOrCreateValue(Game1.player).transformed.Value = true;
                            }
                            else
                            {
                                CurrentBattler.OrigTexture = CurrentTurn.Sprite.textureName.Value;
                                var modInfo = ModSnS.instance.Helper.ModRegistry.Get("DN.SnS");
                                var pack = modInfo.GetType().GetProperty("ContentPack")?.GetValue(modInfo) as IContentPack;
                                CurrentTurn.Sprite.LoadTexture( pack.ModContent.GetInternalAssetName( "Assets/TemporaryActors/Characters/HectorWolf.png" ).BaseName );
                                CurrentTurn.Sprite.SpriteWidth = 32;
                                CurrentTurn.Sprite.SpriteHeight = 32;
                                CurrentTurn.Sprite.CurrentFrame = 4;
                                CurrentTurn.Sprite.UpdateSourceRect();
                            }
                             --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_Shapeshift_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_Shapeshift_Description,
                        AbilityManaCost = 25,
                    }
                },
                { "Cirrus", new BattlerInfo()
                    {
                        BaseDefense = 0,
                        Mana = 50,
                        MaxMana = 50,
                        AbilityFunc = (_) =>
                        {
                            foreach ( var battler in BattlerData )
                            {
                                if (battler.Value.Health == 0 )
                                {
                                    Battlers.First(c => c.Name == battler.Key).stopGlowing();
                                }
                                battler.Value.Health = Math.Min( battler.Value.Health + 50, 100 );
                            }
                            Game1.playSound("healSound");
                             --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_SongHealing_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_SongHealing_Description,
                        AbilityManaCost = 25,
                    }
                },
                { "Roslin", new BattlerInfo()
                    {
                        BaseDefense = 0,
                        Mana = 100,
                        MaxMana = 100,
                        AbilityFunc = (_) =>
                        {
                            Projectiles.Add( new BattleProjectile()
                            {
                                Texture = Projectile.projectileSheet,
                                SourceRect = new Rectangle( 32, 16, 16, 16 ),
                                Position = CurrentTurn.StandingPixel.ToVector2() - new Vector2( 0, 48 ),
                                Target = Battlers.First( c => c.Name == "Duskspire" ).StandingPixel.ToVector2(),
                                OnHit = () =>
                                {
                                    DuskspireHealth -= 50;
                                    DuskspireFrameOverride = 25;
                                    Game1.playSound("serpentHit");
                                    Game1.playSound("explosion");
                                }
                            } );
                             --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_Fireball_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_Fireball_Description,
                        AbilityManaCost = 25,
                    }
                },
                { "MadDog.HashtagBearFam.Gunnar", new BattlerInfo()
                    {
                        BaseDefense = 5,
                        Mana = 25,
                        MaxMana = 25,
                        AbilityFunc = (_) =>
                        {
                            Projectiles.Add( new BattleProjectile()
                            {
                                Texture = ItemRegistry.GetDataOrErrorItem("(O)287").GetTexture(),
                                SourceRect = ItemRegistry.GetDataOrErrorItem("(O)287").GetSourceRect(),
                                Position = CurrentTurn.StandingPixel.ToVector2() - new Vector2( 0, 48 ),
                                Target = Battlers.First( c => c.Name == "Duskspire" ).StandingPixel.ToVector2(),
                                OnHit = () =>
                                {
                                    DuskspireHealth -= 50;
                                    DuskspireFrameOverride = 25;
                                    Game1.playSound("serpentHit");
                                    Game1.playSound("explosion");
                                }
                            } );
                             --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_BombThrow_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_BombThrow_Description,
                        AbilityManaCost = 5,
                    }
                },
            };

            foreach (var actor in @event.actors.ToList())
            {
                if (actor.Name == "Duskspire")
                {
                    DuskspireActor = actor;
                    DuskspireActor.Sprite.LoadTexture(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth.png").BaseName);
                    DuskspireActor.Sprite.SourceRect = new Rectangle(0, 7 * 96, 96, 96);
                    DuskspireActor.Sprite.SpriteWidth = DuskspireActor.Sprite.SpriteHeight = 96;
                    DuskspireActor.Sprite.CurrentFrame = 28;
                    continue;
                }

                if (BattlerData.TryGetValue(actor.Name, out var data))
                {
                    data.BasePosition = actor.Position;
                    data.OrigTexture = actor.Sprite.textureName.Value;
                }
                else @event.actors.Remove(actor);
            }
            BattlerData[Game1.player.Name].BasePosition = Game1.player.Position;

            foreach (var battler in BattlerData.ToArray())
            {
                if (battler.Value.BasePosition == default(Vector2)) // Optional NPC isn't in the event (Gunnar if Bearfam isn't installed)
                {
                    BattlerData.Remove(battler.Key);
                }
            }
        }

        public string minigameId()
        {
            return $"{ModSnS.instance.ModManifest.UniqueID}_finale_phase1";
        }

        public void changeScreenSize()
        {
        }
        public void receiveKeyPress(Keys k)
        {
            if (CurrentTurn.Name == "Duskspire" || MovingActorForTurn >= -1 && MovingActorForTurn <= 1)
                return;

            if (k == Keys.Up)
            {
                if (--CurrentChoice < 0)
                    CurrentChoice += 4;
            }
            else if (k == Keys.Down)
            {
                if (++CurrentChoice > 3)
                    CurrentChoice -= 4;
            }
            else if (k == Utility.mapGamePadButtonToKey(Buttons.A))
            {
                float delay = 0;
                switch (CurrentChoice)
                {
                    case 0: // Attack
                        delay = 0.5f;
                        PendingAction = () =>
                        {
                            if ( delay == 0.5f )
                                CurrentTurn.jumpWithoutSound();

                            float oldDelay = delay;
                            delay -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                            if (oldDelay > 0 && delay <= 0)
                            {
                                DuskspireActor.shake(500);
                                DuskspireHealth -= CurrentBattler.InShadowstep ? 50 : (CurrentBattler.TransformedCounter > 0 ? 30 : 10);
                                DuskspireFrameOverride = 25;
                                Game1.playSound("serpentHit");
                                if (CurrentBattler.InShadowstep)
                                {
                                    Game1.playSound("crit");
                                    CurrentBattler.InShadowstep = false;
                                    CurrentTurn.stopGlowing();
                                }
                            }
                            else if (delay <= -0.5f)
                            {
                                MovingActorForTurn--;
                            }
                        };
                        MovingActorForTurn = 1;
                        break;
                    case 1: // Ability
                        delay = 0;
                        CurrentBattler.Mana -= CurrentBattler.AbilityManaCost;
                        PendingAction = () =>
                        {
                            CurrentBattler.AbilityFunc(-delay);
                            delay -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                        };
                        MovingActorForTurn = 1;
                        break;
                    case 2: // Guard
                        delay = 0.5f;
                        PendingAction = () =>
                        {
                            if (delay == 0.5f)
                            {
                                CurrentBattler.Guarding = true;
                                CurrentTurn.startGlowing(Color.LightSlateGray, false, 0.05f);
                                CurrentTurn.glowRate = 0.05f;
                                Game1.playSound("clank");
                            }

                            delay -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                            if (delay <= 0)
                            {
                                MovingActorForTurn--;
                            }
                        };
                        MovingActorForTurn = 0;
                        break;
                    case 3: // Use potion
                        MovingActorForTurn = 1;
                        delay = 0.5f;
                        PendingAction = () =>
                        {
                            if (delay == 0.5f)
                            {
                                CurrentBattler.Health = 100;
                                --PotionCount;
                                Game1.playSound("healSound");
                            }

                            delay -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                            if (delay <= 0)
                            {
                                MovingActorForTurn--;
                            }
                        };
                        break;
                    default:
                        Log.Warn("Invalid choice??");
                        PendingAction = () => { MovingActorForTurn--; };
                        MovingActorForTurn = 1;
                        break;
                }
            }
        }

        public void receiveKeyRelease(Keys k)
        {
        }

        public void receiveLeftClick(int x, int y, bool playSound = true)
        {
            receiveKeyPress(Utility.mapGamePadButtonToKey(Buttons.A));
        }

        public void receiveRightClick(int x, int y, bool playSound = true)
        {
            Finished = true;
        }

        public void releaseLeftClick(int x, int y)
        {
        }

        public void releaseRightClick(int x, int y)
        {
        }

        private void NextTurn()
        {
            ++TurnCounter;
            DuskspireStartedTurn = false;

            if (CurrentTurn == Battlers.Last())
            {
                CurrentTurn = Battlers.First();
            }
            else
            {
                CurrentTurn = Battlers[Battlers.IndexOf(CurrentTurn) + 1];
            }

            if (CurrentBattler.Guarding)
            {
                CurrentBattler.Guarding = false;
                CurrentTurn.stopGlowing();
            }

            if (CurrentBattler.TransformedCounter > 0 && --CurrentBattler.TransformedCounter == 0)
            {
                CurrentBattler.BaseDefense -= 10;

                if (CurrentTurn == Game1.player)
                {
                    CircleOfThornsSMAPI.ModCoT.farmerData.GetOrCreateValue(Game1.player).transformed.Value = false;
                }
                else
                {
                    CurrentTurn.Sprite.LoadTexture(CurrentBattler.OrigTexture);
                    CurrentTurn.Sprite.SpriteWidth = 16;
                    CurrentTurn.Sprite.SpriteHeight = 32;
                    CurrentTurn.Sprite.CurrentFrame = 4;
                    CurrentTurn.Sprite.UpdateSourceRect();
                }
            }

            if (!BattlerData.Values.Any(b => b.Health > 0))
            {
                Finished = true;
            }

            if (CurrentBattler.Health <= 0)
            {
                NextTurn();
            }
        }

        private bool waitingForProjectiles = false;
        public bool tick(GameTime time)
        {
            foreach (var character in Battlers)
            {
                character.update(time, Game1.currentLocation);
                if (character is Farmer f)
                {
                    ModSnS.instance.Helper.Reflection.GetMethod(f, "updateCommon").Invoke(time, Game1.currentLocation);
                }
            }

            if (CurrentTurn == DuskspireActor)
            {
                if (!DuskspireStartedTurn)
                {
                    DuskspireStartedTurn = true;
                    DuskspireActor.Sprite.CurrentFrame = 40;
                    DuskspireFrameTimer = 0;
                }
                else
                {
                    DuskspireFrameTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
                    if (DuskspireFrameTimer >= 50)
                    {
                        DuskspireFrameTimer -= 50;
                        if (++DuskspireActor.Sprite.CurrentFrame == 47)
                        {
                            List<Character> choices = Battlers.Where(c => c != DuskspireActor && !BattlerData[c.Name].InShadowstep && BattlerData[c.Name].Health > 0).ToList();
                            for (int i = 0; i < choices.Count; ++i)
                            {
                                int ind = Game1.random.Next(choices.Count);
                                Character old = choices[i];
                                choices[i] = choices[ind];
                                choices[ind] = old;
                            }

                            if (choices.Count > 0)
                            {
                                int ind = 0;
                                if (choices[ind] is Farmer f) f.jitterStrength = 10;
                                else if (choices[ind] is NPC n) n.shake(250);
                                BattlerData[choices[ind].Name].Health -= Math.Max(1, 40 - BattlerData[choices[ind].Name].Defense);

                                if (BattlerData[choices[ind].Name].Health <= 0)
                                {
                                    choices[ind].startGlowing(Color.DarkRed, false, 0.01f);
                                }
                            }
                            if (choices.Count > 1)
                            {
                                int ind = 1;
                                if (choices[ind] is Farmer f) f.jitterStrength = 30;
                                else if (choices[ind] is NPC n) n.shake(250);
                                BattlerData[choices[ind].Name].Health -= Math.Max(1, 40 - BattlerData[choices[ind].Name].Defense);

                                if (BattlerData[choices[ind].Name].Health <= 0)
                                {
                                    choices[ind].startGlowing(Color.DarkRed, false, 0.01f);
                                }
                            }
                        }
                        else if (DuskspireActor.Sprite.CurrentFrame == 50)
                        {
                            DuskspireActor.Sprite.CurrentFrame = 28;
                            Game1.player.stopJittering();
                            NextTurn();
                        }
                    }
                }
            }

            if (MovingActorForTurn == 1)
            {
                CurrentTurn.SetMovingRight(true);
                CurrentTurn.MovePosition(time, Game1.viewport, Game1.currentLocation);
                if ((CurrentTurn.Position - CurrentBattler.BasePosition).X >= 64)
                {
                    CurrentTurn.Halt();
                    MovingActorForTurn--;
                }
            }
            else if (MovingActorForTurn == -1)
            {
                CurrentTurn.SetMovingLeft(true);
                CurrentTurn.MovePosition(time, Game1.viewport, Game1.currentLocation);
                if ((CurrentTurn.Position - CurrentBattler.BasePosition).X <= 0)
                {
                    CurrentTurn.Halt();
                    CurrentTurn.faceDirection(Game1.right);
                    MovingActorForTurn--;

                    if (Projectiles.Count > 0)
                    {
                        waitingForProjectiles = true;
                    }
                    else
                    {
                        NextTurn();
                    }
                }
            }
            else if (MovingActorForTurn == 0)
            {
                PendingAction();
            }

            foreach (var proj in Projectiles.ToList())
            {
                if (Vector2.Distance(proj.Position, proj.Target) <= proj.MovementSpeed)
                {
                    proj.OnHit();
                    proj.OnHit = () => { };
                    if (proj.ReturnTo.HasValue)
                    {
                        proj.Target = proj.ReturnTo.Value;
                        proj.ReturnTo = null;
                    }
                    else
                    {
                        Projectiles.Remove(proj);
                    }
                }
                else
                {
                    var diff = (proj.Target - proj.Position);
                    diff.Normalize();
                    proj.Position += diff * proj.MovementSpeed;
                }
            }

            if (waitingForProjectiles && Projectiles.Count == 0)
            {
                waitingForProjectiles = false;
                NextTurn();
            }

            if (DuskspireHealth <= 0)
            {
                Finished = true;
            }

            return Finished;
        }

        public void draw(SpriteBatch b)
        {
            Point windowSize = new(Game1.game1.Window.ClientBounds.Width, Game1.game1.Window.ClientBounds.Height);

            Game1.viewportCenter = new Point(14 * Game1.tileSize, 13 * Game1.tileSize);
            Game1.UpdateViewPort(overrideFreeze: true, Game1.viewportCenter);

            /*
            b.Begin();
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, windowSize.X, windowSize.Y), Color.Black);
            b.End();
            */

            if (CurrentTurn != DuskspireActor)
            {
                DuskspireFrameTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
                if (DuskspireFrameTimer >= 75)
                {
                    DuskspireFrameTimer -= 75;
                    DuskspireActor.Sprite.CurrentFrame += 1;
                    if (DuskspireActor.Sprite.CurrentFrame < 28 || DuskspireActor.Sprite.CurrentFrame >= 28 + 9)
                    {
                        DuskspireActor.Sprite.CurrentFrame = 28;
                    }
                }
                if (DuskspireFrameOverride.HasValue)
                {
                    DuskspireActor.Sprite.CurrentFrame = DuskspireFrameOverride.Value;
                    DuskspireFrameTimer = -75;
                    DuskspireFrameOverride = null;
                }
                DuskspireActor.Sprite.UpdateSourceRect();
            }

            Game1.currentLocation.Map.Update(Game1.currentGameTime.ElapsedGameTime.Milliseconds);
            Game1.game1.DrawWorld(Game1.currentGameTime, Game1.game1.ShouldDrawOnBuffer() ? Game1.game1.screen : null);

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            foreach (var proj in Projectiles)
            {
                proj.Rotation += proj.RotationSpeed;
                b.Draw(proj.Texture, Game1.GlobalToLocal( proj.Position ), proj.SourceRect, Color.White, proj.Rotation, proj.SourceRect.Size.ToVector2() / 2, 4, SpriteEffects.None, 1);
            }

            /*
            Vector2 bossPos = new Vector2(18, 13);
            Rectangle bossSrc = new Rectangle(0, 96, 32, 32);
            bossPos *= Game1.tileSize;
            b.Draw(BossSprite, Game1.GlobalToLocal(bossPos - new Vector2((bossSrc.Width - 16) / 2, bossSrc.Height - 16) * Game1.pixelZoom), bossSrc, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, bossPos.Y / 10000f);
            */

            if (CurrentTurn != null & CurrentTurn != DuskspireActor)
            {
                var rect = new Rectangle(324, 477, 7, 19);
                b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(CurrentTurn.StandingPixel.ToVector2() - new Vector2(12, 200 + 8 * MathF.Sin( (float) Game1.currentGameTime.TotalGameTime.TotalSeconds * 4 ))), rect, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.FlipVertically, 1);

                int y = Game1.viewport.Height - 200 - 16 - 64;

                //IClickableMenu.drawTextureBox(b, 16, y, 275, 80, Color.White);
                IClickableMenu.drawTextureBox(b, 16, y + 64, 450, 200, Color.White);

                SpriteText.drawStringWithScrollCenteredAt(b, CurrentTurn.displayName, 16 + 450 / 2, y, 350);
                SpriteText.drawString(b, $"<{CurrentBattler.Health}/100^={CurrentBattler.Mana}/{CurrentBattler.MaxMana}", 250, y + 16 + 64);

                string[] choices = ["Attack", "Ability", "Guard", $"Use Potion   ({PotionCount} left)"];

                for (int i = 0; i < choices.Length; ++i)
                {
                    string prefix = "   ";
                    if (i == CurrentChoice)
                        prefix = "> ";

                    Utility.drawTextWithShadow(b, $"{prefix}{choices[i]}", Game1.dialogueFont, new Vector2(40, y + 80 + 40 * i), Color.Black);
                }

                int x = Game1.viewport.Width - 450 - 16;

                if (CurrentChoice == 1)
                {
                    IClickableMenu.drawTextureBox(b, x, y + 64, 450, 200, Color.White);

                    SpriteText.drawStringWithScrollCenteredAt(b, $"{CurrentBattler.AbilityName()} ={CurrentBattler.AbilityManaCost}", x + 450 / 2 - 50, y, 450);
                    Utility.drawTextWithShadow(b, Game1.parseText(CurrentBattler.AbilityDescription(), Game1.smallFont, 450 - 32 * 2), Game1.smallFont, new Vector2( x + 32, y + 96 ), Color.Black );
                }
            }

            //SpriteText.drawStringHorizontallyCenteredAt(b, "Click to simulate victory", windowSize.X / 2, windowSize.Y / 2);

            b.End();
        }

        public bool doMainGameUpdates()
        {
            return false;
        }

        public bool forceQuit()
        {
            return false;
        }

        public void leftClickHeld(int x, int y)
        {
        }

        public bool overrideFreeMouseMovement()
        {
            return false;
        }

        public void receiveEventPoke(int data)
        {
        }

        public void unload()
        {
            if (DuskspireHealth > 0)
            {
                Game1.player.health = -999;
                Game1.player.eventsSeen.Remove(Event.id);

                var cmds_ = new List<string>(Event.eventCommands);
                cmds_.Insert(Event.CurrentCommand + 1, $"end");
                Event.eventCommands = cmds_.ToArray();

                Event.CurrentCommand++;
                return;
            }

            // This is really bad. Pathos don't kill me.
            var modInfo = ModSnS.instance.Helper.ModRegistry.Get("DN.SnS");
            var pack = modInfo.GetType().GetProperty("ContentPack")?.GetValue(modInfo) as IContentPack;
            var partnerInfos = pack.ReadJsonFile<Dictionary<string, FinalePartnerInfo>>("Data/FinalePartners.json");

            FinalePartnerInfo partnerInfo;
            if (Game1.player.spouse == null || !partnerInfos.TryGetValue(Game1.player.spouse, out partnerInfo))
                partnerInfo = partnerInfos["default"];

            var commands = new List<string>(Event.eventCommands);
            commands.Insert(Event.CurrentCommand + 1, $"switchEventFull {partnerInfo.IntermissionEventId}");
            Event.eventCommands = commands.ToArray();

            Event.CurrentCommand++;
        }
    }
}