using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SwordAndSorcerySMAPI
{
    public class SongEntry
    {
        public Func<string> Name { get; set; }
        public Action Function { get; set; }

        internal static void Reset()
        {
            usedBuffToday = false;
            usedBattleToday = false;
            usedRestorationToday = false;
            usedCropsToday = false;
        }

        private static bool usedBuffToday = false;
        internal static void BuffSong()
        {
            if (usedBuffToday)
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedBuffToday = true;

            int strMult = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffStrength) ? 2 : 1;
            int duration = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffDuration) ? Buff.ENDLESS : (7 * 60 * 1000);

            BuffEffects effects = new();
            switch (Utility.CreateRandom((double)Game1.startingGameSeed * 3, Game1.player.UniqueMultiplayerID * 5, Game1.stats.DaysPlayed * 7).Next(4))
            {
                case 0: effects.LuckLevel.Value = 1 * strMult; break;
                case 1: effects.FarmingLevel.Value = 1 * strMult; break;
                case 2: effects.Speed.Value = 1 * strMult; break;
                case 3: effects.FishingLevel.Value = 1 * strMult; break;
            }

            Game1.player.applyBuff(new Buff("bardics.buff", "bardics.buff", I18n.Bardics_Song_Buff_Name(), duration, effects: effects));
        }

        private static bool usedBattleToday = false;
        internal static void BattleSong()
        {
            if (usedBattleToday)
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedBattleToday = true;

            int strMult = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffStrength) ? 2 : 1;
            int duration = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffDuration) ? Buff.ENDLESS : (7 * 60 * 1000);

            BuffEffects effects = new();
            switch (Utility.CreateRandom((double)Game1.startingGameSeed * 3, Game1.player.UniqueMultiplayerID * 5, Game1.stats.DaysPlayed * 7).Next(2))
            {
                case 0: effects.AttackMultiplier.Value = 1 + 0.1f * strMult; break;
                case 1: effects.Defense.Value = 3 * strMult; break;
            }

            Game1.player.applyBuff(new Buff("bardics.battle", "bardics.battle", I18n.Bardics_Song_Battle_Name(), duration, effects: effects));
        }

        private static bool usedRestorationToday = false;
        internal static void RestorationSong()
        {
            if (usedRestorationToday)
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedRestorationToday = true;

            Game1.playSound("healSound");
            Game1.player.health = Math.Min(Game1.player.health + (int)(Game1.player.maxHealth * 0.25), Game1.player.maxHealth);
            Game1.player.stamina += (int)(Game1.player.MaxStamina * 0.25f);
        }

        private static int protectionTimer = -1;
        internal static void ProtectionSong()
        {
            if (protectionTimer < 0)
            {
                ModUP.Instance.Helper.Events.GameLoop.UpdateTicked += ProtectionFunctionality;
            }
            protectionTimer = 30 * 1000;
        }
        internal static void ProtectionFunctionality(object sender, UpdateTickedEventArgs args)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            int oldTimer = protectionTimer;
            protectionTimer -= (int) Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            if ( protectionTimer < 0 )
            {
                ModUP.Instance.Helper.Events.GameLoop.UpdateTicked -= ProtectionFunctionality;
            }

            if ( oldTimer % 1000 < protectionTimer % 1000 )
            {
                foreach ( Monster monster in Game1.player.currentLocation.characters.ToList().Where( npc => npc is Monster m ).Cast<Monster>() )
                {
                    if ( Vector2.Distance( Game1.player.StandingPixel.ToVector2(), monster.StandingPixel.ToVector2() ) < Game1.tileSize * 6 )
                    {
                        var traj = (monster.StandingPixel.ToVector2() - Game1.player.StandingPixel.ToVector2());
                        traj.Normalize();
                        monster.takeDamage(0, (int)(traj.X * 50), (int)(traj.Y * -50), false, 0, Game1.player);
                        if ( Game1.IsClient )
                        {
                            ModUP.Instance.Helper.Multiplayer.SendMessage(new MonsterKnockbackMessage()
                            {
                                MonsterId = monster.id,
                                Trajectory = (traj * 50).ToPoint(),
                            }, ModUP.MultiplayerMessage_MonsterKnockback, new string[] { ModUP.Instance.ModManifest.UniqueID } );
                        }
                    }
                }
            }
        }

        private static bool usedTimeToday = false;
        private static int timeTimer = -1;
        internal static void TimeSong()
        {
            if (usedTimeToday)
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedTimeToday = true;

            if (timeTimer < 0)
            {
                ModUP.Instance.Helper.Events.GameLoop.UpdateTicked += TimeFunctionality;
            }
            timeTimer = 2 * 60 * 1000;
        }
        internal static void TimeFunctionality(object sender, UpdateTickedEventArgs args)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            int oldTimer = timeTimer;
            timeTimer -= (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            if (timeTimer < 0)
            {
                ModUP.Instance.Helper.Events.GameLoop.UpdateTicked -= ProtectionFunctionality;
            }

            Game1.gameTimeInterval = 0;
        }

        internal static void HorseSong()
        {
            if ( Game1.IsClient )
            {
                ModUP.Instance.Helper.Multiplayer.SendMessage("", ModUP.MultiplayerMessage_HorseWarp, new string[] { ModUP.Instance.ModManifest.UniqueID }, new long[] { Game1.MasterPlayer.UniqueMultiplayerID });
            }
            else
            {
                Horse horse = null;
                Utility.ForEachBuilding<Stable>((s) =>
                {
                    Horse shorse = s.getStableHorse();
                    if (shorse != null && shorse.getOwner() == Game1.player)
                    {
                        horse = shorse;
                        return false;
                    }
                    return true;
                });

                Game1.warpFarmer(horse.currentLocation.NameOrUniqueName, horse.TilePoint.X, horse.TilePoint.Y, false);
            }
        }

        private static bool usedCropsToday = false;
        internal static void CropSong()
        {
            if (usedCropsToday)
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }

            int grewCount = 0;

            for ( int ix = -3; ix <= 3; ++ix )
            {
                for (int iy = -3; iy <= 3; ++iy)
                {
                    int x = Game1.player.TilePoint.X + ix;
                    int y = Game1.player.TilePoint.Y + iy;

                    if ( Game1.player.currentLocation.terrainFeatures.TryGetValue( new Vector2( x, y ), out TerrainFeature tf ) )
                    {
                        if ( tf is HoeDirt hd )
                        {
                            if ( hd.crop != null && !hd.crop.modData.ContainsKey( $"{ModUP.Instance.ModManifest.UniqueID}_BardicsCropSongBuff" ) )
                            {
                                hd.crop.modData.Add($"{ModUP.Instance.ModManifest.UniqueID}_BardicsCropSongBuff", "meow");

                                int totalDaysOfCropGrowth = 0;
                                for (int j = 0; j < hd.crop.phaseDays.Count - 1; j++)
                                {
                                    totalDaysOfCropGrowth += hd.crop.phaseDays[j];
                                }
                                float speedIncrease = 0.25f;
                                int daysToRemove = (int)Math.Ceiling((float)totalDaysOfCropGrowth * speedIncrease);
                                int tries = 0;
                                while (daysToRemove > 0 && tries < 3)
                                {
                                    for (int i = 0; i < hd.crop.phaseDays.Count; i++)
                                    {
                                        if ((i > 0 || hd.crop.phaseDays[i] > 1) && hd.crop.phaseDays[i] != 99999)
                                        {
                                            hd.crop.phaseDays[i]--;
                                            daysToRemove--;
                                        }
                                        if (daysToRemove <= 0)
                                        {
                                            break;
                                        }
                                    }
                                    tries++;
                                }

                                if (hd.crop.dayOfCurrentPhase.Value >= ((hd.crop.phaseDays.Count > 0) ? hd.crop.phaseDays[Math.Min(hd.crop.phaseDays.Count - 1, hd.crop.currentPhase)] : 0) && hd.crop.currentPhase.Value < hd.crop.phaseDays.Count - 1)
                                {
                                    hd.crop.currentPhase.Value++;
                                    hd.crop.dayOfCurrentPhase.Value = 0;
                                }
                            }
                        }
                    }
                }
            }

            if ( grewCount > 0 )
            {
                usedCropsToday = true;
                Game1.addHUDMessage(new HUDMessage(I18n.Bardics_Song_Crops_Message(grewCount)));
            }
        }

        internal static void ObeliskSong()
        {
            List<string> opts = new();
            if (Game1.IsBuildingConstructed("Desert Obelisk"))
                opts.Add("Desert Obelisk");
            if (Game1.IsBuildingConstructed("Water Obelisk"))
                opts.Add("Water Obelisk");
            if (Game1.IsBuildingConstructed("Earth Obelisk"))
                opts.Add("Earth Obelisk");
            if (Game1.IsBuildingConstructed("Island Obelisk"))
                opts.Add("Island Obelisk");
            if (Game1.player.mailReceived.Contains("ReturnScepter"))
                opts.Add("Return Scepter");

            List<Response> responses = new();
            foreach ( var entry in opts )
            {
                responses.Add(new(entry, I18n.GetByKey($"bardics.song.obelisk.{entry.Replace(" ", "")}")));
            }
            responses.Add(new("cancel", I18n.Cancel()));
            Game1.drawObjectQuestionDialogue(I18n.Bardics_Song_Obelisk_Name(), responses.ToArray());
            Game1.currentLocation.afterQuestion = (Farmer who, string key) =>
            {
                if ( opts.Contains( key ) )
                {
                    if ( key == "Return Scepter" )
                    {
                        FarmHouse home = Utility.getHomeOfFarmer(Game1.player);
                        if (home != null)
                        {
                            Point position = home.getFrontDoorSpot();
                            Game1.warpFarmer("Farm", position.X, position.Y, flip: false);
                        }
                    }
                    else
                    {
                        Building.TryPerformObeliskWarp(key, who);
                    }
                }
            };
        }
    }

    public class MonsterKnockbackMessage
    {
        public int MonsterId { get; set; }
        public Point Trajectory { get; set; }
    }
    public class HorseWarpMessage
    {
        public string Location { get; set; }
        public Point Tile { get; set; }
    }

    public class ModUP
    {
        public static ModUP Instance;

        internal const string MultiplayerMessage_MonsterKnockback = "MonsterKnockback";
        internal const string MultiplayerMessage_HorseWarp = "HorseWarp";

        public const string BardicsEventId = "SnS.Ch3.Cirrus.14";

        public static List<List<SongEntry>> Songs { get; private set; }

        internal static BardicsSkill Skill;

        public IMonitor Monitor;
        public IManifest ModManifest;
        public IModHelper Helper;
        public ModUP(IMonitor monitor, IManifest manifest, IModHelper helper)
        {
            Instance = this;
            Monitor = monitor;
            ModManifest = manifest;
            Helper = helper;
        }

        public void Entry()
        {
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;

            Skill = new BardicsSkill();
            Songs = new()
            {
                new() { new SongEntry() { Name = I18n.Bardics_Song_Buff_Name, Function = SongEntry.BuffSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Battle_Name, Function = SongEntry.BattleSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Restoration_Name, Function = SongEntry.RestorationSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Protection_Name, Function = SongEntry.ProtectionSong } },
                new() {}, // level 5
                new() { new SongEntry() { Name = I18n.Bardics_Song_Time_Name, Function = SongEntry.TimeSong } }, 
                new() { new SongEntry() { Name = () => I18n.Bardics_Song_Horse_Name( Game1.player.horseName.Value ), Function = SongEntry.HorseSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Crops_Name, Function = SongEntry.CropSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Obelisk_Name, Function = SongEntry.ObeliskSong } },
                new() {},
            };

            Helper.ConsoleCommands.Add("sns_playsong", "...", (cmd, args) =>
            {
                var songlist = Songs[int.Parse(args[0])];
                SongEntry toPlay = null;
                if (songlist.Count > 1)
                    toPlay = songlist[int.Parse(args[1])];
                else if (songlist.Count == 1)
                    toPlay = songlist[0];
                else return;

                toPlay.Function();
            });
        }

        private void Multiplayer_ModMessageReceived(object sender, StardewModdingAPI.Events.ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            switch ( e.Type )
            {
                case MultiplayerMessage_MonsterKnockback:
                    {
                        var msg = e.ReadAs<MonsterKnockbackMessage>();
                        var player = Game1.getFarmer(e.FromPlayerID);
                        (player.currentLocation.characters.FirstOrDefault(npc => npc.id == msg.MonsterId) as Monster)?.takeDamage(0, msg.Trajectory.X, -msg.Trajectory.Y, false, 0, player);
                    }
                    break;
                case MultiplayerMessage_HorseWarp:
                    {
                        if ( Game1.IsClient )
                        {
                            var msg = e.ReadAs<HorseWarpMessage>();
                            Game1.warpFarmer(msg.Location, msg.Tile.X, msg.Tile.Y, false);
                        }
                        else
                        {
                            var player = Game1.getFarmer(e.FromPlayerID);

                            Horse horse = null;
                            Utility.ForEachBuilding<Stable>((s) =>
                            {
                                Horse shorse = s.getStableHorse();
                                if (shorse != null && shorse.getOwner() == player)
                                {
                                    horse = shorse;
                                    return false;
                                }
                                return true;
                            });

                            if ( horse != null )
                            {
                                Helper.Multiplayer.SendMessage( new HorseWarpMessage()
                                {
                                    Location = horse.currentLocation.NameOrUniqueName,
                                    Tile = horse.TilePoint,
                                }, MultiplayerMessage_HorseWarp, [ ModManifest.UniqueID ], [ e.FromPlayerID ] );
                            }
                        }
                    }
                    break;
            }
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            SongEntry.Reset();
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            Skills.RegisterSkill(Skill);
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.grantConversationFriendship))]
    public static class NpcFriendshipBardicsExpPatch
    {
        public static void Postfix( NPC __instance, int amount )
        {
            if (!Game1.player.eventsSeen.Contains(ModUP.BardicsEventId))
                return;

            Game1.player.AddCustomSkillExperience(ModUP.Skill, amount / 10);
        }
    }
}