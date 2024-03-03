using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Menus;

namespace SwordAndSorcerySMAPI
{

    public static class Farmer_ShieldSlot
    {
        internal class Holder { public readonly NetRef<Item> Value = new(); }

        internal static ConditionalWeakTable< Farmer, Holder > values = new();

        public static void set_shieldSlot( this Farmer farmer, NetRef<Item> newVal )
        {
            // We don't actually want a setter for this one, since it should be readonly
            // Net types are weird
            // Or do we? Serialization
        }

        public static NetRef<Item> get_shieldSlot( this Farmer farmer )
        {
            var holder = values.GetOrCreateValue( farmer );
            return holder.Value;
        }
    }

    [HarmonyPatch(typeof(Farmer), "initNetFields")]
    public static class FarmerInjectNetFieldsPatch
    {
        public static void Postfix(Farmer __instance)
        {
            __instance.NetFields.AddField(__instance.get_shieldSlot(), "shieldSlot");
        }
    }


    [HarmonyPatch(typeof(InventoryPage), MethodType.Constructor, new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
    public static class InventoryPageShieldConstructorPatch
    {
        public static void Postfix(InventoryPage __instance)
        {
            __instance.equipmentIcons.Add(
                new ClickableComponent(
                    new Rectangle(__instance.xPositionOnScreen + 48 + 208 - 80 - (ModSnS.instance.Helper.ModRegistry.IsLoaded("bcmpinc.WearMoreRings") ? 208 : -144),
                        __instance.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 4 + 256 - 12 + 64,
                        64, 64),
                    "Shield")
                {
                    myID = 123450102, // TODO: Replace with Nexus mod id prefix
                    leftNeighborID = 102,
                    fullyImmutable = true,
                });
        }
    }

    [HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.performHoverAction))]
    public static class InventoryPageShieldHoverPatch
    {
        public static void Postfix(InventoryPage __instance, int x, int y, ref Item ___hoveredItem, ref string ___hoverText, ref string ___hoverTitle)
        {
            var shieldSlot = __instance.equipmentIcons.FirstOrDefault(cc => cc.myID == 123450102);

            if (shieldSlot is null)
                return;

            if (shieldSlot.containsPoint(x, y) && Game1.player.get_shieldSlot().Value != null)
            {
                var shieldItem = Game1.player.get_shieldSlot().Value;
                ___hoveredItem = shieldItem;
                ___hoverText = shieldItem.getDescription();
                ___hoverTitle = shieldItem.DisplayName;
            }
        }
    }
    [HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.receiveLeftClick))]
    public static class InventoryPageShieldLeftClickPatch
    {
        public static bool Prefix(InventoryPage __instance, int x, int y)
        {
            var shieldSlot = __instance.equipmentIcons.FirstOrDefault(cc => cc.myID == 123450102);

            if (shieldSlot is null)
                return true;
            if (shieldSlot.containsPoint(x, y))
            {
                var shieldItem = Game1.player.get_shieldSlot();
                if (Game1.player.CursorSlotItem == null || Game1.player.CursorSlotItem.IsShield())
                {
                    Item tmp = ModSnS.instance.Helper.Reflection.GetMethod(__instance, "takeHeldItem").Invoke<Item>();
                    Item held = shieldItem.Value;
                    if (held != null)
                        held.onUnequip(Game1.player);
                    held = Utility.PerformSpecialItemGrabReplacement(held);
                    ModSnS.instance.Helper.Reflection.GetMethod(__instance, "setHeldItem").Invoke(held);
                    shieldItem.Value = tmp;

                    if (shieldItem.Value != null)
                    {
                        shieldItem.Value.onEquip(Game1.player);
                        Game1.playSound("crit");
                    }
                    else if (Game1.player.CursorSlotItem != null)
                        Game1.playSound("dwop");
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.draw))]
    public static class InventoryPageShieldDrawPatch
    {
        public static void Postfix(InventoryPage __instance, SpriteBatch b, Item ___hoveredItem)
        {
            if (___hoveredItem != null && ___hoveredItem != Game1.player.get_shieldSlot().Value)
                return;

            var shieldSlot = __instance.equipmentIcons.FirstOrDefault(cc => cc.myID == 123450102);

            if (shieldSlot is null)
                return;

            if (Game1.player.get_shieldSlot().Value != null)
            {
                b.Draw(Game1.menuTexture, shieldSlot.bounds, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
                Game1.player.get_shieldSlot().Value.drawInMenu(b, new Vector2(shieldSlot.bounds.X, shieldSlot.bounds.Y), shieldSlot.scale, 1f, 0.866f, StackDrawType.Hide);
            }
            else
            {
                b.Draw(ModSnS.ShieldSlotBackground, shieldSlot.bounds, null, Color.White);
            }
        }
    }
}
