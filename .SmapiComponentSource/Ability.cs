using HarmonyLib;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
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
        public static void Prefix(FarmerRenderer __instance, SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, Color overrideColor, float rotation, float scale, Farmer who)
        {
            var ext = who.GetFarmerExtData();

            if ( who.buffs.AppliedBuffIds.Contains( "shadowstep" ) || ( ext.isGhost.Value && ext.currRenderingMirror == 0 ) )
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

                    transparent = true;
                    position = oldPos;// + new Vector2(0, -Game1.tileSize);
                    position += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);
                    //ext.currRenderingMirror = 1;
                    __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);

                    if (ext.mirrorImages.Value > 1)
                    {
                        rad += MathF.PI * 2 / 3;
                        transparent = true;
                        position = oldPos;// + new Vector2(-Game1.tileSize, Game1.tileSize);
                        position += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);
                        ext.currRenderingMirror = 2;
                        __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);

                        if (ext.mirrorImages.Value > 2)
                        {
                            rad += MathF.PI * 2 / 3;
                            transparent = true;
                            position = oldPos;// + new Vector2(Game1.tileSize, Game1.tileSize);
                            position += new Vector2(MathF.Cos(rad) * Game1.tileSize, MathF.Sin(rad) * Game1.tileSize);
                            ext.currRenderingMirror = 3;
                            __instance.draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, scale, who);
                        }
                    }
                }
                ext.currRenderingMirror = 0;
                position = oldPos;
                transparent = oldTransparent;
            }
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
            if (FarmerRendererShadowstepPatch.transparent)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color) })]
    public static class SpriteBatchTransparencyChanger2
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchTransparencyChanger3
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchTransparencyChanger4
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent)
                color *= 0.5f;
        }
    }
    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color) })]
    public static class SpriteBatchTransparencyChanger5
    {
        public static void Prefix(ref Color color)
        {
            if (FarmerRendererShadowstepPatch.transparent)
                color *= 0.5f;
        }
    }
}
