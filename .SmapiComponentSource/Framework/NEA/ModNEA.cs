using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Objects;
using SwordAndSorcerySMAPI.Framework.NEA.Utils;
using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SwordAndSorcerySMAPI.Framework.NEA
#pragma warning restore IDE0130 // Namespace does not match folder structure
{

    /// <inheritdoc/>
    internal sealed class ModNEA(IMonitor monitor, IManifest manifest, IModHelper helper, Harmony harmony)
    {
        /********* Fields *********/

        /********* Accessors *********/

        /// <summary>The mod instance.</summary>
        public static Mod Instance { get; private set; }

        /// <summary>The item ID for the Ring of Wide Nets.</summary>
        public static string MateoGuildBadge => "DN.SnS_adventureguildbadge";
        public static string MateoStygiumPendant => "DN.SnS_styguimpendant";
        public static string SenPressedCrocus => "DN.SnS_pressedcrocus";

        public IManifest ModManifest = manifest;
        public IModHelper Helper = helper;

        /// <inheritdoc/>
        public void Entry()
        {
            // this is the entry point to your mod.
            // SMAPI calls this method when it loads your mod.
            Log.Monitor = monitor; // this binds SMAPI's logging to the Log class so you can use it.

            // applying harmony patches.
            //Harmony harmony = new(this.ModManifest.UniqueID);
            //UntimedSO.ApplyPatch(harmony, Helper);

            harmony.Patch(AccessTools.Method(typeof(Ring), nameof(Ring.CanCombine)),
                postfix: new HarmonyMethod(typeof(ModNEA), nameof(PreventRingCombining)));

        }

        private static void PreventRingCombining(Ring __instance, Ring ring, ref bool __result)
        {
            if (__instance.ItemId == MateoGuildBadge || ring.ItemId == MateoGuildBadge ||
                 __instance.ItemId == MateoStygiumPendant || ring.ItemId == MateoStygiumPendant || __instance.ItemId == SenPressedCrocus || ring.ItemId == SenPressedCrocus)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Ring), nameof(Ring.AddEquipmentEffects))]
    public static class NeaRingEffectsPatch
    {
        public static void Postfix(Ring __instance, BuffEffects effects)
        {
            if (__instance.ItemId == ModNEA.MateoGuildBadge || __instance.ItemId == ModNEA.MateoStygiumPendant)
            {
                effects.MagneticRadius.Value += 128;
                effects.AttackMultiplier.Value += 0.1f;
                effects.Defense.Value += 3;
                if (__instance.ItemId == ModNEA.MateoStygiumPendant)
                    effects.LuckLevel.Value += 1;
            }
            if (__instance.ItemId == ModNEA.SenPressedCrocus)
            {
                effects.LuckLevel.Value += 1;
                effects.ForagingLevel.Value += 1;
            }
        }
    }

    [HarmonyPatch(typeof(Ring), nameof(Ring.drawTooltip))]
    public static class NeaRingTooltipPatch1
    {
        public static void Postfix(Ring __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha)
        {
            if (__instance.GetsEffectOfRing(ModNEA.MateoGuildBadge))
            {
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", 5 * __instance.GetEffectsOfRingMultiplier(ModNEA.MateoGuildBadge)), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(120, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, "+" + Game1.content.LoadString("Strings\\UI:ItemHover_Buff11", $"{__instance.GetEffectsOfRingMultiplier(ModNEA.MateoGuildBadge) * 10}%"), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(90, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, "+" + Game1.content.LoadString("Strings\\UI:ItemHover_Buff8", __instance.GetEffectsOfRingMultiplier(ModNEA.MateoGuildBadge)), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
            }
            if (__instance.GetsEffectOfRing(ModNEA.MateoStygiumPendant))
            {
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", 5 * __instance.GetEffectsOfRingMultiplier(ModNEA.MateoStygiumPendant)), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(120, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, "+" + Game1.content.LoadString("Strings\\UI:ItemHover_Buff11", $"{__instance.GetEffectsOfRingMultiplier(ModNEA.MateoStygiumPendant) * 10}%"), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(90, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, "+" + Game1.content.LoadString("Strings\\UI:ItemHover_Buff8", __instance.GetEffectsOfRingMultiplier(ModNEA.MateoStygiumPendant)), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                if (__instance.GetsEffectOfRing(ModNEA.MateoStygiumPendant))
                {
                    Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(50, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                    Utility.drawTextWithShadow(spriteBatch, "+" + Game1.content.LoadString("Strings\\UI:ItemHover_Buff4", __instance.GetEffectsOfRingMultiplier(ModNEA.MateoStygiumPendant)), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                    y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                }
            }
            if (__instance.GetsEffectOfRing(ModNEA.SenPressedCrocus))
            {
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(50, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, "+1 Luck", font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
                Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(60, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
                Utility.drawTextWithShadow(spriteBatch, "+1 Foraging", font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
                y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
            }
        }
    }

    [HarmonyPatch(typeof(Ring), nameof(Ring.getExtraSpaceNeededForTooltipSpecialIcons))]
    public static class NeaRingTooltipPatch2
    {
        private static void Postfix(Ring __instance, ref Point __result, SpriteFont font, int startingHeight)
        {
            if (__instance.GetsEffectOfRing(ModNEA.MateoGuildBadge))
            {
                Point dimensions = new(0, startingHeight);
                int extra_rows_needed = 3;

                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }
            if (__instance.GetsEffectOfRing(ModNEA.MateoStygiumPendant))
            {
                Point dimensions = new(0, startingHeight);
                int extra_rows_needed = 4;

                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }
            if (__instance.GetsEffectOfRing(ModNEA.SenPressedCrocus))
            {
                Point dimensions = new(0, startingHeight);
                int extra_rows_needed = 2;

                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }
        }
    }
}
