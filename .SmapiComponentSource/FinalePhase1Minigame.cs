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

        public Vector2 BasePosition { get; set; }
    }

    internal class FinalePhase1Minigame : IMinigame
    {

        public bool Finished { get; set; } = false;
        public Event Event { get; }
        public EventContext Context { get; }

        public Texture2D BossSprite { get; set; }

        public Character CurrentTurn { get; set; }
        public BattlerInfo CurrentBattler => BattlerData.TryGetValue(CurrentTurn.Name, out var info) ? info : DuskspireDummyInfo;

        public List<Character> Battlers { get; set; } = new();
        public Dictionary<string, BattlerInfo> BattlerData { get; set; } = new();

        private NPC DuskspireActor { get; set; }
        private BattlerInfo DuskspireDummyInfo { get; set; } = new();
        private int DuskspireHealth { get; set; } = 500;

        private int CurrentChoice { get; set; } = 0;
        private float CurrentForward { get; set; }
        private int MovingActorForTurn { get; set; } = -2; // -1 = backward, 0 = doing action, 1 = forward, anything else = not doing anything
        private Action PendingAction { get; set; }
        private int PotionCount { get; set; } = 3;

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
                        AbilityFunc = (_) => { CurrentBattler.InShadowstep = true; --MovingActorForTurn; },
                        AbilityName = I18n.FinaleMinigame_Ability_Shadowstep_Name,
                        AbilityDescription = I18n.FinaleMinigame_Ability_Shadowstep_Description,
                        AbilityManaCost = 10,
                    }
                },
                { Game1.player.Name, new BattlerInfo() { BaseDefense = Game1.player.buffs.Defense + (Game1.player.GetArmorItem()?.GetArmorAmount() ?? 0) / 25, Mana = Game1.player.GetFarmerExtData().mana.Value, MaxMana = Game1.player.GetFarmerExtData().maxMana.Value } },
                { "Dandelion", new BattlerInfo() { BaseDefense = 12, Mana = 40, MaxMana = 40 } },
                { "Hector", new BattlerInfo() { BaseDefense = 3, Mana = 70, MaxMana = 70 } },
                { "Cirrus", new BattlerInfo() { BaseDefense = 0, Mana = 50, MaxMana = 50 } },
                { "Roslin", new BattlerInfo() { BaseDefense = 0, Mana = 100, MaxMana = 100 } },
                { "MadDog.HashtagBearFam.Gunnar", new BattlerInfo() { BaseDefense = 5, Mana = 25, MaxMana = 25 } }
            };

            foreach (var actor in @event.actors.ToList())
            {
                if (actor.Name == "Duskspire")
                {
                    DuskspireActor = actor;
                    continue;
                }

                if (BattlerData.TryGetValue(actor.Name, out var data))
                {
                    data.BasePosition = actor.Position;
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
                                DuskspireHealth -= CurrentBattler.InShadowstep ? 50 : 10;
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
        }

        public bool tick(GameTime time)
        {
            foreach (var character in Battlers)
            {
                character.update(time, Game1.currentLocation);
            }

            if (CurrentTurn == DuskspireActor)
            {
                NextTurn();
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

                    NextTurn();
                }
            }
            else if (MovingActorForTurn == 0)
            {
                PendingAction();
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

            Game1.currentLocation.Map.Update(Game1.currentGameTime.ElapsedGameTime.Milliseconds);
            Game1.game1.DrawWorld(Game1.currentGameTime, Game1.game1.ShouldDrawOnBuffer() ? Game1.game1.screen : null);

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            /*
            Vector2 bossPos = new Vector2(18, 13);
            Rectangle bossSrc = new Rectangle(0, 96, 32, 32);
            bossPos *= Game1.tileSize;
            b.Draw(BossSprite, Game1.GlobalToLocal(bossPos - new Vector2((bossSrc.Width - 16) / 2, bossSrc.Height - 16) * Game1.pixelZoom), bossSrc, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, bossPos.Y / 10000f);
            */

            if (CurrentTurn != null)
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

                    SpriteText.drawStringWithScrollCenteredAt(b, $"{CurrentBattler.AbilityName()} ={CurrentBattler.AbilityManaCost}", x + 450 / 2, y, 350);
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