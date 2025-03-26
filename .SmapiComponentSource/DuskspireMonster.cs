using HarmonyLib;
using MageDelve.Mercenaries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using NeverEndingAdventure.Utils;
using StardewValley;
using StardewValley.Companions;
using StardewValley.GameData;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI;

//AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
public class DuskspireMonster(Vector2 pos, string name = "Duskspire Behemoth") : Monster(name, pos)
{
    private readonly NetEvent0 laughEvent = new();
    private readonly NetEvent1Field<bool, NetBool> swingEvent = new();
    private readonly NetFloat noMovementTime = [];

    private readonly int[] Down = [0, 1, 2, 3];
    private readonly int[] Right = [4, 5, 6, 7];
    private readonly int[] Up = [8, 9, 10, 11];
    private readonly int[] Left = [12, 13, 14, 15];
    private int WalkTimer = 0;
    private int currWalkIndex = 0;

    private int prevFrame = 0;
    private Vector2 lastPos = Vector2.Zero;
    private bool flippedSwing = false;
    private readonly NetBool doingLaugh = new(false);

    public DuskspireMonster() :
        this(new Vector2(18, 13) * Game1.tileSize)
    { }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(laughEvent);
        NetFields.AddField(swingEvent);
        NetFields.AddField(doingLaugh);
        NetFields.AddField(noMovementTime);

        laughEvent.onEvent += LaughEvent_onEvent;
        swingEvent.onEvent += SwingEvent_onEvent;
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        base.reloadSprite(onlyAppearance);

        Sprite = new AnimatedSprite(ModSnS.Instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth.png").BaseName, 0, 96, 96);
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
        if (noMovementTime.Value > 0)
            return;
        var farmer = findPlayer();
        if (farmer.currentLocation == location)
        {
            Vector2 vel = Utility.getVelocityTowardPlayer(GetBoundingBox().Center, Speed, farmer);
            //Log.Debug("vel: " + vel);
            Rectangle bb = GetBoundingBox();
            bb.X += (int)vel.X;
            bb.Y += (int)vel.Y;
            if (true || !location.isCollidingPosition(bb, Game1.viewport, this))
                Position += vel;
        }

        if (WalkTimer <= 0)
            AnimateMovement();
        else
            WalkTimer -= time.ElapsedGameTime.Milliseconds;
    }

    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);
        Sprite.SpriteWidth = 96;
        Sprite.SpriteHeight = 96;
        laughEvent.Poll();
        swingEvent.Poll();

        if (prevFrame != 61 && Sprite.CurrentFrame == 61)
        {
            if (!doingLaugh.Value)
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
            doingLaugh.Value = !doingLaugh.Value;
        }

        if (Game1.IsMasterGame)
        {
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
                if (farmer.currentLocation == location)
                {
                    float dist = Vector2.Distance(farmer.getStandingPosition(), getStandingPosition());

                    if (dist <= Sprite.SpriteWidth / 2 - 75)
                    {
                        swingEvent.Fire(farmer.Position.X > Position.X);
                        Sprite.animateOnce(time);
                    }
                    else if (Game1.random.NextDouble() < 1f / (5 * 60))
                    {
                        doingLaugh.Value = false;
                        laughEvent.Fire();
                        Sprite.animateOnce(time);
                        DelayedAction.playSoundAfterDelay("SnS.DuskspireLaugh_NoLoop", 500, currentLocation, Position);
                        Game1.playSound("SnS.DuskspireLaugh_NoLoop");
                    }
                }
            }
        }
        lastPos = Position;
        prevFrame = Sprite.CurrentFrame;
    }

    private void AnimateMovement()
    {
        if (Sprite.CurrentAnimation != null) return;

        Vector2 posDiff = Position - lastPos;
        int dir = 0;
        if (stunTime.Value <= 0)
        {
            if (Math.Abs(posDiff.Y) > Math.Abs(posDiff.X))
            {
                if (posDiff.Y < 0)
                    dir = FacingDirection = Game1.up;
                else
                    dir = FacingDirection = Game1.down;
            }
            else
            {
                if (posDiff.X < 0)
                    dir = FacingDirection = Game1.left;
                else
                    dir = FacingDirection = Game1.right;
            }
        }
        switch (dir)
        {
            case Game1.up: Sprite.CurrentFrame = Up[currWalkIndex]; break;
            case Game1.down: Sprite.CurrentFrame = Down[currWalkIndex]; break;
            case Game1.left: Sprite.CurrentFrame = Left[currWalkIndex]; break;
            case Game1.right: Sprite.CurrentFrame = Right[currWalkIndex]; break;
        }

        WalkTimer = stunTime.Value == 0 ? 75 : stunTime.Value;
        if (WalkTimer > 75)
            return;

        currWalkIndex++;
        if (currWalkIndex >= 4)
            currWalkIndex = 0;

    }

    protected override void localDeathAnimation()
    {
        var Pos = Position - new Vector2(Sprite.SpriteWidth * Game1.pixelZoom / 2, Sprite.SpriteHeight * Game1.pixelZoom);

        if (IsFinalBoss())
        {
            Game1.screenGlowOnce(Color.White, false);
            DelayedAction.playMusicAfterDelay("SnS.DuskspireDeath", 100);
            Game1.addMail("DuskspireDefeated", true, false);
            DelayedAction.functionAfterDelay(() =>
            {
                Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)DN.SnS_DuskspireHeart"));
                Game1.stopMusicTrack(MusicContext.ImportantSplitScreenMusic);
                Game1.player.GetCurrentMercenaries().Clear();
                var partnerInfos = Game1.content.Load<Dictionary<string, FinalePartnerInfo>>("DN.SnS/FinalePartners");

                FinalePartnerInfo partnerInfo = partnerInfos["default"];

                foreach (string key in partnerInfos.Keys)
                {
                    if (Game1.player.friendshipData.TryGetValue(key, out var data) && (data.IsDating() || data.IsRoommate()))
                    {
                        partnerInfo = partnerInfos[key];
                        break;
                    }
                }
                const string GuildmasterEventId = "SnS.Ch4.Victory.FarmerGuildmaster";
                Game1.PlayEvent(Game1.player.hasOrWillReceiveMail("FarmerGuildmasterBattle") ? GuildmasterEventId : partnerInfo.VictoryEventId, checkPreconditions: false, checkSeen: false);
                ModSnS.State.FinaleBoss = null;
            }, 12300);
            Game1.player.GetFarmerExtData().DoingFinale.Value = true;
        }
        else
            DelayedAction.functionAfterDelay(() =>
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)DN.SnS_DuskspireHeart"), Utility.PointToVector2(StandingPixel), Game1.up, currentLocation);
            }, 12300);

        DelayedAction.playSoundAfterDelay("SnS.DuskspireLaugh_NoLoop", 1000, currentLocation, Position, local: true);
        TemporaryAnimatedSprite Duskspire = new(ModSnS.Instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth-death.png").BaseName, new(0, 0, 96, 96), 75, 84, 0, Pos, false, false) { scale = 4 },
                                Heart = new(ModSnS.Instance.Helper.ModContent.GetInternalAssetName("assets/duskspire-behemoth-death.png").BaseName, new(0, 2016, 96, 96), 75, 16, 5, Pos, false, false) { scale = 4 };
        DelayedAction.addTemporarySpriteAfterDelay(Duskspire, currentLocation, 0);
        DelayedAction.addTemporarySpriteAfterDelay(Heart, currentLocation, 6300);

        if (!currentLocation.modData?.ContainsKey("DN.SnS_DuskspireFaught") ?? true)
            currentLocation.modData.Add("DN.SnS_DuskspireFaught", "true");
    }

    private bool IsFinalBoss() => Name == "Duskspire Behemoth";

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
        noMovementTime.Value = frames.Count * 70;
        DelayedAction.functionAfterDelay(() => { Sprite.ClearAnimation(); }, (int)noMovementTime.Value);
    }

    private void SwingEvent_onEvent(bool arg)
    {
        Log.Warn("Doing swing event");
        List<FarmerSprite.AnimationFrame> frames = [];
        for (int i = 0; i < 10; i++)
        {
            frames.Add(new(40 + i, 70));
        }
        Rectangle Dusktangle = GetBoundingBox();
        Dusktangle.Inflate(32, 0);
        foreach (Farmer f in Game1.getAllFarmers().Where(f => f.currentLocation == currentLocation))
            if (f.GetBoundingBox().Intersects(Dusktangle) && f.CanBeDamaged())
                f.takeDamage(DamageToFarmer, false, this);
        Log.Warn("setting animation");
        Sprite.setCurrentAnimation(frames);
        flippedSwing = arg;
        noMovementTime.Value = frames.Count * 70;
        DelayedAction.functionAfterDelay(() => { Sprite.ClearAnimation(); }, (int)noMovementTime.Value);
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
}
