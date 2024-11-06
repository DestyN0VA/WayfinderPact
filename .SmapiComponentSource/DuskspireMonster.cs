using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI;
public class DuskspireMonster(Vector2 pos, string name = "Duskspire Behemoth") : Monster(name, pos)
{
    private readonly NetEvent0 laughEvent = new();
    private readonly NetEvent1Field<bool, NetBool> swingEvent = new();
    private readonly NetFloat noMovementTime = [];

    private int prevFrame = 0;
    private Vector2 lastPos = Vector2.Zero;
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

                    Game1.playSound("SnS.DuskspireLaugh_NoLoop");
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
                noMovementTime.Value -= (float)time.ElapsedGameTime.TotalMilliseconds;

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
                        laughEvent.Fire();
                        noMovementTime.Value = 66 * 75;
                    }
                    else if (dist < Sprite.SpriteWidth * Game1.pixelZoom / 2 - 75)
                    {
                        swingEvent.Fire(farmer.Position.X > Position.X);
                        noMovementTime.Value = 100 * 10;
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

        if (noMovementTime.Value <= 0)
        {
            Vector2 posDiff = Position - lastPos;
            int dir;
            if (Math.Abs(posDiff.Y) > Math.Abs(posDiff.X))
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
                case Game1.up: Sprite.AnimateUp(time, -50); break;
                case Game1.down: Sprite.AnimateDown(time, -50); break;
                case Game1.left: Sprite.AnimateLeft(time, -50); break;
                case Game1.right: Sprite.AnimateRight(time, -50); break;
            }
        }
        else
        {
            //Sprite.animateOnce(time);
        }

        ModSnS.DuskspireDeathPos = Tile - new Vector2(4, 4);
        lastPos = Position;
    }

    private void LaughEvent_onEvent()
    {
        List<FarmerSprite.AnimationFrame> frames = [];
        List<FarmerSprite.AnimationFrame> actualFrames = [];
        for (int i = 0; i < 8 * 4 + 1; ++i)
        {
            frames.Add(new(52 + i, 75));
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
            frames.Add(new(40 + i, 75));
        }
        Sprite.setCurrentAnimation(frames);
        flippedSwing = arg;
    }

    public override void draw(SpriteBatch b)
    {
        //b.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, GetBoundingBox()), Color.Red);
        Sprite.draw(b, Game1.GlobalToLocal(Position - new Vector2(Sprite.SpriteWidth * Game1.pixelZoom / 2, Sprite.SpriteHeight * Game1.pixelZoom)), Position.Y / 10000f, 0, 0, Color.White, (Sprite.CurrentFrame >= 40 && Sprite.CurrentFrame <= 50) && flippedSwing, Game1.pixelZoom);
    }
}
