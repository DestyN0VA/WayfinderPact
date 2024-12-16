using CircleOfThornsSMAPI;
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
using StardewValley.Enchantments;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
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
            usedBuffToday = 0;
            usedBattleToday = 0;
            usedRestorationToday = 0;
            usedTimeToday = 0;
            usedCropsToday = 0;
            usedNpcSong = 0;
        }

        internal static int usedBuffToday = 0;
        internal static void BuffSong()
        {
            if (usedBuffToday >= ModUP.GetSongLimit())
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedBuffToday++;

            int strMult = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffStrength) ? 2 : 1;
            int duration = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffDuration) ? Buff.ENDLESS : (7 * 6 * 6 * 1000);

            BuffEffects effects = new();
            switch (Utility.CreateRandom((double)Game1.uniqueIDForThisGame * 3, Game1.player.UniqueMultiplayerID * 5, Game1.stats.DaysPlayed * 7).Next(4))
            {
                case 0: effects.LuckLevel.Value = 1 * strMult; break;
                case 1: effects.FarmingLevel.Value = 1 * strMult; break;
                case 2: effects.Speed.Value = 1 * strMult; break;
                case 3: effects.FishingLevel.Value = 1 * strMult; break;
            }

            Game1.player.applyBuff(new Buff("bardics.buff", "bardics.buff", I18n.Bardics_Song_Buff_Name(), duration, effects: effects, iconTexture: ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/abilities.png"), iconSheetIndex: 2, displayName: I18n.Bardics_Song_Buff_Name(), description: ""));
        }

        internal static int usedBattleToday = 0;
        internal static void BattleSong()
        {
            if (usedBattleToday >= ModUP.GetSongLimit())
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedBattleToday++;

            int strMult = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffStrength) ? 2 : 1;
            int duration = Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffDuration) ? Buff.ENDLESS : (7 * 6 * 6 * 1000);

            BuffEffects effects = new();
            switch (Utility.CreateRandom((double)Game1.uniqueIDForThisGame * 3, Game1.player.UniqueMultiplayerID * 5, Game1.stats.DaysPlayed * 7).Next(2))
            {
                case 0: effects.AttackMultiplier.Value = 1 + 0.1f * strMult; break;
                case 1: effects.Defense.Value = 3 * strMult; break;
            }

            Game1.player.applyBuff(new Buff("bardics.battle", "bardics.battle", I18n.Bardics_Song_Battle_Name(), duration, effects: effects, iconTexture: ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/abilities.png"), iconSheetIndex: 3, displayName: I18n.Bardics_Song_Battle_Name()));
        }

        internal static int usedRestorationToday = 0;
        internal static void RestorationSong()
        {
            if (usedRestorationToday >= ModUP.GetSongLimit())
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedRestorationToday++;

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
            protectionTimer -= (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            if (protectionTimer < 0)
            {
                ModUP.Instance.Helper.Events.GameLoop.UpdateTicked -= ProtectionFunctionality;
            }

            if (oldTimer % 1000 < protectionTimer % 1000)
            {
                foreach (Monster monster in Game1.player.currentLocation.characters.ToList().Where(npc => npc is Monster m).Cast<Monster>())
                {
                    if (Vector2.Distance(Game1.player.StandingPixel.ToVector2(), monster.StandingPixel.ToVector2()) < Game1.tileSize * 6)
                    {
                        var traj = (monster.StandingPixel.ToVector2() - Game1.player.StandingPixel.ToVector2());
                        traj.Normalize();
                        monster.takeDamage(0, (int)(traj.X * 50), (int)(traj.Y * -50), false, 0, Game1.player);
                        if (Game1.IsClient)
                        {
                            ModUP.Instance.Helper.Multiplayer.SendMessage(new MonsterKnockbackMessage()
                            {
                                MonsterId = monster.id,
                                Trajectory = (traj * 50).ToPoint(),
                            }, ModUP.MultiplayerMessage_MonsterKnockback, new string[] { ModUP.Instance.ModManifest.UniqueID });
                        }
                    }
                }
            }
        }

        internal static int usedTimeToday = 0;
        private static int timeTimer = -1;
        internal static void TimeSong()
        {
            if (usedTimeToday >= ModUP.GetSongLimit())
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            usedTimeToday++;

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
                ModUP.Instance.Helper.Events.GameLoop.UpdateTicked -= TimeFunctionality;
            }

            Game1.gameTimeInterval = 0;
        }

        internal static void HorseSong()
        {
            if (Game1.IsClient)
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

        internal static int usedCropsToday = 0;
        internal static void CropSong()
        {
            if (usedCropsToday >= ModUP.GetSongLimit())
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }

            int grewCount = 0;

            for (int ix = -3; ix <= 3; ++ix)
            {
                for (int iy = -3; iy <= 3; ++iy)
                {
                    int x = Game1.player.TilePoint.X + ix;
                    int y = Game1.player.TilePoint.Y + iy;

                    if (Game1.player.currentLocation.terrainFeatures.TryGetValue(new Vector2(x, y), out TerrainFeature tf))
                    {
                        if (tf is HoeDirt hd)
                        {
                            if (hd.crop != null && !hd.crop.modData.ContainsKey($"{ModUP.Instance.ModManifest.UniqueID}_BardicsCropSongBuff"))
                            {
                                hd.crop.modData.Add($"{ModUP.Instance.ModManifest.UniqueID}_BardicsCropSongBuff", "hoot");

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

                                if (hd.crop.dayOfCurrentPhase.Value >= ((hd.crop.phaseDays.Count > 0) ? hd.crop.phaseDays[Math.Min(hd.crop.phaseDays.Count - 1, hd.crop.currentPhase.Value)] : 0) && hd.crop.currentPhase.Value < hd.crop.phaseDays.Count - 1)
                                {
                                    hd.crop.currentPhase.Value++;
                                    hd.crop.dayOfCurrentPhase.Value = 0;
                                }
                            }
                        }
                    }
                }
            }

            if (grewCount > 0)
            {
                usedCropsToday++;
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
            foreach (var entry in opts)
            {
                responses.Add(new(entry, I18n.GetByKey($"bardics.song.obelisk.{entry.Replace(" ", "")}")));
            }
            responses.Add(new("cancel", I18n.Cancel()));
            Game1.drawObjectQuestionDialogue(I18n.Bardics_Song_Obelisk_Name(), responses.ToArray());
            Game1.currentLocation.afterQuestion = (Farmer who, string key) =>
            {
                if (opts.Contains(key))
                {
                    if (key == "Return Scepter")
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

        internal static void AttackSong()
        {
            int damage = (int)(Game1.player.Items.Where(i => i is MeleeWeapon).Max(i => (i as MeleeWeapon).getItemLevel()) * 2.5f);
            if (Game1.player.HasCustomProfession(BardicsSkill.ProfessionAttackDamage))
                damage *= 2;
            damage = (int)(damage * ModSnS.AetherDamageMultiplier());

            float dist = Game1.tileSize * 5;
            if (Game1.player.HasCustomProfession(BardicsSkill.ProfessionAttackRange))
                dist = float.MaxValue;

            foreach (var monster_ in Game1.player.currentLocation.characters.Where(c => c is Monster))
            {
                var monster = monster_ as Monster;
                if (Vector2.Distance(monster.StandingPixel.ToVector2(), Game1.player.StandingPixel.ToVector2()) <= dist)
                {
                    Vector2 traj = (monster.StandingPixel.ToVector2() - Game1.player.StandingPixel.ToVector2());
                    traj.Normalize();
                    traj *= 5;
                    int dmg = monster.takeDamage(damage, (int)traj.X, (int)traj.Y, false, 0, Game1.player);

                    Rectangle monsterBox = monster.GetBoundingBox();
                    if (dmg == -1)
                    {
                        string missText = Game1.content.LoadString("Strings\\StringsFromCSFiles:Attack_Miss");
                        Game1.player.currentLocation.debris.Add(new Debris(missText, 1, new Vector2(monsterBox.Center.X, monsterBox.Center.Y), Color.LightGray, 1f, 0f));
                    }
                    else
                    {
                        Game1.player.currentLocation.removeDamageDebris(monster);
                        Game1.player.currentLocation.debris.Add(new Debris(dmg, new Vector2(monsterBox.Center.X + 16, monsterBox.Center.Y), false ? Color.Yellow : new Color(255, 130, 0), false ? (1f + (float)dmg / 300f) : 1f, monster));
                    }
                }
            }
        }

        internal static int usedNpcSong = 0;
        internal static void NpcSong()
        {
            if (usedNpcSong >= ModUP.GetSongLimit())
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }

            int duration = 7000 * 6 * 6;
            if (Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffDuration))
                duration = Buff.ENDLESS;

            // TODO: Convert to data asset so mods can add their own NPCs
            Dictionary<string, Func<BuffEffects>> buffs = new()
            {
                { "Elliott", () => new BuffEffects() { FishingLevel = { 1 } } },
                { "Harvey", () => new BuffEffects() { MaxStamina = { 30 } } },
                { "Sam", () => new BuffEffects() { CriticalChanceMultiplier = { 1.1f } } },
                { "Sebastian", () => new BuffEffects() { Defense = { 2 } } },
                { "Shane", () => new BuffEffects() { FarmingLevel = { 1 } } },
                { "Abigail", () => new BuffEffects() { AttackMultiplier = { 1.1f } } },
                { "Emily", () => new BuffEffects() { LuckLevel = { 1 } } },
                { "Haley", () => new BuffEffects() { Speed = { 1 } } },
                { "Leah", () => new BuffEffects() { ForagingLevel = { 1 } } },
                { "Maru", () => new BuffEffects() { MagneticRadius = { 48 } } },
                //exp gain//{ "Penny", () => new BuffEffects() { FishingLevel = { 1 } } },
                { "Caroline", () => new BuffEffects() { FarmingLevel = { 1 } } },
                { "Clint", () => new BuffEffects() { MiningLevel = { 1 } } },
                { "Demetrius", () => new BuffEffects() { MagneticRadius = { 48 } } },
                { "Dwarf", () => new BuffEffects() { MiningLevel = { 1 } } },
                { "Evelyn", () => new BuffEffects() { FarmingLevel = { 1 } } },
                { "George", () => new BuffEffects() { MiningLevel = { 1 } } },
                { "Gus", () => new BuffEffects() { ForagingLevel = { 1 } } },
                { "Jas", () => new BuffEffects() { Speed = { 1 } } },
                { "Jodi", () => new BuffEffects() { Defense = { 2 } } },
                { "Kent", () => new BuffEffects() { AttackMultiplier = { 1.1f } } },
                //squid ink ravioli//{ "Krobus", () => new BuffEffects() { FishingLevel = { 1 } } },
                { "Leo", () => new BuffEffects() { ForagingLevel = { 1 } } },
                { "Lewis", () => new BuffEffects() { Speed = { 1 } } },
                { "Linus", () => new BuffEffects() { ForagingLevel = { 1 } } },
                { "Marnie", () => new BuffEffects() { FarmingLevel = { 1 } } },
                { "Pam", () => new BuffEffects() { Speed = { 1 } } },
                { "Pierre", () => new BuffEffects() { FarmingLevel = { 1 } } },
                { "Robin", () => new BuffEffects() { ForagingLevel = { 1 } } },
                //oil of garlic//{ "Sandy", () => new BuffEffects() { FishingLevel = { 1 } } },
                { "Vincent", () => new BuffEffects() { Speed = { 1 } } },
                { "Willy", () => new BuffEffects() { FishingLevel = { 1 } } },
                //arcana//{ "Wizard", () => new BuffEffects() { FishingLevel = { 1 } } },
                //monstermusk//{ "MarlonFey", () => new BuffEffects() { FishingLevel = { 1 } } },
                { "GuntherSilvian", () => new BuffEffects() { MiningLevel = { 1 } } },
                //exp gain//{ "Cirrus", () => new BuffEffects() { ForagingLevel = { 1 } } },
                { "Mateo", () => new BuffEffects() { AttackMultiplier = { 1.1f } } },
                //druidics//{ "Hector", () => new BuffEffects() { FarmingLevel = { 1 } } },
            };

            NPC target = null;
            float distSoFar = float.MaxValue;
            foreach (var npc in Game1.currentLocation.characters.Where(c => c.IsVillager))
            {
                float dist = Vector2.Distance(npc.StandingPixel.ToVector2(), Game1.player.StandingPixel.ToVector2());
                if (dist < distSoFar)
                {
                    target = npc;
                    distSoFar = dist;
                }
            }

            if (!buffs.ContainsKey(target?.Name ?? ""))
            {
                Game1.drawObjectDialogue(I18n.Harp_BadSong());
                return;
            }
            BuffEffects effects = buffs[target.Name]();
            if (Game1.player.HasCustomProfession(BardicsSkill.ProfessionBuffStrength))
            {
                effects.CombatLevel.Value *= 2;
                effects.FarmingLevel.Value *= 2;
                effects.FishingLevel.Value *= 2;
                effects.MiningLevel.Value *= 2;
                effects.LuckLevel.Value *= 2;
                effects.ForagingLevel.Value *= 2;
                effects.MaxStamina.Value *= 2;
                effects.MagneticRadius.Value *= 2;
                effects.Speed.Value *= 2;
                effects.Defense.Value *= 2;
                effects.Attack.Value *= 2;
                effects.Immunity.Value *= 2;
                effects.AttackMultiplier.Value = 1 + (effects.AttackMultiplier.Value - 1) * 2;
                effects.KnockbackMultiplier.Value = 1 + (effects.KnockbackMultiplier.Value - 1) * 2;
                effects.WeaponSpeedMultiplier.Value = 1 + (effects.WeaponSpeedMultiplier.Value - 1) * 2;
                effects.CriticalChanceMultiplier.Value = 1 + (effects.CriticalChanceMultiplier.Value - 1) * 2;
                effects.CriticalPowerMultiplier.Value = 1 + (effects.CriticalPowerMultiplier.Value - 1) * 2;
                effects.WeaponPrecisionMultiplier.Value = 1 + (effects.WeaponPrecisionMultiplier.Value - 1) * 2;
            }

            // TODO: Icon
            Game1.player.applyBuff(new Buff("npcsong", "npcsong", I18n.Bardics_Song_Npcbuff_Buff(target.Name), duration, effects: effects));
            usedNpcSong++;
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

        internal static int GetSongLimit()
        {
            int amt = 1;
            if (Game1.player.CurrentTool?.QualifiedItemId == "(W)DN.SnS_BardShield" || Game1.player.GetOffhand()?.QualifiedItemId == "(W)DN.SnS_BardShield")
            {
                ++amt;
            }
            return amt;
        }

        private static void SongPreamble(Action songStuff)
        {
            Game1.player.faceDirection(Game1.down);
            Game1.player.performPlayerEmote("music");
            Game1.player.isEmoteAnimating = false;
            Game1.player.noMovementPause = 150 * 9;
            Game1.player.FarmerSprite.endOfAnimationFunction += (f) => { songStuff(); Game1.player.CanMove = true; };
        }

        public void Entry()
        {
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
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
                new() { new SongEntry() { Name = () => I18n.Bardics_Song_Horse_Name( Game1.player.horseName.Value ?? I18n.Cptoken_Horse() ), Function = SongEntry.HorseSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Crops_Name, Function = SongEntry.CropSong } },
                new() { new SongEntry() { Name = I18n.Bardics_Song_Obelisk_Name, Function = SongEntry.ObeliskSong } },
                new() {},
            };

            GameStateQuery.Register("PLAYER_IS_COLLEGE_ELOQUENCE", (args, ctx) =>
            {
                return GameStateQuery.Helpers.WithPlayer(ctx.Player, args[1], (f) => f.HasCustomProfession(BardicsSkill.ProfessionBuff));
            });
            GameStateQuery.Register("PLAYER_IS_COLLEGE_VALOR", (args, ctx) =>
            {
                return GameStateQuery.Helpers.WithPlayer(ctx.Player, args[1], (f) => f.HasCustomProfession(BardicsSkill.ProfessionAttack));
            });

            Ability.Abilities.Add("song_buff", new Ability("song_buff")
            {
                Name = I18n.Bardics_Song_Buff_Name,
                Description = I18n.Bardics_Song_Buff_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 2,
                ManaCost = () => 20,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 1",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(1),
                CanUse = () => SongEntry.usedBuffToday < ModUP.GetSongLimit(),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 50);
                    SongPreamble(() => SongEntry.BuffSong());
                }
            });
            Ability.Abilities.Add("song_battle", new Ability("song_battle")
            {
                Name = I18n.Bardics_Song_Battle_Name,
                Description = I18n.Bardics_Song_Battle_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 3,
                ManaCost = () => 20,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 2",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(2),
                CanUse = () => SongEntry.usedBattleToday < ModUP.GetSongLimit(),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 50);
                    SongPreamble(() => SongEntry.BattleSong());
                }
            });
            Ability.Abilities.Add("song_restoration", new Ability("song_restoration")
            {
                Name = I18n.Bardics_Song_Restoration_Name,
                Description = I18n.Bardics_Song_Restoration_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 4,
                ManaCost = () => 10,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 3",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(3),
                CanUse = () => SongEntry.usedRestorationToday < ModUP.GetSongLimit(),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 50);
                    SongPreamble(() => SongEntry.RestorationSong());
                }
            });
            Ability.Abilities.Add("song_protection", new Ability("song_protection")
            {
                Name = I18n.Bardics_Song_Protection_Name,
                Description = I18n.Bardics_Song_Protection_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 5,
                ManaCost = () => 10,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 4",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(4),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 25);
                    SongPreamble(() => SongEntry.ProtectionSong());
                }
            });
            Ability.Abilities.Add("song_npcbuff", new Ability("song_npcbuff")
            {
                Name = I18n.Bardics_Song_Npcbuff_Name,
                Description = I18n.Bardics_Song_Npcbuff_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 6,
                ManaCost = () => 25,
                CanUse = () => SongEntry.usedNpcSong < ModUP.GetSongLimit(),
                KnownCondition = $"PLAYER_IS_COLLEGE_ELOQUENCE Current",
                HiddenIfLocked = true,
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 50);
                    SongPreamble(() => SongEntry.NpcSong());
                }
            });
            Ability.Abilities.Add("song_attack", new Ability("song_attack")
            {
                Name = I18n.Bardics_Song_Attack_Name,
                Description = I18n.Bardics_Song_Attack_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 7,
                ManaCost = () => 20,
                KnownCondition = $"PLAYER_IS_COLLEGE_VALOR Current",
                HiddenIfLocked = true,
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 25);
                    SongPreamble(() => SongEntry.AttackSong());
                }
            });
            Ability.Abilities.Add("song_time", new Ability("song_time")
            {
                Name = I18n.Bardics_Song_Time_Name,
                Description = I18n.Bardics_Song_Time_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 8,
                ManaCost = () => 35,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 6",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(6),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 25);
                    SongPreamble(() => SongEntry.TimeSong());
                }
            });
            Ability.Abilities.Add("song_horse", new Ability("song_horse")
            {
                Name = () => I18n.Bardics_Song_Horse_Name(Game1.player.horseName.Value ?? I18n.Cptoken_Horse()),
                Description = I18n.Bardics_Song_Horse_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 9,
                ManaCost = () => 10,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 7",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(7),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 25);
                    SongPreamble(() => SongEntry.HorseSong());
                }
            });
            Ability.Abilities.Add("song_crop", new Ability("song_crop")
            {
                Name = I18n.Bardics_Song_Crops_Name,
                Description = I18n.Bardics_Song_Crops_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 10,
                ManaCost = () => 30,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 8",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(8),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 25);
                    SongPreamble(() => SongEntry.CropSong());
                }
            });
            Ability.Abilities.Add("song_obelisk", new Ability("song_obelisk")
            {
                Name = I18n.Bardics_Song_Obelisk_Name,
                Description = I18n.Bardics_Song_Obelisk_Description,
                TexturePath = Helper.ModContent.GetInternalAssetName("assets/abilities.png").Name,
                SpriteIndex = 11,
                ManaCost = () => 15,
                KnownCondition = $"PLAYER_DESTYNOVA.SWORDANDSORCERY.BARDICS_LEVEL Current 9",
                UnlockHint = () => I18n.Ability_Bardics_UnlockHint(9),
                Function = () =>
                {
                    Game1.player.AddCustomSkillExperience(ModUP.Skill, 25);
                    SongPreamble(() => SongEntry.ObeliskSong());
                }
            });

            Event.RegisterCommand("sns_bardicsunlock", (Event @event, string[] args, EventContext context) =>
            {
                // This implementation is incredibly lazy
                ArgUtility.TryGetVector2(args, 1, out Vector2 center, out string error);
                center.Y -= 0.5f;

                List<TemporaryAnimatedSprite> tass = new();

                @event.aboveMapSprites ??= new();

                Color[] cols = [Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Cyan, Color.Blue, Color.Purple, Color.Magenta];

                int soFar = 0;
                void makeNote()
                {
                    TemporaryAnimatedSprite tas = new(Helper.ModContent.GetInternalAssetName("assets/notes.png").Name, new Rectangle(Game1.random.Next(2) * 16, 0, 16, 16), center * Game1.tileSize + new Vector2(0, -96), false, 0, cols[soFar])
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
                        Console.WriteLine(i + " " + getSpeed() + " " + getLength());
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

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !Game1.shouldTimePass())
                return;
        }

        private void Multiplayer_ModMessageReceived(object sender, StardewModdingAPI.Events.ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            switch (e.Type)
            {
                case MultiplayerMessage_MonsterKnockback:
                    {
                        var msg = e.ReadAs<MonsterKnockbackMessage>();
                        var player = Game1.GetPlayer(e.FromPlayerID);
                        (player.currentLocation.characters.FirstOrDefault(npc => npc.id == msg.MonsterId) as Monster)?.takeDamage(0, msg.Trajectory.X, -msg.Trajectory.Y, false, 0, player);
                    }
                    break;
                case MultiplayerMessage_HorseWarp:
                    {
                        if (Game1.IsClient)
                        {
                            var msg = e.ReadAs<HorseWarpMessage>();
                            Game1.warpFarmer(msg.Location, msg.Tile.X, msg.Tile.Y, false);
                        }
                        else
                        {
                            var player = Game1.GetPlayer(e.FromPlayerID);

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

                            if (horse != null)
                            {
                                Helper.Multiplayer.SendMessage(new HorseWarpMessage()
                                {
                                    Location = horse.currentLocation.NameOrUniqueName,
                                    Tile = horse.TilePoint,
                                }, MultiplayerMessage_HorseWarp, [ModManifest.UniqueID], [e.FromPlayerID]);
                            }
                        }
                    }
                    break;
            }
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            SongEntry.Reset();
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            //var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            Skills.RegisterSkill(Skill);
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.grantConversationFriendship))]
    public static class NpcFriendshipBardicsExpPatch
    {
        public static void Postfix(int amount)
        {
            if (!Game1.player.eventsSeen.Contains(ModUP.BardicsEventId))
                return;

            Game1.player.AddCustomSkillExperience(ModUP.Skill, amount);
        }
    }
}