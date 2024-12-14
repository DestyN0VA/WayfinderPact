using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Companions;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace SwordAndSorcerySMAPI;
public class DuskspireMonster(Vector2 pos, string name = "Duskspire Behemoth") : Monster(name, pos)
{
    private readonly NetEvent0 laughEvent = new();
    private readonly NetEvent1Field<bool, NetBool> swingEvent = new();
    private readonly NetFloat noMovementTime = [];

    private int[] Up = [8, 9, 10, 11];
    private int[] Down = [0, 1, 2, 3];
    private int[] Left = [12, 13, 14, 15];
    private int[] Right = [4, 5, 6, 7];
    private int WalkTimer = 0;
    private int currWalkIndex = 0;

    private int prevFrame = 0;
    private Vector2 lastPos = Vector2.Zero;
    private Vector2 DeathPos = Vector2.Zero;
    private bool flippedSwing = false;
    private bool doingLaugh = false;

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(laughEvent);
        NetFields.AddField(swingEvent);
        NetFields.AddField(noMovementTime);

        laughEvent.onEvent += LaughEvent_onEvent;
        swingEvent.onEvent += SwingEvent_onEvent;
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        base.reloadSprite(onlyAppearance);

        Sprite = new AnimatedSprite(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth.png").BaseName, 0, 96, 96);
    }

    public override Rectangle GetBoundingBox()
    {
        var ret = new Rectangle((int)Position.X - 160 / 2, (int)Position.Y - 160, 160, 160);

        if (Sprite.CurrentFrame >= 44 && Sprite.CurrentFrame <= 49)
        {
            int sizeDiff = (96 * Game1.pixelZoom - 160) / 2;
            ret.X -= sizeDiff / 2;
            ret.Y -= sizeDiff / 2;
            ret.Width += sizeDiff;
            ret.Height += sizeDiff;
        }

        return ret;
    }

    public override void behaviorAtGameTick(GameTime time)
    {
    }

    public override void updateMovement(GameLocation location, GameTime time)
    {
    }

    public override void update(GameTime time, GameLocation location)
    {
        Sprite.SpriteWidth = 96;
        Sprite.SpriteHeight = 96;
        prevFrame = Sprite.CurrentFrame;

        //Health = 99999;

        base.update(time, location);
        laughEvent.Poll();
        swingEvent.Poll();

        if (Game1.IsMasterGame)
        {
            if (prevFrame != 61 && Sprite.CurrentFrame == 61)
            {
                if (!doingLaugh)
                {
                    string[] projectileDebuffs =
                    [
                        // Darkness, nauseous, weakness, jinxed, slimed
                        "26", "25", "27", "14", "13"
                    ];

                    for (int i = 0; i < 16; ++i)
                    {
                        float angle = (360 / 16 * i) * MathF.PI / 180;
                        float xVel = MathF.Cos(angle) * 10;
                        float yVel = MathF.Sin(angle) * 10;
                        DebuffingProjectile proj = new(projectileDebuffs[Game1.random.Next(projectileDebuffs.Length)], 2, 1, 2, 0, xVel, yVel, Position, location, this, false, false);
                        location.projectiles.Add(proj);
                    }
                }
                doingLaugh = !doingLaugh;
            }

            if (noMovementTime.Value > 0)
                noMovementTime.Value -= time.ElapsedGameTime.Milliseconds;
            if (stunTime.Value > 0)
            {
                stunTime.Value -= time.ElapsedGameTime.Milliseconds;
                noMovementTime.Value = stunTime.Value;
            }

            //Log.Debug("nmt:" + noMovementTime.Value);
            if (noMovementTime.Value <= 0)
            {
                var farmer = findPlayer();
                if ( farmer.currentLocation == location )
                {
                    float dist = Vector2.Distance(farmer.StandingPixel.ToVector2(), GetBoundingBox().Center.ToVector2());

                    //Log.Debug("dist : " + dist);
                    if (Game1.random.NextDouble() < 1f / (5 * 60))
                    {
                        doingLaugh = false;
                        laughEvent.Fire();
                        Sprite.animateOnce(time);
                        DelayedAction.playSoundAfterDelay("SnS.DuskspireLaugh_NoLoop", 500, currentLocation, Position);
                        Game1.playSound("SnS.DuskspireLaugh_NoLoop");
                        noMovementTime.Value = 67 * 70;
                    }
                    else if (dist < Sprite.SpriteWidth * Game1.pixelZoom / 2 - 75)
                    {
                        swingEvent.Fire(farmer.Position.X > Position.X);
                        Sprite.animateOnce(time);
                        noMovementTime.Value = 11 * 70;
                    }
                    else
                    {
                        Vector2 vel = Utility.getVelocityTowardPlayer(GetBoundingBox().Center, Speed, farmer);
                        //Log.Debug("vel: " + vel);
                        Rectangle bb = GetBoundingBox();
                        bb.X += (int)vel.X;
                        bb.Y += (int)vel.Y;
                        if (true || !location.isCollidingPosition(bb, Game1.viewport, this))
                        {
                            Position += vel;
                        }
                    }
                }
            }
        }

        if (noMovementTime.Value <= 0 && WalkTimer <= 0)
        {
            Sprite.StopAnimation();
            Vector2 posDiff = Position - lastPos;
            int dir;
            if (stunTime.Value > 0)
                dir = Game1.down;
            else if (Math.Abs(posDiff.Y) > Math.Abs(posDiff.X))
            {
                if (posDiff.Y < 0)
                    dir = Game1.up;
                else
                    dir = Game1.down;
            }
            else
            {
                if (posDiff.X < 0)
                    dir = Game1.left;
                else
                    dir = Game1.right;
            }

            switch (dir)
            {
                case Game1.up: Sprite.CurrentFrame = Up[currWalkIndex]; break;
                case Game1.down: Sprite.CurrentFrame = Down[currWalkIndex]; break;
                case Game1.left: Sprite.CurrentFrame = Left[currWalkIndex]; break;
                case Game1.right: Sprite.CurrentFrame = Right[currWalkIndex]; break;
            }
            currWalkIndex++;
            if (currWalkIndex >= 4)
                currWalkIndex = 0;
            WalkTimer = 75;
        }
        else
        {
            WalkTimer -= time.ElapsedGameTime.Milliseconds;
        }

        DeathPos = Position - new Vector2(4, 4) * 64;
        lastPos = Position;
    }

    protected override void sharedDeathAnimation()
    {
        if (Name != "Duskspire Remnant")
            Game1.playSound("SnS.DuskspireDeath");
        var pos = DeathPos;
        TemporaryAnimatedSprite DuskspireDeath = new(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth-death.png").BaseName, new(0, 0, 96, 96), 75, 84, 0, pos, false, false) { scale = 4 };
        TemporaryAnimatedSprite DuskspireHeart = new(ModSnS.instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth-death.png").BaseName, new(0, 2016, 96, 96), 75, 16, 5, pos, false, false) { scale = 4 };
        currentLocation.TemporarySprites.Add(DuskspireDeath);
        DelayedAction.addTemporarySpriteAfterDelay(DuskspireHeart, Game1.getLocationFromName("EastScarp_DuskspireLair"), 6300);
        DelayedAction.functionAfterDelay(() => Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_DuskspireHeart"), Position, Game1.down, currentLocation), 12300);
        currentLocation.modData.Add("DN.SnS_DuskspireFaught", "true");
    }

    private void LaughEvent_onEvent()
    {
        List<FarmerSprite.AnimationFrame> frames = [];
        List<FarmerSprite.AnimationFrame> actualFrames = [];
        for (int i = 0; i < 33; ++i)
        {
            frames.Add(new(52 + i, 70));
        }

        actualFrames.AddRange(frames);
        frames.Reverse();
        actualFrames.AddRange(frames);

        Sprite.setCurrentAnimation(actualFrames);
    }

    private void SwingEvent_onEvent(bool arg)
    {
        List<FarmerSprite.AnimationFrame> frames = [];
        for (int i = 0; i < 10; ++i)
        {
            frames.Add(new(40 + i, 70));
        }
        Sprite.setCurrentAnimation(frames);
        foreach (Farmer f in Game1.getAllFarmers().Where(f => f.currentLocation == currentLocation))
            if (f.GetBoundingBox().Intersects(GetBoundingBox()))
                f.takeDamage(DamageToFarmer, false, this);
        flippedSwing = arg;
    }

    public override void draw(SpriteBatch b)
    {
        //b.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, GetBoundingBox()), Color.Red);
        Sprite.draw(b, Game1.GlobalToLocal(Position - new Vector2(Sprite.SpriteWidth * Game1.pixelZoom / 2, Sprite.SpriteHeight * Game1.pixelZoom)), Position.Y / 10000f, 0, 0, Color.White, (Sprite.CurrentFrame >= 40 && Sprite.CurrentFrame <= 50) && flippedSwing, Game1.pixelZoom);
    }

    [HarmonyPatch(typeof(HungryFrogCompanion), nameof(HungryFrogCompanion.tongueReachedMonster))]
    public static class FrogTrinketDoesntAutoKillDuskspire
    {
        public static bool Prefix(HungryFrogCompanion __instance, Monster m)
        {
            if (m is DuskspireMonster)
            {
                m.currentLocation.damageMonster(m.GetBoundingBox(), 80, 120, false, __instance.Owner);
                __instance.OnOwnerWarp();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Bat), "updateAnimation")]
    public static class DropletDoesntSparkle
    {
        public static bool Prefix(Bat __instance, GameTime time)
        {
            if (__instance.Name != "Stygium Droplet") return true;

            if (__instance.focusedOnFarmers || __instance.withinPlayerThreshold(6) || __instance.seenPlayer.Value || __instance.magmaSprite.Value)
            {
                __instance.Sprite.Animate(time, 0, 4, 80f);
                if (__instance.cursedDoll.Value)
                {
                    __instance.shakeTimer -= time.ElapsedGameTime.Milliseconds;
                    if (__instance.shakeTimer < 0)
                    {
                        __instance.shakeTimer = 50;
                        if (__instance.magmaSprite.Value)
                        {
                            __instance.shakeTimer = (__instance.lungeTimer > 0) ? 50 : 100;
                        }
                    }
                    IReflectedProperty<List<Vector2>> previousPositions = ModSnS.instance.Helper.Reflection.GetProperty<List<Vector2>>(__instance, "previousPositions", true);
                    List<Vector2> pP = previousPositions.GetValue();
                    pP.Add(__instance.Position);
                    if (pP.Count > 8)
                    {
                        pP.RemoveAt(0);
                    }
                    previousPositions.SetValue(pP);
                }
            }
            else
            {
                __instance.Sprite.currentFrame = 4;
                __instance.Halt();
            }
            ModSnS.instance.Helper.Reflection.GetMethod(__instance, "resetAnimationSpeed", true).Invoke();
            return false;
        }
    }

    [HarmonyPatch(typeof(Bat), nameof(Bat.takeDamage))]
    public static class DropletDoesntSparkle2
    {
        public static bool Prefix(Bat __instance, int damage, int xTrajectory, int yTrajectory, double addedPrecision, Farmer who, ref int __result)
        {
            if (__instance.Name != "Stygium Droplet") return true;

            if (__instance.Age == 789)
            {
                __result = -1;
                return false;
            }
            __instance.lungeVelocity = Vector2.Zero;
            if (__instance.lungeTimer > 0)
            {
                __instance.nextLunge = __instance.lungeFrequency;
                __instance.lungeTimer = 0;
            }
            else if (__instance.nextLunge < 1000)
            {
                __instance.nextLunge = 1000;
            }
            int actualDamage = Math.Max(1, damage - __instance.resilience.Value);
            __instance.seenPlayer.Value = true;
            if (Game1.random.NextDouble() < __instance.missChance.Value - __instance.missChance.Value * addedPrecision)
            {
                actualDamage = -1;
            }
            else
            {
                __instance.Health -= actualDamage;
                __instance.setTrajectory(xTrajectory / 3, yTrajectory / 3);
                __instance.wasHitCounter.Value = 500;
                if (__instance.magmaSprite.Value)
                {
                    __instance.currentLocation.playSound("magma_sprite_hit");
                }
                else
                {
                    __instance.currentLocation.playSound("hitEnemy");
                }
                if (__instance.Health <= 0)
                {
                    __instance.deathAnimation();
                    if (!__instance.magmaSprite.Value)
                    {
                        Game1.Multiplayer.broadcastSprites(__instance.currentLocation, new TemporaryAnimatedSprite(44, __instance.Position, Color.DarkMagenta, 10));
                    }
                    if (__instance.cursedDoll.Value)
                    {
                        Vector2 pixelPosition = __instance.Position;
                        if (__instance.magmaSprite.Value)
                        {
                            __instance.currentLocation.playSound("magma_sprite_die");
                        }
                        else
                        {
                            __instance.currentLocation.playSound("rockGolemHit");
                        }
                        if (__instance.hauntedSkull.Value)
                        {
                            Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(__instance.Sprite.textureName.Value, new Rectangle(0, 32, 16, 16), 2000f, 1, 9999, pixelPosition, flicker: false, flipped: false, 1f, 0.02f, Color.White, 4f, 0f, 0f, 0f)
                            {
                                motion = new Vector2((float)xTrajectory / 4f, Game1.random.Next(-12, -7)),
                                acceleration = new Vector2(0f, 0.4f),
                                rotationChange = (float)Game1.random.Next(-200, 200) / 1000f
                            });
                        }
                        else if (who != null && !__instance.magmaSprite.Value)
                        {
                            Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(388, 1894, 24, 22), 40f, 6, 9999, pixelPosition, flicker: false, flipped: true, 1f, 0f, Color.Black * 0.67f, 4f, 0f, 0f, 0f)
                            {
                                motion = new Vector2(8f, -4f)
                            });
                        }
                    }
                    else
                    {
                        __instance.currentLocation.playSound("batScreech");
                    }
                }
            }
            __instance.addedSpeed = Game1.random.Next(-1, 1);
            __result = actualDamage;
            return false;
        }
    }

}
