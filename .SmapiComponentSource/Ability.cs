using CircleOfThornsSMAPI;
using HarmonyLib;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static StardewValley.FarmerSprite;

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
        public Func<string> UnlockHint { get; set; } = () => "...";
        public bool HiddenIfLocked { get; set; } = false;
        public Func<int> ManaCost { get; set; } = () => 10;
        public Func<bool> CanUse { get; set; } = () => true;
        public Action Function { get; set; }

        public Ability(string id) { Id = id; }

        public static Dictionary<string, Ability> Abilities { get; } = new();
    }


    [HarmonyPatch(typeof(FarmerRenderer), nameof(FarmerRenderer.draw), new Type[] { typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame ), typeof(int ), typeof(Rectangle ), typeof(Vector2 ), typeof(Vector2 ), typeof(float ), typeof(int ), typeof(Color ), typeof(float ), typeof(float ), typeof(Farmer) })]
    public static class FarmerRendererShadowstepPatch
    {
        internal static bool transparent = false;
        public static bool Prefix(FarmerRenderer __instance, SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, ref Color overrideColor, float rotation, float scale, Farmer who)
        {
            var ext = who.GetFarmerExtData();

            Texture2D tex = ModCoT.formTexs[ext.form.Value][0];
            Texture2D eTex = ModCoT.formTexs[ext.form.Value][1];
            Rectangle frame = default(Rectangle);
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

                    int f1 = 0;
                    if (ext.isResting)
                    {
                        if (ext.noMovementTimer > 3 && ext.noMovementTimer < 3.375)
                        {
                            f1 = (int)((ext.noMovementTimer - 3) / 0.125f);
                            frame = new Rectangle(f1 * 32, 128, 32, 32);
                        }
                        else
                        {
                            f1 = 3;
                            frame = new Rectangle(3 * 32, 128, 32, 32);
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
                            f1 = Game1.currentGameTime.TotalGameTime.Milliseconds % 700 / 100;
                            frame = new(frame.X + 32 * f1, frame.Y, frame.Width, frame.Height);
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

            int[] offsetHatX = new int[5] { 6, 13, 6, -4, -1 };
            int[][] offsetHatY = new int[5][]
                {
                    new int[ 7 ] { 8, 8, 9, 9, 8, 8, 8 }, // up
                    new int[ 7 ] { 7, 7, 8, 8, 7, 7, 7 }, // right
                    new int[ 7 ] { 15, 15, 15, 16, 15, 15, 15 }, // down
                    new int[ 7 ] { 7, 7, 8, 8, 7, 7, 7 }, // left
                    new int[ 7 ] { 7, 8, 10, 12, 12, 10, 8 }, // rest
                };
            int f = 0;
            if (data.isResting)
            {
                if (data.noMovementTimer > 3 && data.noMovementTimer < 3.375)
                {
                    f = (int)((data.noMovementTimer - 3) / 0.125f);
                    frame = new Rectangle(f * 32, 128, 32, 32);
                }
                else
                {
                    f = 3;
                    frame = new Rectangle(3 * 32, 128, 32, 32);
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
                    f = Game1.currentGameTime.TotalGameTime.Milliseconds % 700 / 100;
                    frame = new(frame.X + 32 * f, frame.Y, frame.Width, frame.Height);
                }
            }
            b.Draw(tex, position + new Vector2(0, 24), frame, overrideColor, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth);
            b.Draw(eTex, position + new Vector2(0, 24), frame, who.newEyeColor.Value, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth + 0.001f);

            if (who.hat.Value != null)
            {
                var hatData = ItemRegistry.GetData(who.hat.Value.QualifiedItemId);
                var hatRect = new Rectangle(20 * (int)hatData.SpriteIndex % hatData.GetTexture().Width, 20 * (int)hatData.SpriteIndex / hatData.GetTexture().Width * 20 * 4, 20, 20);

                if (!data.isResting)
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
                if (data.isResting) offsetInd = 4;

                Vector2 offset = new(offsetHatX[offsetInd], offsetHatY[offsetInd][f]);
                Vector2 p = position + new Vector2(0, 24) + offset * Game1.pixelZoom + new Vector2(0, -10) * Game1.pixelZoom;
                b.Draw(FarmerRenderer.hatsTexture, p, hatRect, Color.White, rotation, origin + new Vector2(8, 0), Vector2.One * Game1.pixelZoom, fx, layerDepth + 0.002f);
            }
            return false;
        }
        public static void Postfix(Farmer who)
        {
            transparent = false;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchTransparencyChanger1
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent || ModTOP.drawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color) })]
    public static class SpriteBatchTransparencyChanger2
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent || ModTOP.drawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchTransparencyChanger3
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent || ModTOP.drawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchTransparencyChanger4
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent || ModTOP.drawingBanished)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color) })]
    public static class SpriteBatchTransparencyChanger5
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent || ModTOP.drawingBanished)
                color *= 0.5f;
        }
    }
}
