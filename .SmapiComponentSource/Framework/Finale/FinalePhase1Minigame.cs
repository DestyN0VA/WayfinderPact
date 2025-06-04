using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Projectiles;
using SwordAndSorcerySMAPI.Deprecated;
using SwordAndSorcerySMAPI.Framework.NEA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI.Framework.Finale
{
    [HarmonyPatch(typeof(GameLocation), "drawFarmers")]
    public class GameLocationDrawFarmerInFinalePatch
    {
        public static void Postfix(SpriteBatch b)
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

        public Character CurrentTurn { get; set; }
        public BattlerInfo CurrentBattler => BattlerData.TryGetValue(CurrentTurn.Name, out var info) ? info : DuskspireDummyInfo;
        public string OrigRoslinTexture { get; set; }

        public List<Character> Battlers { get; set; } = [];
        public Dictionary<string, BattlerInfo> BattlerData { get; set; } = [];

        public List<BattleProjectile> Projectiles { get; set; } = [];

        private NPC DuskspireActor { get; set; }
        private BattlerInfo DuskspireDummyInfo { get; set; } = new();
        private int DuskspireHealth { get; set; } = 1000;
        public int? DuskspireFrameOverride { get; set; }
        public bool DuskspireStartedTurn { get; set; } = false;
        public int DuskspireLoop = 1;

        private int CurrentChoice { get; set; } = 0;
        //private float CurrentForward { get; set; } Unused?
        private int MovingActorForTurn { get; set; } = -2; // -1 = backward, 0 = doing action, 1 = forward, anything else = not doing anything
        private Action PendingAction { get; set; }
        private int PotionCount { get; set; } = 3;

        private BattlerInfo ChooseNonFarmerAlly()
        {
            List<BattlerInfo> choices = new(BattlerData.Where(kvp => kvp.Key != Game1.player.Name && kvp.Key != "Hector").Select(kvp => kvp.Value));
            return choices[new Random((int)(Game1.player.UniqueMultiplayerID + TurnCounter + Game1.stats.DaysPlayed)).Next(choices.Count)];

        }


        public FinalePhase1Minigame(Event @event, EventContext context)
        {
            Event = @event;
            Context = context;

            CurrentTurn = @event.actors.First();

            Game1.fadeToBlack = false; // WHY WAS THIS KEEPING THINGS FROM TICKING

            BattlerData = new()
            {
                { "Val", new BattlerInfo()
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
                                Target = Battlers.First(c => c.Name == "Duskspire").StandingPixel.ToVector2() - new Vector2( 0, 128 ),
                                ReturnTo = CurrentTurn.StandingPixel.ToVector2() - new Vector2( 0, 48 ),
                                OnHit = () =>
                                {
                                    DuskspireHealth -= 30;
                                    DuskspireFrameOverride = 25;
                                    Game1.playSound("serpentHit");
                                }
                            });
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
                                ModCoT.farmerData.GetOrCreateValue(Game1.player).transformed.Value = true;
                            }
                            else
                            {
                                CurrentBattler.OrigTexture = CurrentTurn.Sprite.textureName.Value;
                                CurrentTurn.Sprite.LoadTexture("Characters/Hector_Wolf");
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
                                if (battler.Value.Health <= 0 )
                                {
                                    continue;
                                    //Battlers.First(c => c.Name == battler.Key).stopGlowing();
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
                { "SenS", new BattlerInfo()
                    {
                        BaseDefense = 3,
                        Mana = 50,
                        MaxMana = 50,
                        AbilityFunc = (_) =>
                        {
                            Game1.playSound("shadowpeep");

                            string[] SenRandomBooks = ["(O)Book_Void", "(O)Book_Trash", "(O)PurpleBook", "(O)SkillBook_0", "(O)SkillBook_1", "(O)SkillBook_2", "(O)SkillBook_3", "(O)SkillBook_4", "(O)Book_Crabbing", "(O)Book_Bombs", "(O)Book_Roe", "(O)Book_WildSeeds", "(O)Book_Woodcutting", "(O)Book_Woodcutting", "(O)Book_Defense", "(O)Book_Friendship", "(O)Book_Speed", "(O)Book_Speed2", "(O)Book_Marlon", "(O)Book_PriceCatalogue", "(O)Book_QueenOfSauce", "(O)Book_Diamonds", "(O)Book_Mystery", "(O)Book_AnimalCatalogue", "(O)Book_Artifact", "(O)Book_Horse", "(O)Book_Grass"];
                            string GetRandomBook()
                            {
                                Random randomizer = Game1.random;
                                return SenRandomBooks[randomizer.Next(SenRandomBooks.Length)];
                            }

                            Projectiles.Add( new BattleProjectile()
                            {
                                Texture = ItemRegistry.GetDataOrErrorItem(GetRandomBook()).GetTexture(),
                                SourceRect = ItemRegistry.GetDataOrErrorItem(GetRandomBook()).GetSourceRect(),
                                Position = CurrentTurn.StandingPixel.ToVector2() - new Vector2( 0, 48 ),
                                Target = Battlers.First( c => c.Name == "Duskspire" ).StandingPixel.ToVector2(),
                                OnHit = () =>
                                {
                                    DuskspireHealth -= 30;
                                    //DuskspireFrameOverride = 25;
                                    Game1.playSound("woodWhack");

                                }
                            } );
                             --MovingActorForTurn;
                        },
                        AbilityName = I18n.FinaleMinigame_Ability_StolenLibraryBook_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_StolenLibraryBook_Description,
                        AbilityManaCost = 10,
                    }
                },
            };

            foreach (var actor in @event.actors.ToList())
            {
                if (actor.Name == "Duskspire")
                {
                    DuskspireActor = actor;
                    DuskspireActor.Sprite.LoadTexture(ModSnS.Instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth.png").BaseName);
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

            Battlers.AddRange(@event.actors);
            Battlers.Insert(1, Game1.player);
            Character c = Battlers.First(c => c.Name == "Roslin");
            c.Sprite.SpriteWidth = 43;
            c.Sprite.SpriteHeight = 45;
            OrigRoslinTexture = c.Sprite.textureName.Value;
            c.Sprite.LoadTexture("Characters/FakeSolomon");
            c.Sprite.UpdateSourceRect();
            c.Position += new Vector2(-64f, -13f);
            BattlerData["Roslin"].BasePosition = c.Position;


            foreach (var battler in BattlerData.ToArray())
            {
                if (battler.Value.BasePosition == default) // Optional NPC isn't in the event (Gunnar & Sen)
                {
                    BattlerData.Remove(battler.Key);
                }
            }
        }

        public string minigameId()
        {
            return $"{ModSnS.Instance.ModManifest.UniqueID}_finale_phase1";
        }

        public void changeScreenSize()
        {
        }

        public void receiveKeyPress(Keys k)
        {
            if (CurrentTurn.Name == "Duskspire" || MovingActorForTurn >= -1 && MovingActorForTurn <= 1 || waitingForProjectiles)
                return;

            if (k == Keys.Up || k == Keys.W || k == Utility.mapGamePadButtonToKey(Buttons.DPadUp) || k == Utility.mapGamePadButtonToKey(Buttons.LeftThumbstickUp))
            {
                if (--CurrentChoice < 0)
                    CurrentChoice += 4;
            }
            else if (k == Keys.Down || k == Keys.S || k == Utility.mapGamePadButtonToKey(Buttons.DPadDown) || k == Utility.mapGamePadButtonToKey(Buttons.LeftThumbstickDown))
            {
                if (++CurrentChoice > 3)
                    CurrentChoice -= 4;
            }
            else if (k == Utility.mapGamePadButtonToKey(Buttons.A) || k == Keys.Enter)
            {
                float delay = 0;
                switch (CurrentChoice)
                {
                    case 0: // Attack
                        delay = 0.5f;
                        PendingAction = () =>
                        {
                            if (delay == 0.5f)
                                CurrentTurn.jumpWithoutSound();

                            float oldDelay = delay;
                            delay -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                            if (oldDelay > 0 && delay <= 0)
                            {
                                DuskspireActor.shake(500);
                                DuskspireHealth -= CurrentBattler.InShadowstep ? 50 : CurrentBattler.TransformedCounter > 0 ? 30 : 10;
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
                        if (CurrentBattler.Mana <= 0)
                        {
                            MovingActorForTurn = 3;
                            break;
                        }
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
                        if (PotionCount <= 0) { MovingActorForTurn = 3; break; }
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
#if DEBUG
            //Finished = true;
            DuskspireHealth = 1;
#endif
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
                    ModCoT.farmerData.GetOrCreateValue(Game1.player).transformed.Value = false;
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
            DuskspireActor.Sprite.animateOnce(time);
            foreach (var character in Battlers)
            {
                character.update(time, Game1.currentLocation);
                if (character is Farmer f)
                {
                    ModSnS.Instance.Helper.Reflection.GetMethod(f, "updateCommon").Invoke(time, Game1.currentLocation);
                }
            }

            if (CurrentTurn == DuskspireActor)
            {
                if (!DuskspireStartedTurn)
                {
                    DuskspireStartedTurn = true;
                    DuskspireActor.Sprite.StopAnimation();
                    DuskspireActor.Sprite.setCurrentAnimation([
                        new(40, 50),
                        new(41, 50),
                        new(42, 50),
                        new(43, 50),
                        new(44, 50),
                        new(45, 50),
                        new(46, 50),
                        new(47, 50) {frameStartBehavior = (f) =>
                        {
                            List<Character> choices = Battlers.Where(c => c != DuskspireActor && !BattlerData[c.Name].InShadowstep && BattlerData[c.Name].Health > 0).ToList();
                        for (int i = 0; i < choices.Count; ++i)
                        {
                            int ind = Game1.random.Next(choices.Count);
                            (choices[ind], choices[i]) = (choices[i], choices[ind]);
                        }

                        if (choices.Count > 0)
                        {
                            int ind = 0;
                            if (choices[ind] is Farmer who) who.jitterStrength = 10;
                            else if (choices[ind] is NPC n) n.shake(250);
                            BattlerData[choices[ind].Name].Health -= Math.Max(1, 40 - BattlerData[choices[ind].Name].Defense);

                            if (BattlerData[choices[ind].Name].Health <= 0)
                                choices[ind].startGlowing(Color.DarkRed, false, 0.01f);
                        }
                        if (choices.Count > 1)
                        {
                            int ind = 1;
                            if (choices[ind] is Farmer who) who.jitterStrength = 30;
                            else if (choices[ind] is NPC n) n.shake(250);
                            BattlerData[choices[ind].Name].Health -= Math.Max(1, 40 - BattlerData[choices[ind].Name].Defense);

                            if (BattlerData[choices[ind].Name].Health <= 0)
                                choices[ind].startGlowing(Color.DarkRed, false, 0.01f);
                        }
                        }},
                        new(48, 50),
                        new(49, 50) {frameEndBehavior = (f) =>
                        {
                        DuskspireActor.Sprite.StopAnimation();
                        Game1.player.stopJittering();
                        NextTurn();
                        }}
                    ]);
                }
            }

            if (MovingActorForTurn == 1)
            {
                CurrentTurn.isCharging = true;
                CurrentTurn.SetMovingRight(true);
                CurrentTurn.MovePosition(time, Game1.viewport, Game1.currentLocation);
                if ((CurrentTurn.Position - CurrentBattler.BasePosition).X >= 64)
                {
                    CurrentTurn.Halt();
                    CurrentTurn.isCharging = true;
                    MovingActorForTurn--;
                }
            }
            else if (MovingActorForTurn == -1)
            {
                CurrentTurn.isCharging = true;
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
                    var diff = proj.Target - proj.Position;
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

            if (CurrentTurn != DuskspireActor)
            {
                if (Game1.player.IsMainPlayer)
                {
                    if (DuskspireActor.Sprite.CurrentAnimation == null)
                    {
                        List<FarmerSprite.AnimationFrame> anim = [
                            new(28, 75),
                            new(28, 75),
                            new(30, 75),
                            new(31, 75),
                            new(32, 75),
                            new(33, 75),
                            new(34, 75),
                            new(35, 75),
                            new(36, 75),
                            new(35, 75),
                            new(34, 75),
                            new(33, 75),
                            new(32, 75),
                            new(31, 75),
                            new(30, 75),
                            new(29, 75) {frameEndBehavior = (f) => {DuskspireActor.Sprite.StopAnimation(); }}
                            ];

                        DuskspireActor.Sprite.setCurrentAnimation(anim);
                    }
                    if (DuskspireFrameOverride.HasValue)
                    {
                        DuskspireActor.Sprite.StopAnimation();
                        DuskspireActor.Sprite.setCurrentAnimation([new(DuskspireFrameOverride.Value, DuskspireFrameOverride != 25 ? 75 : 300) { frameEndBehavior = (f) => { DuskspireActor.Sprite.StopAnimation(); }}]);
                        DuskspireFrameOverride = null;
                    }
                    DuskspireActor.Sprite.UpdateSourceRect();
                }
            }

            Game1.currentLocation.Map.Update(Game1.currentGameTime.ElapsedGameTime.Milliseconds);
            Game1.game1.DrawWorld(Game1.currentGameTime, Game1.game1.ShouldDrawOnBuffer() ? Game1.game1.screen : null);

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            foreach (var proj in Projectiles)
            {
                proj.Rotation += proj.RotationSpeed;
                b.Draw(proj.Texture, Game1.GlobalToLocal(proj.Position), proj.SourceRect, Color.White, proj.Rotation, proj.SourceRect.Size.ToVector2() / 2, 4, SpriteEffects.None, 1);
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
                b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(CurrentTurn.StandingPixel.ToVector2() - new Vector2(12, 200 + 8 * MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 4))), rect, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.FlipVertically, 1);

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
                if (CurrentChoice != 1)
                {
                    IClickableMenu.drawTextureBox(b, x + 170, y + 200, 280, 64, Color.White);
                    SpriteText.drawString(b, $"<{DuskspireHealth}/1000", x + 194, y + 212);
                    SpriteText.drawStringWithScrollCenteredAt(b, "Duskspire", x + 450 / 2 + 35, y + 132, 280);
                }
                if (CurrentChoice == 1)
                {
                    IClickableMenu.drawTextureBox(b, x, y + 64, 450, 200, Color.White);

                    SpriteText.drawStringWithScrollCenteredAt(b, $"{CurrentBattler.AbilityName()} ={CurrentBattler.AbilityManaCost}", x + 450 / 2 - 100, y, 550);
                    Utility.drawTextWithShadow(b, Game1.parseText(CurrentBattler.AbilityDescription(), Game1.smallFont, 450 - 32 * 2), Game1.smallFont, new Vector2(x + 32, y + 96), Color.Black);

                    IClickableMenu.drawTextureBox(b, x + 170, y - 76, 280, 64, Color.White);
                    SpriteText.drawString(b, $"<{DuskspireHealth}/1000", x + 194, y - 64);
                    SpriteText.drawStringWithScrollCenteredAt(b, "Duskspire", x + 450 / 2 + 35, y - 144, 280);
                }
            }
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
            Character c = Battlers.First(c => c.Name == "Roslin");
            c.Sprite.SpriteWidth = 16;
            c.Sprite.SpriteHeight = 32;
            c.Sprite.LoadTexture(OrigRoslinTexture);
            c.Sprite.UpdateSourceRect();

            if (DuskspireHealth > 0)
            {
                Game1.player.health = -999;
                Game1.player.eventsSeen.Remove(Event.id);
                Game1.player.GetFarmerExtData().DoingFinale.Value = false;

                Event.endBehaviors();

                return;
            }

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

            Event.endBehaviors();
            DelayedAction.functionAfterDelay(() => Game1.PlayEvent(partnerInfo.IntermissionEventId, false, false), 500);
        }
    }
}