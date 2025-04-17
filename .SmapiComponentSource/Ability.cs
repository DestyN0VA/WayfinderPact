using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;

namespace SwordAndSorcerySMAPI
{
    public class Ability
    {
        public string Id { get; } 
        public Func<string> Name { get; set; }
        public Func<string> Description { get; set; }
        public string TexturePath { get; set; }
        public int SpriteIndex { get; set; }
        public string KnownCondition { get; set; }
        public Func<bool>? KnownCondition2 { get; set; } = null;
        public Func<string> UnlockHint { get; set; } = () => "...";
        public bool HiddenIfLocked { get; set; } = false;
        public Func<int> ManaCost { get; set; } = () => 10;
        public Func<bool> CanUse { get; set; } = () => true;
        public Func<bool>? CanUseForAdventureBar { get; set;}
        public Action Function { get; set; }

        public static Dictionary<string, Ability> Abilities { get; } = [];

        public Ability(string id)
        {
            Id = id;
            CanUseForAdventureBar ??= CanUse;
        }
    }

    [HarmonyBefore("PeacefulEnd.FashionSense")]
    [HarmonyPatch(typeof(FarmerRenderer), nameof(FarmerRenderer.draw), [typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame), typeof(int), typeof(Rectangle), typeof(Vector2), typeof(Vector2), typeof(float), typeof(int), typeof(Color), typeof(float), typeof(float), typeof(Farmer)])]
    public static class FarmerRendererShadowstepAndTransformingPatch
    {
        internal static bool transparent = false;

        internal static int Index = 0;
        internal static Rectangle[][] SourceRects = [
            [new(32, 64, 32, 32), new(64, 64, 32, 32), new(96, 64, 32, 32), new(128, 64, 32, 32), new(160, 64, 32, 32), new(192, 64, 32, 32)],
            [new(32, 32, 32, 32), new(64, 32, 32, 32), new(96, 32, 32, 32), new(128, 32, 32, 32), new(160, 32, 32, 32), new(192, 32, 32, 32)],
            [new(32, 0, 32, 32), new(64, 0, 32, 32), new(96, 0, 32, 32), new(128, 0, 32, 32), new(160, 0, 32, 32), new(192, 0, 32, 32)],
            [new(32, 96, 32, 32), new(64, 96, 32, 32), new(96, 96, 32, 32), new(128, 96, 32, 32), new(160, 96, 32, 32), new(192, 96, 32, 32)]
            ];

        [HarmonyBefore("PeacefulEnd.FashionSense")]
        public static bool Prefix(FarmerRenderer __instance, SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, ref Color overrideColor, float rotation, float scale, Farmer who)
        {
            var ext = who.GetFarmerExtData();

            if (ext.noMovementTimer != 0)
            {
                Index = 0;
            }

            Texture2D tex = ModCoT.FormTexs[ext.form.Value][0];
            Texture2D eTex = ModCoT.FormTexs[ext.form.Value][1];
            Rectangle frame = default;
            SpriteEffects fx = SpriteEffects.None;

            if (ext.stasisTimer.Value > 0)
                overrideColor = Color.Black;

            if (who.buffs.AppliedBuffIds.Contains("shadowstep") || (ext.isGhost.Value && ext.currRenderingMirror == 0))
            {
                transparent = true;
            }

            if (ext.currRenderingMirror == 0)
            {
                ext.currRenderingMirror = 1;

                Vector2 oldPos = position;
                bool oldTransparent = transparent;

                if (ext.isGhost.Value)
                {
                    transparent = false;
                    position = Game1.GlobalToLocal(ext.ghostOrigPosition.Value);
                    __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);
                }

                if (ext.mirrorImages.Value > 0)
                {
                    float rad = (float)-Game1.currentGameTime.TotalGameTime.TotalSeconds / 3 * 2;

                    int f1;
                    if (ext.IsResting)
                    {
                        if (ext.noMovementTimer > 3 && ext.noMovementTimer < (ext.form.Value != 2 ? 3.375 : 3.750))
                        {
                            f1 = (int)((ext.noMovementTimer - 3) / 0.125f);
                            frame = new Rectangle(f1 * 32, 128, 32, 32);
                        }
                        else
                        {
                            if (ext.form.Value != 2)
                                f1 = (int)(ext.noMovementTimer % 6000 / 240) == 0 ? 4 : 3;
                            else
                                f1 = 6;
                            frame = new Rectangle(f1 * 32, 128, 32, 32);
                        }
                    }
                    else
                    {
                        switch (who.FacingDirection)
                        {
                            case Game1.down: frame = new(0, 0, 32, 32); break;
                            case Game1.right: frame = new(0, 32, 32, 32); break;
                            case Game1.up: frame = new(0, 64, 32, 32); break;
                            case Game1.left: frame = new(0, 96, 32, 32); break;
                        }

                        if (ext.noMovementTimer == 0)
                        {
                            frame = SourceRects[who.FacingDirection][Index];
                        }
                    }

                    transparent = true;
                    position = oldPos;// + new Vector2(0, -Game1.tileSize);
                    position += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize) * (ext.transformed.Value ? 1.5f : 1f);
                    //ext.currRenderingMirror = 1;
                    if (!who.GetFarmerExtData().transformed.Value)
                        __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);
                    else
                    {
                        b.Draw(tex, position + new Vector2(0, 24), frame, overrideColor, rotation, origin + new Vector2(8, 0), 4, fx, layerDepth);
                        b.Draw(eTex, position + new Vector2(0, 24), frame, who.newEyeColor.Value, rotation, origin + new Vector2(8, 0), 4, fx, layerDepth + 0.001f);
                    }

                    if (ext.mirrorImages.Value > 1)
                    {
                        rad += MathF.PI * 2 / 3;
                        transparent = true;
                        position = oldPos;// + new Vector2(-Game1.tileSize, Game1.tileSize);
                        position += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize) * (ext.transformed.Value ? 1.5f : 1f);
                        ext.currRenderingMirror = 2;

                        if (!who.GetFarmerExtData().transformed.Value)
                            __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);
                        else
                        {
                            b.Draw(tex, position + new Vector2(0, 24), frame, overrideColor, rotation, origin + new Vector2(8, 0), 4, fx, layerDepth);
                            b.Draw(eTex, position + new Vector2(0, 24), frame, who.newEyeColor.Value, rotation, origin + new Vector2(8, 0), 4, fx, layerDepth + 0.001f);
                        }

                        if (ext.mirrorImages.Value > 2)
                        {
                            rad += MathF.PI * 2 / 3;
                            transparent = true;
                            position = oldPos;// + new Vector2(Game1.tileSize, Game1.tileSize);
                            position += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize) * (ext.transformed.Value ? 1.5f : 1f);
                            ext.currRenderingMirror = 3;
                            if (!who.GetFarmerExtData().transformed.Value)
                                __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);
                            else
                            {
                                b.Draw(tex, position + new Vector2(0, 24), frame, overrideColor, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth);
                                b.Draw(eTex, position + new Vector2(0, 24), frame, who.newEyeColor.Value, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth + 0.001f);
                            }
                        }
                    }
                }
                ext.currRenderingMirror = 0;
                position = oldPos;
                transparent = oldTransparent;
            }

            if (Game1.CurrentEvent != null && Game1.currentMinigame is not FinalePhase1Minigame)
            {
                if (Game1.player.currentLocation.currentEvent != null && !Game1.player.currentLocation.currentEvent.isFestival)
                    return true;
            }
            var data = who.GetFarmerExtData();
            if (!data.transformed.Value)
                return true;

            int[] offsetHatX = [6, 13, 6, -4, -1];
            int[][] offsetHatY =
                [
                    [8, 8, 9, 9, 8, 8, 8], // up
                    [7, 7, 8, 8, 7, 7, 7], // right
                    [15, 15, 15, 16, 15, 15, 15], // down
                    [7, 7, 8, 8, 7, 7, 7], // left
                    [7, 8, 10, 12, 12, 10, 8], // rest
                ];
            int f = 0;
            if (data.IsResting)
            {
                if (data.noMovementTimer > 3 && data.noMovementTimer < (data.form.Value != 2 ? 3.375 : 3.750))
                {
                    f = (int)((data.noMovementTimer - 3) / 0.125f);
                    frame = new Rectangle(f * 32, 128, 32, 32);
                }
                else
                {
                    if (data.form.Value != 2)
                        f = 3;
                    else
                        f = 6;
                    frame = new Rectangle(f * 32, 128, 32, 32);
                }
            }
            else
            {
                switch (who.FacingDirection)
                {
                    case Game1.down: frame = new(0, 0, 32, 32); break;
                    case Game1.right: frame = new(0, 32, 32, 32); break;
                    case Game1.up: frame = new(0, 64, 32, 32); break;
                    case Game1.left: frame = new(0, 96, 32, 32); break;
                }

                if (data.noMovementTimer == 0)
                {
                    if (data.MovementTimer >= 100)
                    {
                        Index++;
                        data.MovementTimer = 0;
                    }
                    if (Index > 5)
                        Index = 0;
                    frame = SourceRects[who.FacingDirection][Index];
                }
            }
            b.Draw(tex, position + new Vector2(0, 24), frame, overrideColor, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth);
            b.Draw(eTex, position + new Vector2(0, 24), frame, who.newEyeColor.Value, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth + 0.001f);

            if (who.hat.Value != null)
            {
                var hatData = ItemRegistry.GetData(who.hat.Value.QualifiedItemId);
                var hatRect = new Rectangle(20 * (int)hatData.SpriteIndex % hatData.GetTexture().Width, 20 * (int)hatData.SpriteIndex / hatData.GetTexture().Width * 20 * 4, 20, 20);

                if (!data.IsResting)
                {
                    switch (who.FacingDirection)
                    {
                        case Game1.down: break;
                        case Game1.right: hatRect.Offset(0, 20); break;
                        case Game1.up: hatRect.Offset(0, 60); break;
                        case Game1.left: hatRect.Offset(0, 40); break;
                    }
                }
                else hatRect.Offset(0, 40);

                int offsetInd = who.FacingDirection;
                if (data.IsResting) offsetInd = 4;

                Vector2 offset = new(offsetHatX[offsetInd], offsetHatY[offsetInd][f]);
                Vector2 p = position + new Vector2(0, 24) + offset * Game1.pixelZoom + new Vector2(0, -10) * Game1.pixelZoom;
                var Hats = DataLoader.Hats(Game1.content);
                Hats.TryGetValue(who.hat.Value.ItemId, out var hat);
                Texture2D HatTex = FarmerRenderer.hatsTexture;
                if (hat.Split('/').Length >= 8 && Game1.content.DoesAssetExist<Texture2D>(hat.Split('/')[7]))
                    HatTex = Game1.content.Load<Texture2D>(hat.Split('/')[7]);
                b.Draw(HatTex, p, hatRect, Color.White, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth + 0.002f);
            }
            return false;
        }
        public static void Postfix()
        {
            transparent = false;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), [typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(SpriteEffects), typeof(float)])]
    public static class SpriteBatchTransparencyChanger1
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepAndTransformingPatch.transparent || ModTOP.DrawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), [typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color)])]
    public static class SpriteBatchTransparencyChanger2
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepAndTransformingPatch.transparent || ModTOP.DrawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), [typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float)])]
    public static class SpriteBatchTransparencyChanger3
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepAndTransformingPatch.transparent || ModTOP.DrawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), [typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float)])]
    public static class SpriteBatchTransparencyChanger4
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepAndTransformingPatch.transparent || ModTOP.DrawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), [typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color)])]
    public static class SpriteBatchTransparencyChanger5
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepAndTransformingPatch.transparent || ModTOP.DrawingBanished)
                color *= 0.5f;
        }
    }
}
