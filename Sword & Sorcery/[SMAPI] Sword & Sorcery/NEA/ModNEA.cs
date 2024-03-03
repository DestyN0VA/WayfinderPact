using SpaceShared.APIs;
using HarmonyLib;
//using NeverEndingAdventure.HarmonyPatches;
using NeverEndingAdventure.Utils;
using SpaceCore.Events;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using StardewValley.Buffs;
using System.Linq;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;

namespace NeverEndingAdventure
{

    /// <inheritdoc/>
    internal sealed class ModNEA
    {
        /********* Fields *********/

        /// <summary>The Wear More Rings mod API.</summary>
        private IMoreRingsApi WearMoreRings;

        /********* Accessors *********/

        /// <summary>The mod instance.</summary>
        public static Mod Instance { get; private set; }

        /// <summary>The item ID for the Ring of Wide Nets.</summary>
        public static string MateoGuildBadge => "swordandsorcery.adventureguildbadge";
        public static string MateoStygiumPendant => "swordandsorcery.styguimpendant";

        public bool HasWearMoreRings => this.WearMoreRings != null;


        public IMonitor Monitor;
        public IManifest ModManifest;
        public IModHelper Helper;
        public ModNEA(IMonitor monitor, IManifest manifest, IModHelper helper)
        {
            Monitor = monitor;
            ModManifest = manifest;
            Helper = helper;
        }

        /// <inheritdoc/>
        public void Entry(Harmony harmony)
        {
            // this is the entry point to your mod.
            // SMAPI calls this method when it loads your mod.
            Log.Monitor = this.Monitor; // this binds SMAPI's logging to the Log class so you can use it.

            // applying harmony patches.
            //Harmony harmony = new(this.ModManifest.UniqueID);
            //UntimedSO.ApplyPatch(harmony, Helper);

            // Subscribing to SpaceEvents
            SpaceEvents.OnEventFinished += SpaceEvents_OnEventFinished;
            SpaceEvents.BeforeGiftGiven += SpaceEvents_BeforeGiftGiven;

            harmony.Patch(AccessTools.Method(typeof(Ring), nameof(Ring.CanCombine)),
                postfix: new HarmonyMethod(typeof(ModNEA), nameof(PreventRingCombining)));

        }
        private static void PreventRingCombining(Ring __instance, Ring ring, ref bool __result)
        {
            if (__instance.ItemId == MateoGuildBadge || ring.ItemId == MateoGuildBadge ||
                 __instance.ItemId == MateoStygiumPendant || ring.ItemId == MateoStygiumPendant)
            {
                __result = false;
            }
        }

        private void SpaceEvents_BeforeGiftGiven(object sender, EventArgsBeforeReceiveObject e)
        {
            if (e.Gift.Name == "Wilted Bouquet" && e.Npc.Name == "Mateo")
            {
                BreakupMateo(Game1.player);
            }
        }
        private static void BreakupMateo(Farmer who)
        {
            //Removes romance mail flag if player has it, preventing the player from dating them again.
            if (who.mailReceived.Remove("MateoRomanticFlag"))
                who.mailReceived.Add("MateoHeartbreak");
        }
        private void SpaceEvents_OnEventFinished(object sender, EventArgs e)
        {
            if (Game1.CurrentEvent.id.Equals(12369014))
            {
                DatingMateo(Game1.player);
            }
        }
        private static void DatingMateo(Farmer who)
        {
            Friendship friendship = who.friendshipData["Mateo"];
            friendship.Status = FriendshipStatus.Dating;
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
        }
    }

    [HarmonyPatch(typeof(Ring), nameof(Ring.drawTooltip))]
    public static class NeaRingTooltipPatch1
    {
        public static void Postfix(Ring __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
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
        }
    }

    [HarmonyPatch(typeof(Ring), nameof(Ring.getExtraSpaceNeededForTooltipSpecialIcons))]
    public static class NeaRingTooltipPatch2
    {
        private static void Postfix(Ring __instance, ref Point __result, SpriteFont font, int startingHeight)
        {
            if (__instance.GetsEffectOfRing(ModNEA.MateoGuildBadge))
            {
                Point dimensions = new Point(0, startingHeight);
                int extra_rows_needed = 3;

                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }
            if (__instance.GetsEffectOfRing(ModNEA.MateoStygiumPendant))
            {
                Point dimensions = new Point(0, startingHeight);
                int extra_rows_needed = 4;

                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }
        }
    }
}
