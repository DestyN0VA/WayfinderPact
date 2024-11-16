using HarmonyLib;
using Microsoft.Build.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using SpaceCore;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI
{
    internal static class Spells
    {
        public static int GetSpellDamange(int BaseDamange, int LevelAddition, out int Max)
        {
            Farmer farmer = Game1.player;
            float Damage = BaseDamange;
            float Mult = 0;

            for (int i = 0; i < farmer.GetCustomSkillLevel("DestyNova.SwordAndSorcery.Witchcraft"); i++)
            {
                if (i >= 10) break;
                Damage += LevelAddition;
            }

            if (farmer.HasCustomProfession(WitchcraftSkill.ProfessionSpellDamage))
                Mult += 0.25f;

            Damage *= 1 + Mult;
            Max = (int)(Damage * 1.2);

            return (int)Damage;
        }

        public static void Haste()
        {
            var buff = new Buff("spell_haste", "spell_haste", duration: 7000 * 6 * 5, effects: new StardewValley.Buffs.BuffEffects() { Speed = { 1 } }, displayName: I18n.Witchcraft_Spell_Haste_Name() );
            Game1.player.applyBuff( buff );
        }

        public static void Polymorph()
        {
            Vector2 pos = ModSnS.instance.Helper.Input.GetCursorPosition().AbsolutePixels;
            if (Game1.options.gamepadControls)
            {
                pos = Game1.player.Position;
            }

            if (Game1.IsClient)
            {
                ModTOP.Instance.Helper.Multiplayer.SendMessage(pos, ModTOP.MultiplayerMessage_Polymorph, [ModTOP.Instance.ModManifest.UniqueID], [Game1.MasterPlayer.UniqueMultiplayerID]);
            }
            else
            {
                PolymorphImpl(Game1.player, pos);
            }
        }

        public static void PolymorphImpl(Farmer player, Vector2 pos)
        {
            var data = Game1.content.Load<Dictionary<string, MonsterExtensionData>>("KCC.SnS/MonsterExtensionData");

            Monster closestMonster = null;
            float closestDist = float.MaxValue;
            foreach (var monster in player.currentLocation.characters.Where(npc => npc is Monster).Cast<Monster>())
            {
                if (monster is GreenSlime)
                    continue;
                if (data.TryGetValue(monster.Name, out var specificData) && !specificData.CanPolymorph)
                    continue;

                float dist = Vector2.Distance(pos, monster.GetBoundingBox().Center.ToVector2());
                if (closestDist > dist)
                {
                    closestMonster = monster;
                    closestDist = dist;
                }
            }

            if (closestMonster != null)
            {
                var slime = new GreenSlime(closestMonster.Position);
                slime.focusedOnFarmers = true;

                closestMonster.currentLocation.characters.Add(slime);
                closestMonster.currentLocation.characters.Remove(closestMonster);
                ModSnS.State.Polymorphed.Add(slime, new()
                {
                    Original = closestMonster
                });
                
                player.AddCustomSkillExperience(ModTOP.Skill, 5 * ModTOP.WitchcraftExpMultiplier);
            }
        }

        public static void Stasis()
        {
            Game1.player.GetFarmerExtData().stasisTimer.Value = 3;
        }

        public static void MageArmor()
        {
            Game1.player.GetFarmerExtData().mageArmor = true;
        }

        public static void WallOfForce()
        {
            Vector2 facingOffset = Vector2.Zero;
            switch (ModSnS.State.PreCastFacingDirection)
            {
                case Game1.up: facingOffset = new(0, -1); break;
                case Game1.down: facingOffset = new(0, 1); break;
                case Game1.left: facingOffset = new(-1, 0); break;
                case Game1.right: facingOffset = new(1, 0); break;
            }
            Vector2 sideOffset = new(facingOffset.Y, facingOffset.X);

            for (int io = -3; io <= 3; ++io)
            {
                Vector2 pos = Game1.player.Tile + facingOffset * 2 + sideOffset * io;

                if (Game1.player.currentLocation.Objects.ContainsKey(pos))
                    continue;

                Game1.player.currentLocation.Objects.Add(pos, new StardewValley.Object(pos, "DN.SnS_WallOfForce") { MinutesUntilReady = 60 });
            }
        }

        public static void Banish()
        {
            Vector2 pos = ModSnS.instance.Helper.Input.GetCursorPosition().AbsolutePixels;
            if (Game1.options.gamepadControls)
            {
                pos = Game1.player.Position;
            }

            if (Game1.IsClient)
            {
                ModTOP.Instance.Helper.Multiplayer.SendMessage(pos, ModTOP.MultiplayerMessage_Banish, [ModTOP.Instance.ModManifest.UniqueID], [Game1.MasterPlayer.UniqueMultiplayerID]);
            }
            else
            {
                BanishImpl(Game1.player, pos);
            }
        }

        public static void BanishImpl(Farmer player, Vector2 pos)
        {
            var data = Game1.content.Load<Dictionary<string, MonsterExtensionData>>("KCC.SnS/MonsterExtensionData");

            Monster closestMonster = null;
            float closestDist = float.MaxValue;
            foreach (var monster in player.currentLocation.characters.Where(npc => npc is Monster).Cast<Monster>())
            {
                if (data.TryGetValue(monster.Name, out var specificData) && !specificData.CanBanish)
                    continue;

                float dist = Vector2.Distance(pos, monster.GetBoundingBox().Center.ToVector2());
                if (closestDist > dist)
                {
                    closestMonster = monster;
                    closestDist = dist;
                }
            }

            if (closestMonster != null)
            {
                ModSnS.State.Banished.Add(closestMonster, new()
                {
                    Location = closestMonster.currentLocation
                });
                closestMonster.currentLocation.characters.Remove(closestMonster);

                player.AddCustomSkillExperience(ModTOP.Skill, 8 * ModTOP.WitchcraftExpMultiplier);
            }
        }
        public static void RevivePlant()
        {
            for (int ix = -2; ix <= 2; ++ix)
            {
                for (int iy = -2; iy <= 2; ++iy)
                {
                    Vector2 pos = Game1.player.Tile + new Vector2(ix, iy);
                    if (Game1.player.currentLocation.terrainFeatures.TryGetValue(pos, out TerrainFeature tf) && tf is HoeDirt hd)
                    {
                        if (hd.crop.dead.Value)
                        {
                            hd.crop.dead.Value = false;
                        }
                    }
                }
            }
        }

        public static void MirrorImage()
        {
            Game1.player.GetFarmerExtData().mirrorImages.Value = 3;

            Game1.player.playNearbySoundLocal("coldSpell");

            for (int im = 1; im <= 3; ++im)
            {
                Vector2 spot = Game1.player.StandingPixel.ToVector2();
                float rad = (float)-Game1.currentGameTime.TotalGameTime.TotalSeconds / 3 * 2;
                /*
                switch (im)
                {
                    case 1: spot -= new Vector2(0, Game1.tileSize); break;
                    case 2: spot += new Vector2(-Game1.tileSize, Game1.tileSize); break;
                    case 3: spot += new Vector2(Game1.tileSize, Game1.tileSize); break;
                }
                */
                rad += MathF.PI * 2 / 3 * (im - 1);
                spot += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);

                for (int i = 0; i < 8; ++i)
                {
                    Vector2 diff = new Vector2(Game1.random.Next(96) - 48, Game1.random.Next(96) - 48);
                    Game1.player.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, spot - new Vector2(32, 48) + diff, flicker: false, flipped: false));
                }
            }
        }

        public static void FindFamiliar()
        {
            if (Game1.player.companions.Any(c => c is FamiliarCompanion))
                return;

            Game1.player.AddCompanion(new FamiliarCompanion());
            Game1.player.buffs.Apply(new Buff("FamiliarLuck", "familiar", I18n.Witchcraft_Spell_FindFamiliar_Name(), Buff.ENDLESS, null, 4, new() { LuckLevel = { 1 } }, false, I18n.Witchcraft_Spell_FindFamiliar_Name(), I18n.Witchcraft_Spell_FindFamiliar_Description()));
        }

        public static void GhostlyProjection()
        {
            var ext = Game1.player.GetFarmerExtData();

            ext.isGhost.Value = true;
            ext.ghostOrigPosition.Value = Game1.player.Position;
            Game1.player.ignoreCollisions = true;

            DelayedAction.functionAfterDelay(() =>
            {
                if (!ext.isGhost.Value)
                    return;

                ext.isGhost.Value = false;
                Game1.player.Position = ext.ghostOrigPosition.Value;
                Game1.player.ignoreCollisions = false;
            }, 15000);
        }

        public static void PocketChest()
        {
            string invName = $"{ModTOP.Instance.ModManifest.UniqueID}/PocketChest/{Game1.player.UniqueMultiplayerID}";

            var chest = new Chest();
            chest.GlobalInventoryId = invName;
            chest.ShowMenu();
        }

        public static void PocketDimension()
        {
            ModSnS.State.PocketDimensionLocation = Game1.player.currentLocation.NameOrUniqueName;
            ModSnS.State.PocketDimensionCoordinates = Game1.player.TilePoint;
            Game1.player.currentLocation.performTouchAction($"MagicWarp EastScarp_PocketDimension 15 8", Game1.player.getStandingPosition());
        }

        public static void Fireball(Vector2 MousePos)
        {
            Farmer farmer = Game1.player;
            GameLocation location = farmer.currentLocation;
            var PlayerPos = farmer.getStandingPosition();
            var TargetPos = new Vector2(Game1.viewport.X, Game1.viewport.Y) + MousePos;

            int time = 1000;
            float speed = Vector2.Distance(PlayerPos, TargetPos) / 64f;
            if (speed < 8)
            {
                time = (int)(speed * (1000 / 8));
                speed = 8;
            }

            Vector2 motion = Utility.getVelocityTowardPoint(PlayerPos, TargetPos, speed);

            DebuffingProjectile Fireball = new(null, 10, 0, 5, 0.1f, motion.X, motion.Y, PlayerPos - new Vector2(32f, 48f), location, farmer, hitsMonsters: true, playDefaultSoundOnFire: false);
            Fireball.uniqueID.Value = Game1.random.Next();
            Fireball.wavyMotion.Value = false;
            Fireball.piercesLeft.Value = 99999;
            Fireball.IgnoreLocationCollision = true;
            Fireball.ignoreObjectCollisions.Value = true;
            Fireball.projectileID.Value = 15;
            Fireball.alpha.Value = 0.001f;
            Fireball.alphaChange.Value = 0.05f;
            Fireball.light.Value = true;
            Fireball.boundingBoxWidth.Value = 32;
            location.projectiles.Add(Fireball);
            location.playSound("fireball");

            TargetPos = TargetPos - new Vector2(32f, 48f);

            Fireballs.Add(new(Fireball, time));
        }

        public static void Icebolt(Vector2 MousePos)
        {
            Farmer farmer = Game1.player;
            GameLocation location = farmer.currentLocation;
            var PlayerPos = farmer.getStandingPosition();
            var TargetPos = new Vector2(Game1.viewport.X, Game1.viewport.Y) + MousePos;

            int time = 500;
            float speed = Vector2.Distance(PlayerPos, TargetPos) / 64f * 2;
            if (speed < 4)
            {
                time = (int)(speed * (500 / 4));
                speed = 4;
            }

            Vector2 motion = Utility.getVelocityTowardPoint(PlayerPos, TargetPos, speed);

            DebuffingProjectile Icebolt = new("frozen", 17, 0, 3, 1f, motion.X, motion.Y, PlayerPos - new Vector2(32f, 48f), location, farmer, hitsMonsters: true, playDefaultSoundOnFire: false);
            Icebolt.uniqueID.Value = Game1.random.Next();
            Icebolt.wavyMotion.Value = false;
            Icebolt.piercesLeft.Value = 99999;
            Icebolt.IgnoreLocationCollision = true;
            Icebolt.ignoreObjectCollisions.Value = true;
            Icebolt.projectileID.Value = 15;
            Icebolt.alpha.Value = 0.001f;
            Icebolt.alphaChange.Value = 0.05f;
            Icebolt.light.Value = true;
            Icebolt.boundingBoxWidth.Value = 32;

            location.projectiles.Add(Icebolt);
            location.playSound("fireball");

            TargetPos = TargetPos - new Vector2(32f, 48f);

            Icebolts.Add(new(Icebolt, time));
        }

        public static void MagicMissle()
        {
            Farmer farmer = Game1.player;
            GameLocation location = farmer.currentLocation;
            Vector2 PlayerPos = farmer.Position;
            
            if (Utility.findClosestMonsterWithinRange(location, PlayerPos, 100 * 64) == null)
            {
                Ability.Abilities.TryGetValue("spell_magicmissle", out Ability abil);
                farmer.GetFarmerExtData().mana.Value += abil.ManaCost();
                Game1.showRedMessage(I18n.Witchcraft_Spell_NoEnemyFound(I18n.Witchcraft_Spell_MagicMissle_Name()));
                return;
            }

            var TargetPos1 = PlayerPos + new Vector2(0, -64);
            SetUpMagicMissle(location, TargetPos1, Utility.getVelocityTowardPoint(PlayerPos, TargetPos1, 4));

            var TargetPos2 = PlayerPos + new Vector2(64, 64);
            SetUpMagicMissle(location, TargetPos2, Utility.getVelocityTowardPoint(PlayerPos, TargetPos2, 4));

            var TargetPos3 = PlayerPos + new Vector2(-64, 64);
            SetUpMagicMissle(location, TargetPos3, Utility.getVelocityTowardPoint(PlayerPos, TargetPos3, 4));
        }

        private static void SetUpMagicMissle(GameLocation location, Vector2 TargetPos, Vector2 Motion)
        {
            int Damage = GetSpellDamange(50, 10, out _);

            BasicProjectile MagicMissle = new(Damage / 3, 8, 999, 10, 0, 0, 0, TargetPos, damagesMonsters: true, location: location, firer: Game1.player);
            MagicMissle.uniqueID.Value = Game1.random.Next();
            MagicMissle.lightSourceId = "Magic Missle";
            MagicMissle.maxTravelDistance.Value = 3000;
            MagicMissle.IgnoreLocationCollision = true;
            MagicMissle.ignoreObjectCollisions.Value = true;
            MagicMissle.alpha.Value = 0.001f;
            MagicMissle.alphaChange.Value = 0.05f;
            MagicMissle.boundingBoxWidth.Value = 32;
            MagicMissle.xVelocity.Value = Motion.X;
            MagicMissle.yVelocity.Value = Motion.Y;
            location.projectiles.Add(MagicMissle);
        }

        public static Dictionary<int, List<Monster>> Monsters = [];

        public static void LightningBolt()
        {
            int i = 0;
            var keys = Monsters.Keys.ToList();
            foreach (var key in keys)
                if (i < key)
                    i = key + 1;

            Farmer farmer = Game1.player;
            GameLocation location = farmer.currentLocation;
            Monster m = Utility.findClosestMonsterWithinRange(location, Game1.player.Position, 15 * 64);
            if (m is null)
            {
                Ability.Abilities.TryGetValue("spell_lightningbolt", out Ability abil);
                Game1.player.GetFarmerExtData().mana.Value += abil.ManaCost();
                Game1.showRedMessage(I18n.Witchcraft_Spell_NoEnemyFound(I18n.Witchcraft_Spell_LightningBolt_Name()));
                return;
            }
            else Monsters.Add(i, [m]);

            TemporaryAnimatedSprite LightningBolt = new(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/ThorLightning.png").BaseName, new(0, 0, 32, 48), 75, 16, 1, m.Position - new Vector2(64, 64 * 3), false, false) { scale = 4, layerDepth = 1 };
            location.TemporarySprites.Add(LightningBolt);
            location.damageMonster(m.GetBoundingBox(), GetSpellDamange(50, 10, out int Max), Max, false, farmer);
            Game1.playSound("thunder");
            DelayedAction.functionAfterDelay(() => ChainLightningBolt(farmer, location, m, i, 5), 500);
        }

        public static void ChainLightningBolt(Farmer farmer, GameLocation location, Monster monster, int DictKey, int Chain)
        {
            Monster m = Utility.findClosestMonsterWithinRange(location, monster.Position, 15 * 64, match: l => !Monsters[DictKey].Contains(l));
            if (m is null || Chain <= 0)
            {
                Monsters.Remove(DictKey);
                return;
            }
            else Monsters[DictKey].Add(m);
            TemporaryAnimatedSprite LightningBolt = new(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/ThorLightning.png").BaseName, new(0, 0, 32, 48), 75, 16, 1, m.Position - new Vector2(64, 64 * 3), false, false) { scale = 4, layerDepth = 1 };
            location.TemporarySprites.Add(LightningBolt);
            location.damageMonster(m.GetBoundingBox(), GetSpellDamange(50 - ((6 - Chain) * 10), 10 - (6 - Chain), out int Max), Max, false, farmer);
            DelayedAction.functionAfterDelay(() => ChainLightningBolt(farmer, location, m, DictKey, Chain - 1), 500);
            Game1.playSound("thunder");
        }

        public static List<Tuple<Projectile, bool, float, float>> MagicMissles = [];
        public static List<Tuple<Projectile, float>> Fireballs = [];
        public static List<Tuple<Projectile, float>> Icebolts = [];

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.update))]
        public static class ProjectileSpellsPatch
        {
            public static void Postfix(Projectile __instance, GameTime time, GameLocation location)
            {
                if (__instance.lightSourceId == "Magic Missle")
                {
                    if (!MagicMissles.Any(p => p.Item1 == __instance))
                    {
                        MagicMissles.Add(new(__instance, false, __instance.xVelocity.Value / 50, __instance.yVelocity.Value / 50));
                    }

                    var p = MagicMissles.First(p => p.Item1 == __instance);

                    if (!p.Item2)
                    {

                        if (MathF.Round(__instance.xVelocity.Value, MidpointRounding.ToZero) != 0)
                            __instance.xVelocity.Value -= p.Item3;

                        if (MathF.Round(__instance.yVelocity.Value, MidpointRounding.ToZero) != 0)
                            __instance.yVelocity.Value -= p.Item4;

                        if (MathF.Round(__instance.xVelocity.Value, MidpointRounding.ToZero) == 0 && MathF.Round(__instance.yVelocity.Value, MidpointRounding.ToZero) == 0)
                            MagicMissles[MagicMissles.IndexOf(p)] = new(p.Item1, true, p.Item3, p.Item4);
                    }
                    else
                    {
                        if (__instance.acceleration.Value != Vector2.Zero) __instance.acceleration.Value = Vector2.Zero;
                        Vector2 Motion;
                        Monster m = Utility.findClosestMonsterWithinRange(location, __instance.position.Value, 100 * 64);
                        if (m is not null)
                        {
                            Motion = Utility.getVelocityTowardPoint(__instance.position.Value, Utility.PointToVector2(m.GetBoundingBox().Center), 10);
                        }
                        else
                        {
                            Motion = Utility.getVelocityTowardPoint(__instance.position.Value, Utility.PointToVector2(Game1.player.GetBoundingBox().Center), 10);
                            if (Game1.player.GetBoundingBox().Contains(__instance.position.Value))
                            {
                                location.projectiles.Remove(__instance);
                                MagicMissles.Remove(p);
                                return;
                            }
                        }
                        __instance.xVelocity.Value = (int)Motion.X;
                        __instance.yVelocity.Value = (int)Motion.Y;
                    }
                }
                else if (Fireballs.Any(f => f.Item1 == __instance))
                {
                    var Fireball = Fireballs.First(f => f.Item1 == __instance);
                    if (Fireball.Item2 - (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds <= 0)
                    {
                        int Min = GetSpellDamange(30, 10, out int Max);
                        int Min2 = GetSpellDamange(20, 8, out int Max2);

                        location.projectiles.RemoveWhere(f => f.uniqueID == Fireball.Item1.uniqueID);
                        location.playSound("explosion");
                        Fireballs.Remove(Fireball);

                        List<Monster> BurnMonsters = [];

                        foreach (Monster m in location.characters.Where(c => c is Monster))
                        {
                            if (Vector2.Distance(__instance.position.Value, m.Position) <= -5 * 64 || Vector2.Distance(__instance.position.Value, m.Position) >= 5 * 64) continue;

                            BurnMonsters.Add(m);
                        }

                        foreach (Monster m in BurnMonsters)
                        {
                            location.damageMonster(m.GetBoundingBox(), Min, Max, false, Game1.player, true);

                            DelayedAction.functionAfterDelay(() => { if (m.Health > 0) location.damageMonster(m.GetBoundingBox(), Min2, Max2, isBomb: false, Game1.player); }, 1000);
                            DelayedAction.functionAfterDelay(() => { if (m.Health > 0) location.damageMonster(m.GetBoundingBox(), Min2, Max2, isBomb: false, Game1.player); }, 2000);
                            DelayedAction.functionAfterDelay(() => { if (m.Health > 0) location.damageMonster(m.GetBoundingBox(), Min2, Max2, isBomb: false, Game1.player); }, 3000);
                        }
                    }
                    else
                    {
                        Fireballs[Fireballs.IndexOf(Fireball)] = new(Fireball.Item1, Fireball.Item2 - (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds);
                    }
                }
                else if (Icebolts.Any(i => i.Item1 == __instance))
                {
                    var Icebolt = Icebolts.First(i => i.Item1 == __instance);
                    if (Icebolt.Item2 - (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds <= 0)
                    {
                        location.projectiles.RemoveWhere(p => p.uniqueID == Icebolt.Item1.uniqueID);
                        location.playSound("frozen");
                        Icebolts.Remove(Icebolt);
                        int Min = GetSpellDamange(20, 5, out int Max);

                        List<Monster> FreezeMonsters = [];

                        foreach (Monster m in location.characters.Where(c => c is Monster))
                        {
                            if (Vector2.Distance(__instance.position.Value, m.Position) <= -5 * 64 || Vector2.Distance(__instance.position.Value, m.Position) >= 5 * 64) continue;
                            FreezeMonsters.Add(m);
                        }

                        foreach (Monster m in FreezeMonsters)
                        {
                            if (m is not DuskspireMonster || m.stunTime.Value <= 0)
                                m.stunTime.Value = 10000;
                            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(118, 227, 16, 13), new Vector2(0f, 0f), flipped: false, 0f, Color.White)
                            {
                                layerDepth = (float)(m.StandingPixel.Y + 2) / 10000f,
                                animationLength = 1,
                                interval = 5000,
                                scale = 4f,
                                id = (int)(m.position.X * 777f + m.position.Y * 77777f),
                                positionFollowsAttachedCharacter = true,
                                attachedCharacter = m
                            });

                            location.damageMonster(m.GetBoundingBox(), Min, Max, false, Game1.player, true);
                        }
                    }
                    else
                    {
                        Icebolts[Icebolts.IndexOf(Icebolt)] = new(Icebolt.Item1, Icebolt.Item2 - (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(BasicProjectile), nameof(BasicProjectile.behaviorOnCollisionWithMonster))]
        public static class RemoveMagicMissleDataIfHitMonsters
        {
            public static void Postfix(BasicProjectile __instance)
            {
                if (__instance.lightSourceId != "Magic Missle") return;
                MagicMissles.RemoveWhere(p => p.Item1 == __instance);
            }
        }
    }
}
