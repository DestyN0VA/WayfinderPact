using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;

namespace SwordAndSorcerySMAPI
{
    [XmlType("Mods_spacechase0_ThrowableAxe_ThrownAxe")]
    public class ThrownShield : Projectile
    {
        private readonly NetInt Damage = new(3);
        public readonly NetVector2 Target = new();
        private readonly NetFloat Speed = new(1);
        public bool Dead = false;
        [XmlIgnore]
        public List<NPC> NpcsHit = new();

        public ThrownShield()
        {
            this.NetFields.AddField(this.Damage, nameof(this.Damage));
            this.NetFields.AddField(this.Target, nameof(this.Target));
            this.NetFields.AddField(this.Speed, nameof(this.Speed));
        }

        public ThrownShield(Farmer thrower, int damage, Vector2 target, float speed)
        : this()
        {
            this.position.X = thrower.StandingPixel.X - 16;
            this.position.Y = thrower.StandingPixel.Y - 64;

            this.theOneWhoFiredMe.Set(thrower.currentLocation, thrower);
            this.damagesMonsters.Value = true;
            this.Damage.Value = damage;
            this.Target.Value = target;
            this.Speed.Value = speed;
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {
            if (this.NpcsHit.Contains(n))
                return;

            this.NpcsHit.Add(n);
            if (n is Monster)
            {
                location.damageMonster(this.getBoundingBox(), this.Damage.Value, this.Damage.Value, false, (Farmer)this.theOneWhoFiredMe.Get(location));
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

        public override bool update(GameTime time, GameLocation location)
        {
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
            Rectangle sourceRect = new(0, 0, 16, 16);
            //b.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, getBoundingBox()), null, Color.Red, 0, Vector2.Zero, SpriteEffects.None, 0.99f);
            b.Draw(ModSnS.ShieldItemTexture, Game1.GlobalToLocal(Game1.viewport, this.position.Value + new Vector2(32, 32)), sourceRect, Color.White, this.rotation, new Vector2(8, 8), 4, SpriteEffects.None, 1);
            this.rotation += 0.3f;
        }

        public Vector2 GetPosition()
        {
            return this.position.Value;
        }
    }
}
