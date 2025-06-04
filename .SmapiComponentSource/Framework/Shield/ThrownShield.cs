using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using SwordAndSorcerySMAPI.Framework.ModSkills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SwordAndSorcerySMAPI.Framework.Shield
{

    internal class Shockwave
    {
        private readonly Vector2 Position;
        private readonly GameLocation Location;
        private readonly int Level;
        private readonly int Damage;

        private float Timer;
        private int CurrRad;

        public Shockwave(Vector2 position, GameLocation loc, int level, int damage)
        {
            Position = position;
            Location = loc;
            Level = level;
            Damage = damage;

            ModSnS.Instance.Helper.Events.GameLoop.UpdateTicked += Update;
        }

        public void Update(object sender, UpdateTickedEventArgs e)
        {
            if (--Timer > 0)
            {
                return;
            }
            Timer = 10;

            int spotsForCurrRadius = 1 + CurrRad * 7;
            for (int i = 0; i < spotsForCurrRadius; ++i)
            {
                Vector2 pixelPos = new(
                    x: Position.X + (float)Math.Cos(Math.PI * 2 / spotsForCurrRadius * i) * CurrRad * Game1.tileSize,
                    y: Position.Y + (float)Math.Sin(Math.PI * 2 / spotsForCurrRadius * i) * CurrRad * Game1.tileSize
                );

                Location.playSound("hoeHit", pixelPos);
                Game1.Multiplayer.broadcastSprites(Location, new TemporaryAnimatedSprite(6, pixelPos, Color.White, 8, Game1.random.NextDouble() < 0.5, 30), new TemporaryAnimatedSprite(12, pixelPos, Color.White, 8, Game1.random.NextDouble() < 0.5, 50f));
            }
            ++CurrRad;

            foreach (var character in Location.characters)
            {
                if (character is Monster mob)
                {
                    if (Vector2.Distance(Position, mob.Position) < CurrRad * Game1.tileSize)
                    {
                        mob.invincibleCountdown = -1;
                        Location.damageMonster(mob.GetBoundingBox(), Damage, Damage, false, 0, 0, 0, 1, false, Game1.player, true);
                    }
                }
            }

            if (CurrRad >= 1 + (Level + 1) * 2)
            {
                ModSnS.Instance.Helper.Events.GameLoop.UpdateTicked -= Update;
            }
        }
    }

    [XmlType("Mods_spacechase0_ThrowableAxe_ThrownAxe")]
    public class ThrownShield : Projectile
    {
        private readonly NetInt Damage = new(3);
        public readonly NetVector2 Target = [];
        public readonly NetCharacterRef TargetMonster = new();
        private readonly NetFloat Speed = new(1);
        private readonly NetString ShieldType = [];
        public readonly NetInt Bounces = new(1);
        public bool Dead = false;
        [XmlIgnore]
        public List<NPC> NpcsHit = [];

        public ThrownShield()
        {
            NetFields.AddField(Damage, nameof(Damage));
            NetFields.AddField(Target, nameof(Target));
            NetFields.AddField(TargetMonster.NetFields);
            NetFields.AddField(Speed, nameof(Speed));
            NetFields.AddField(ShieldType);
            NetFields.AddField(Bounces);
        }

        public ThrownShield(Farmer thrower, int damage, Vector2 target, float speed, string shieldType, int bounces)
        : this()
        {
            position.X = thrower.StandingPixel.X - 16;
            position.Y = thrower.StandingPixel.Y - 64;

            theOneWhoFiredMe.Set(thrower.currentLocation, thrower);
            damagesMonsters.Value = true;
            Damage.Value = damage;
            Target.Value = target;
            Speed.Value = speed;
            ShieldType.Value = shieldType;
            Bounces.Value = bounces;
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {
            if (NpcsHit.Contains(n))
                return;

            NpcsHit.Add(n);
            if (n is Monster)
            {
                location.damageMonster(getBoundingBox(), Damage.Value, Damage.Value, false, (Farmer)theOneWhoFiredMe.Get(location), true);
                if (theOneWhoFiredMe.Get(location) is Farmer farmer && farmer.HasCustomProfession(PaladinSkill.ProfessionShieldThrowLightning))
                {
                    float maxDist = Game1.tileSize * 1.5f * (Game1.tileSize * 1.5f);
                    foreach (var monster in location.characters.Where(npc => npc is Monster).Cast<Monster>())
                    {
                        var mpos = monster.Position;
                        var dist = Vector2.DistanceSquared(mpos, position.Value);

                        if (dist < maxDist)
                        {
                            monster.invincibleCountdown = -1;

                            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(ModSnS.Instance.Helper.ModContent.GetInternalAssetName("assets/ThorLightning.png").BaseName, new Rectangle(0, 0, 32, 48), 75, 16, 0, monster.Position - new Vector2(64, 96), false, false) { scale = 4 });
                        }
                    }
                    location.damageMonster(new Rectangle((int)position.X - Game1.tileSize * 3 / 2, (int)position.Y - Game1.tileSize * 3 / 2, Game1.tileSize * 3, Game1.tileSize * 3), Damage.Value, Damage.Value, false, (Farmer)theOneWhoFiredMe.Get(location), true);
                }
                if (ShieldType.Value == "(W)DN.SnS_SorcererShield" && Game1.random.NextDouble() < 0.15)
                {
                    _ = new Shockwave(getBoundingBox().Center.ToVector2(), location, 0, Damage.Value);
                }
                if (n == TargetMonster.Get(location))
                {
                    TargetMonster.Clear();
                    if (Bounces.Value > 0)
                    {
                        FindTargetMonster(location);
                    }
                }
            }
        }

        public override void behaviorOnCollisionWithOther(GameLocation location)
        {
        }

        public override void behaviorOnCollisionWithPlayer(GameLocation location, Farmer player)
        {
        }

        public override void behaviorOnCollisionWithTerrainFeature(TerrainFeature t, Vector2 tileLocation, GameLocation location)
        {
        }

        private void FindTargetMonster(GameLocation loc)
        {
            float maxDist = Game1.tileSize * 4 * (Game1.tileSize * 4);

            float leastDist = float.MaxValue;
            Monster leastMonster = null;
            foreach (var monster in loc.characters.Where(npc => npc is Monster && !NpcsHit.Contains(npc)).Cast<Monster>())
            {
                var mpos = monster.Position;
                var dist = Vector2.DistanceSquared(mpos, position.Value);

                if (dist < leastDist && dist < maxDist)
                {
                    leastDist = dist;
                    leastMonster = monster;
                }
            }

            if (leastMonster != null)
            {
                TargetMonster.Set(loc, leastMonster);
                Bounces.Value--;
            }
        }

        public override bool update(GameTime time, GameLocation location)
        {
            if (TargetMonster.Get(location) == null && Bounces.Value > 0)
            {
                FindTargetMonster(location);
            }
            else
            {
                if (TargetMonster.Get(location) != null)
                    Target.Value = TargetMonster.Get(location).getStandingPosition();
            }

            base.update(time, location);

            return Dead;
        }

        public override Rectangle getBoundingBox()
        {
            return new((int)position.X, (int)position.Y, 64, 64);
        }

        public override void updatePosition(GameTime time)
        {
            Vector2 targetDiff = Target.Value - position.Value;
            Vector2 targetDir = targetDiff;
            targetDir.Normalize();

            if (targetDiff.Length() < Speed.Value)
                position.Value = Target.Value;
            else
                position.Value += targetDir * Speed.Value;

            //Log.trace($"{position.Value} {target.Value} {targetDir}");
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(ItemRegistry.GetDataOrErrorItem(ShieldType.Value).GetTexture(), Game1.GlobalToLocal(Game1.viewport, position.Value + new Vector2(32, 32)), ItemRegistry.GetDataOrErrorItem(ShieldType.Value).GetSourceRect(), Color.White, rotation, new Vector2(8, 8), 4, SpriteEffects.None, 1);
            rotation += 0.3f;
        }

        public Vector2 GetPosition()
        {
            return position.Value;
        }
    }
}
