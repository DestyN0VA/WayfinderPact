using HarmonyLib;
using StardewValley.Objects.Trinkets;
using StardewValley;
using StardewValley.Extensions;
using System.Linq;
using Microsoft.Xna.Framework;
using System;
using Object = StardewValley.Object;
using StardewModdingAPI.Events;
using StardewValley.Tools;
using StardewValley.Menus;

namespace SwordAndSorcerySMAPI
{

    public class KeychainsAndTrinkets
    {

        public static void DayStarted(object? sender, DayStartedEventArgs e)
        {
            while (Game1.player.trinketItems.Count <= Farmer.MaximumTrinkets)
                Game1.player.trinketItems.Add(null);

            foreach (Item i in Game1.player.Items.Where(o => o is MeleeWeapon or Slingshot && o.QualifiedItemId.ContainsIgnoreCase("(W)DN.SnS_longlivetheking")))
            {
                Tool LLTK = i as Tool;
                if (LLTK.attachments[1] is Trinket t)
                {
                    HandleTrinketEquipUnequip(t, null);
                }
            }
        }

        public static void DayEnding(object? sender, DayEndingEventArgs e)
        {
            Game1.player.trinketItems.RemoveWhere(t => t.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false);
        }

        public static void TryAttach(Tool LLTK, Object held, out Object attached, out Object OnHand, out int? Slot)
        {
            attached = null;
            OnHand = held;
            Slot = null;

            if (held == null)
            {
                for (int i = 0; i < LLTK.AttachmentSlotsCount; i++)
                {
                    if (LLTK.attachments[i] != null)
                    {
                        OnHand = LLTK.attachments[i];
                        Slot = i;
                        Game1.playSound("dwop");
                        return;
                    }
                }
                return;
            }
            for (int i = 0; i < LLTK.AttachmentSlotsCount; i++)
            {
                if (!CanThisBeAttached(held, i)) continue;
                if (LLTK.attachments[i] == null)
                {
                    attached = held;
                    OnHand = null;
                    Slot = i;
                    Game1.playSound("button1");
                    return;
                }
                else if (LLTK.attachments[i].canStackWith(held))
                {
                    int NewStack = GetLeftOverStack(LLTK.attachments[i], held, out int Stack); ;
                    attached = LLTK.attachments[i];
                    attached.Stack = NewStack;
                    OnHand = held;
                    OnHand.Stack = Stack;
                    if (OnHand.Stack <= 0)
                        OnHand = null;
                    Game1.playSound("button1");
                    Slot = i;
                    return;
                }
                else
                {
                    Slot = i;
                    attached = held;
                    OnHand = LLTK.attachments[i];
                    Game1.playSound("button1");
                }
            }
        }

        public static bool CanThisBeAttached(Object o, int slot)
        {
            if (o == null) return true;
            if (slot == 0)
                return o.HasContextTag("bullet_item");
            else
                return o.HasContextTag("keychain_item") || (o is Trinket t && (t.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false));
        }

        public static int GetLeftOverStack(Object toAddTo, Object AddedFrom, out int LeftOverStack)
        {
            int UnregulatedNewStack = toAddTo.Stack + AddedFrom.Stack;
            int NewStack = Math.Clamp(UnregulatedNewStack, 0, toAddTo.maximumStackSize());

            int Diff = UnregulatedNewStack - NewStack;
            LeftOverStack = Math.Clamp(AddedFrom.Stack - Diff, 0, AddedFrom.maximumStackSize());
            return NewStack;
        }

        public static void HandleTrinketEquipUnequip(Object? New, Object? Old)
        {
            if (New != null && New is Trinket NewTrinket)
                Game1.player.trinketItems.Add(NewTrinket);
            if (Old != null && Old is Trinket OldTrinket)
                Game1.player.trinketItems.Remove(OldTrinket);
        }
    }

    [HarmonyPatch(typeof(Object), nameof(Object.getCategoryName))]
    public static class ObjectKeychainCategoryName
    {
        public static void Postfix(Object __instance, ref string __result)
        {
            if (__instance.HasContextTag("keychain_item"))
            {
                __result = I18n.KeychainCategory();
            }
        }
    }

    [HarmonyPatch(typeof(Object), nameof(Object.getCategoryColor))]
    public static class ObjectKeychainCategoryColor
    {
        public static void Postfix(Object __instance, ref Color __result)
        {
            if (__instance.HasContextTag("keychain_item"))
            {
                __result = Color.DarkSlateGray;
            }
        }
    }

    [HarmonyPatch(typeof(Trinket), nameof(Trinket.getCategoryName))]
    public static class TrinketKeychainCategoryName
    {
        public static void Postfix(Trinket __instance, ref string __result)
        {
            if (__instance != null && (__instance.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))
            {
                __result = I18n.KeychainCategory();
            }
        }
    }

    [HarmonyPatch(typeof(Trinket), nameof(Trinket.getCategoryColor))]
    public static class TrinketKeychainCategoryColor
    {
        public static void Postfix(Trinket __instance, ref Color __result)
        {
            if (__instance != null && (__instance?.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))
            {
                __result = Color.DarkSlateGray;
            }
        }
    }

    [HarmonyPatch(typeof(Object), nameof(Object.canBeTrashed))]
    public static class TrinketNoTrashing
    {
        public static void Postfix(Object __instance, ref bool __result)
        {
            if (__instance != null && __instance is Trinket t && (t.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Item), nameof(Item.CanBeLostOnDeath))]
    public static class TrinketNoLosingOnDeath
    {
        public static void Postfix(Item __instance, ref bool __result)
        {
            if (__instance != null && __instance is Trinket t && (t.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Trinket), nameof(Trinket.canBeGivenAsGift))]
    public static class TrinketNoGifting
    {
        public static void Postfix(Trinket __instance, ref bool __result)
        {
            if (__instance != null && (__instance.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))
            {
                __result = false;
            }
        }
    }
}
