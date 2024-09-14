using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using NeverEndingAdventure.Utils;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;

namespace SwordAndSorcerySMAPI
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
            this.Position = position;
            this.Location = loc;
            this.Level = level;
            this.Damage = damage;

            ModSnS.instance.Helper.Events.GameLoop.UpdateTicked += Update;
        }

        public void Update(object sender, UpdateTickedEventArgs e)
        {
            if (--this.Timer > 0)
            {
                return;
            }
            this.Timer = 10;

            int spotsForCurrRadius = 1 + this.CurrRad * 7;
            for (int i = 0; i < spotsForCurrRadius; ++i)
            {
                Vector2 pixelPos = new(
                    x: this.Position.X + (float)Math.Cos(Math.PI * 2 / spotsForCurrRadius * i) * this.CurrRad * Game1.tileSize,
                    y: this.Position.Y + (float)Math.Sin(Math.PI * 2 / spotsForCurrRadius * i) * this.CurrRad * Game1.tileSize
                );

                Location.playSound("hoeHit", pixelPos);
                Game1.Multiplayer.broadcastSprites(Location, new TemporaryAnimatedSprite(6, pixelPos, Color.White, 8, Game1.random.NextDouble() < 0.5, 30), new TemporaryAnimatedSprite(12, pixelPos, Color.White, 8, Game1.random.NextDouble() < 0.5, 50f));
            }
            ++this.CurrRad;

            foreach (var character in Location.characters)
            {
                if (character is Monster mob)
                {
                    if (Vector2.Distance(Position, mob.Position) < this.CurrRad * Game1.tileSize)
                    {
                        mob.invincibleCountdown = -1;
                        Location.damageMonster(mob.GetBoundingBox(), Damage, Damage, false, 0, 0, 0, 1, false, Game1.player, true);
                    }
                }
            }

            if (this.CurrRad >= 1 + (this.Level + 1) * 2)
            {
                ModSnS.instance.Helper.Events.GameLoop.UpdateTicked -= Update;
            }
        }
    }

    [XmlType("Mods_spacechase0_ThrowableAxe_ThrownAxe")]
    public class ThrownShield : Projectile
    {
        private readonly NetInt Damage = new(3);
        public readonly NetVector2 Target = new();
        public readonly NetCharacterRef TargetMonster = new();
        private readonly NetFloat Speed = new(1);
        private readonly NetString ShieldType = new();
        public readonly NetInt Bounces = new(1);
        public bool Dead = false;
        [XmlIgnore]
        public List<NPC> NpcsHit = new();

        public ThrownShield()
        {
            this.NetFields.AddField(this.Damage, nameof(this.Damage));
            this.NetFields.AddField(this.Target, nameof(this.Target));
            this.NetFields.AddField(this.TargetMonster.NetFields);
            this.NetFields.AddField(this.Speed, nameof(this.Speed));
            this.NetFields.AddField(ShieldType);
            this.NetFields.AddField(Bounces);
        }

        public ThrownShield(Farmer thrower, int damage, Vector2 target, float speed, string shieldType, int bounces)
        : this()
        {
            this.position.X = thrower.StandingPixel.X - 16;
            this.position.Y = thrower.StandingPixel.Y - 64;

            this.theOneWhoFiredMe.Set(thrower.currentLocation, thrower);
            this.damagesMonsters.Value = true;
            this.Damage.Value = damage;
            this.Target.Value = target;
            this.Speed.Value = speed;
            this.ShieldType.Value = shieldType;
            Bounces.Value = bounces;
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {
            if (this.NpcsHit.Contains(n))
                return;

            this.NpcsHit.Add(n);
            if (n is Monster)
            {
                location.damageMonster(this.getBoundingBox(), this.Damage.Value, this.Damage.Value, false, (Farmer)this.theOneWhoFiredMe.Get(location), true);
                if (theOneWhoFiredMe.Get(location) is Farmer farmer && farmer.HasCustomProfession(PaladinSkill.ProfessionShieldThrowLightning))
                {
                    float maxDist = (Game1.tileSize * 1.5f) * (Game1.tileSize * 1.5f);
                    foreach (var monster in location.characters.Where(npc => npc is Monster).Cast<Monster>())
                    {
                        var mpos = monster.Position;
                        var dist = Vector2.DistanceSquared(mpos, position.Value);

                        if (dist < maxDist)
                        {
                            monster.invincibleCountdown = -1;
                            
                            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/ThorLightning.png").BaseName, new Rectangle(0, 0, 68, 48), 50, 16/2, 0, monster.Position - new Vector2(Game1.tileSize, Game1.tileSize/2), false, false) { scale = 2 } );
                        }
                    }
                    location.damageMonster(new Rectangle((int)position.X - Game1.tileSize * 3 / 2, (int)position.Y - Game1.tileSize * 3 / 2, Game1.tileSize * 3, Game1.tileSize * 3), this.Damage.Value, this.Damage.Value, false, (Farmer)this.theOneWhoFiredMe.Get(location), true);
                }
                if (ShieldType.Value == "(W)DN.SnS_SorcererShield" && Game1.random.NextDouble() < 0.15)
                {
                    new Shockwave(getBoundingBox().Center.ToVector2(), location, 0, Damage.Value);
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
            float maxDist = (Game1.tileSize * 4) * (Game1.tileSize * 4);

            float leastDist = float.MaxValue;
            Monster leastMonster = null;
            foreach (var monster in loc.characters.Where(npc => npc is Monster && !NpcsHit.Contains(npc)).Cast<Monster>() )
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
                    Target.Value = TargetMonster.Get(location).Position;
            }

            base.update(time, location);

            return this.Dead;
        }

        public override Rectangle getBoundingBox()
        {
            return new((int)position.X, (int)position.Y, 64, 64);
        }

        public override void updatePosition(GameTime time)
        {
            Vector2 targetDiff = this.Target.Value - this.position.Value;
            Vector2 targetDir = targetDiff;
            targetDir.Normalize();

            if (targetDiff.Length() < this.Speed.Value)
                this.position.Value = this.Target.Value;
            else
                this.position.Value += targetDir * this.Speed.Value;

            //Log.trace($"{position.Value} {target.Value} {targetDir}");
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(ItemRegistry.GetDataOrErrorItem(ShieldType.Value).GetTexture(), Game1.GlobalToLocal(Game1.viewport, this.position.Value + new Vector2(32, 32)), ItemRegistry.GetDataOrErrorItem(ShieldType.Value).GetSourceRect(), Color.White, this.rotation, new Vector2(8, 8), 4, SpriteEffects.None, 1);
            this.rotation += 0.3f;
        }

        public Vector2 GetPosition()
        {
            return this.position.Value;
        }
    }
}
