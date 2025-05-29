using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Minigames;
using SwordAndSorcerySMAPI.Deprecated;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI.Framework.Finale
{
    [HarmonyPatch(typeof(GameLocation), "drawFarmers")]
    public class GameLocationDrawFarmerInRPGFightPatch
    {
        public static void Postfix(SpriteBatch b)
        {
            if (Game1.currentMinigame is RPGMinigame)
                Game1.player.draw(b);
        }
    }

    public class RPGBattlerData
    {
        public string VictoryEvent { get; set; }
        public string IntermissionEvent { get; set; }
        public Battler BattlerData { get; set; }
    }

    public class Battler
    {
        public bool IsEnemy { get; set; } = false;
        public int Health { get; set; } = 100;
        public int BaseDefense { get; set; }
        public int MaxMana { get; set; }

        public List<BattlerAbility> Abilities { get; set; }
        //Currently Unimplented
        public Dictionary<string, List<int>> BattleAnimations { get; set; }
    }

    public class BattlerAbility
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "todo";
        public string Description { get; set; } = "not implemented";
        public string Type { get; set; } = "slash";
        public AnimationData Animation { get; set; } = null;
        public int ManaCost { get; set; } = 10;
        public int Cooldown { get; set; } = 0;

    }

    public class AnimationData
    {
        public List<int> Frames { get; set; }
        public int? FrameDuration { get; set; }
    }

    internal class RPGMinigame : IMinigame
    {
        private class BattlerData(Battler stats)
        {
            public Battler Stats { get; set; } = stats;
            public int Health { get; set; } = stats.Health;
            public int Mana { get; set; } = stats.MaxMana;
            public int Defense { get => Stats.BaseDefense * (Guarding ? 1 : 2) + (Guarding ? 10 : 0); }
            public List<BattlerAbility> Abilities { get; set; }

            public Vector2 BasePosition { get; set; }

            public bool Guarding { get; set; } = false;
        }

        private enum RPGMenuState
        {
            None,
            Main,
            Abilities,
            EnemySelection
        }
        private Dictionary<string, RPGBattlerData> _Data = null;
        public Dictionary<string, RPGBattlerData> Data
        {
            get
            {
                return _Data ??= Game1.content.Load<Dictionary<string, RPGBattlerData>>("FinalMix/FinalePartners");
            }
        }

        private readonly string Fight; //unused in 2.x, will be used in 3.0
        private readonly Event Event;
        private readonly Dictionary<Character, BattlerData> CharsData = [];
        private readonly List<Character> Chars = [];
        private int CurrCharNum = 0;
        private BattlerData CurrChar;
        private RPGMenuState State = RPGMenuState.None;
        private int MenuOption = 0;
        private int PrevMenuOption = 0;


        public RPGMinigame(Event @event, string whichFight)
        {
            Fight = whichFight;
            Event = @event;
            State = RPGMenuState.Main;

            Chars.Add(Game1.player);
            CharsData.Add(Game1.player, new(new()
            {
                Health = Game1.player.health,
                BaseDefense = Game1.player.buffs.Defense + (Game1.player.GetArmorItem()?.GetArmorAmount() ?? 0) / 25,
                MaxMana = Game1.player.GetFarmerExtData().maxMana.Value
            }));

            foreach (Character c in @event.actors)
            {
                if (Data.ContainsKey(c.Name) && !Data[c.Name].BattlerData.IsEnemy)
                {
                    Chars.Add(c);
                    CharsData.Add(c, new(Data[c.Name].BattlerData));
                }
            }

            foreach (Character c in @event.actors)
            {
                if (Data.ContainsKey(c.Name) && Data[c.Name].BattlerData.IsEnemy)
                {
                    Chars.Add(c);
                    CharsData.Add(c, new(Data[c.Name].BattlerData));
                }
            }

            CharsData[Game1.player].BasePosition = Game1.player.Position;
            CurrChar = CharsData[Game1.player];
        }

        public void changeScreenSize() { }

        public bool doMainGameUpdates()
        {
            return false;
        }

        public bool forceQuit()
        {
            unload();
            return true;
        }

        public void leftClickHeld(int x, int y)
        {
        }

        public string minigameId()
        {
            return $"DN.SnS.RPGBattle.{Fight}";
        }

        public bool overrideFreeMouseMovement()
        {
            return false;
        }

        public void receiveEventPoke(int data)
        {
        }

        public void receiveKeyPress(Keys k)
        {
            switch (State)
            {
                case RPGMenuState.Main:
                    if (k == Keys.Down || k == Keys.S || k == Utility.mapGamePadButtonToKey(Buttons.DPadDown) || k == Utility.mapGamePadButtonToKey(Buttons.LeftThumbstickDown) || k == Utility.mapGamePadButtonToKey(Buttons.RightThumbstickDown))
                    {
                        if (MenuOption == 3) return;
                        else MenuOption++;
                    }
                    else if (k == Keys.Up || k == Keys.W || k == Utility.mapGamePadButtonToKey(Buttons.DPadUp) || k == Utility.mapGamePadButtonToKey(Buttons.LeftThumbstickUp) || k == Utility.mapGamePadButtonToKey(Buttons.RightThumbstickUp))
                    {
                        if (MenuOption == 3) return;
                        else MenuOption++;
                    }
                    else if (k == Keys.Enter || k == Utility.mapGamePadButtonToKey(Buttons.A))
                    {
                        switch (MenuOption)
                        {
                            case 0:
                                //normal attack code
                                NextCharacter();
                                break;
                            case 1:
                                PrevMenuOption = MenuOption;
                                State = RPGMenuState.Abilities;
                                break;
                            case 2:
                                CurrChar.Guarding = true;
                                if (CurrChar.Abilities.Any(a => a.ID.EqualsIgnoreCase("Guard") || a.ID.EqualsIgnoreCase("Guarding")))
                                {
                                    Chars[CurrCharNum].Sprite.setCurrentAnimation(GetAnimation(CurrChar.Abilities.First(a => a.ID.EqualsIgnoreCase("Guard") || a.ID.EqualsIgnoreCase("Guarding")).Animation));
                                }
                                NextCharacter();
                                break;
                        }
                    }
                    break;
                case RPGMenuState.Abilities:
                    break;
                case RPGMenuState.EnemySelection:
                    break;
            }
        }

        private void NextCharacter()
        {
            CurrCharNum++;
            if (CurrCharNum == Chars.Count)
                CurrCharNum = 0;

            CurrChar = CharsData[Chars[CurrCharNum]];
        }

        private static List<FarmerSprite.AnimationFrame> GetAnimation(AnimationData data)
        {
            List<FarmerSprite.AnimationFrame> Animation = [];

            int duration = data.FrameDuration ?? 150;
            foreach (int frame in data.Frames)
            {
                Animation.Add(new FarmerSprite.AnimationFrame(frame, duration));
            }

            return Animation;
        }

        public void receiveKeyRelease(Keys k)
        {
        }

        public void receiveLeftClick(int x, int y, bool playSound = true)
        {
            switch (State)
            {
                case RPGMenuState.Main:
                    switch (MenuOption)
                    {
                        case 0:
                            //normal attack code
                            NextCharacter();
                            break;
                        case 1:
                            PrevMenuOption = MenuOption;
                            State = RPGMenuState.Abilities;
                            break;
                        case 2:
                            CurrChar.Guarding = true;
                            if (CurrChar.Abilities.Any(a => a.ID.EqualsIgnoreCase("Guard") || a.ID.EqualsIgnoreCase("Guarding")))
                            {
                                Chars[CurrCharNum].Sprite.setCurrentAnimation(GetAnimation(CurrChar.Abilities.First(a => a.ID.EqualsIgnoreCase("Guard") || a.ID.EqualsIgnoreCase("Guarding")).Animation));
                            }
                            NextCharacter();
                            break;
                    }
                    break;
                case RPGMenuState.Abilities:
                    break;
                case RPGMenuState.EnemySelection:
                    break;
            }
        }

        public void receiveRightClick(int x, int y, bool playSound = true)
        {
            receiveLeftClick(x, y, playSound);
        }

        public void releaseLeftClick(int x, int y)
        {
        }

        public void releaseRightClick(int x, int y)
        {
        }

        public bool tick(GameTime time)
        {
            throw new NotImplementedException();
        }

        public void unload()
        {
            Character c = Chars.First(c => c.Name == "Roslin");
            c.Sprite.SpriteWidth = 16;
            c.Sprite.SpriteHeight = 32;
            //c.Sprite.LoadTexture(OrigRoslinTexture);
            c.Sprite.UpdateSourceRect();

            if (Game1.random.Next(1) > 0)
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

        public void draw(SpriteBatch b)
        {
            throw new NotImplementedException();
        }
    }
}
