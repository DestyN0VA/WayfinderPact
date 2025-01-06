using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore.Dungeons;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace SwordAndSorcerySMAPI
{
    // Legacy data, now uses SpaceCore
    public static class Farmer_ArmorSlot
    {
        internal class Holder { public readonly NetRef<Item> Value = new(); }

        internal static ConditionalWeakTable< Farmer, Holder > values = new();

        public static void set_armorSlot(this Farmer farmer, NetRef<Item> newVal)
        {
            // We don't actually want a setter for this one, since it should be readonly
            // Net types are weird
            // Or do we? Serialization
        }

        public static NetRef<Item> get_armorSlot( this Farmer farmer )
        {
            var holder = values.GetOrCreateValue( farmer );
            return holder.Value;
        }

        public static Item GetArmorItem(this Farmer farmer)
        {
            return ModSnS.sc.GetItemInEquipmentSlot(farmer, $"{ModSnS.instance.ModManifest.UniqueID}_Armor") ?? null;
        }

        public static void SetArmorItem(this Farmer farmer, Item item)
        {
            ModSnS.sc.SetItemInEquipmentSlot(farmer, $"{ModSnS.instance.ModManifest.UniqueID}_Armor", item);
        }
    }

    [HarmonyPatch(typeof(Farmer), "initNetFields")]
    public static class FarmerInjectNetFieldsPatch
    {
        public static void Postfix(Farmer __instance)
        {
            __instance.NetFields.AddField(__instance.get_armorSlot());
        }
    }

    [HarmonyPatch(typeof(Game1), "drawHUD")]
    public static class Game1DrawHealthBarAndArmorPointsInDD
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insns)
        {
            CodeMatcher match = new(insns);
            bool next = false;
            object operand = null;
            foreach (var ins in insns)
            {
                if (next)
                {
                    operand = ins.operand;
                    break;
                }
                if (ins.opcode == OpCodes.Isinst)
                    next = true;
            }
            if (operand != null)
                match.MatchEndForward([
                    new(OpCodes.Brtrue_S, operand)
                    ])
                .Advance(1)
                .Insert([
                    new(OpCodes.Call, AccessTools.Method(typeof(Game1DrawHealthBarAndArmorPointsInDD), nameof(ShouldShowHealth))),
                    new(OpCodes.Brtrue_S, operand)
                    ]);

            return match.Instructions();
        }

        public static bool ShouldShowHealth()
        {
            if (Game1.currentLocation.NameOrUniqueName == "EastScarp_DuskspireLair" || Game1.currentLocation.GetDungeonExtData().spaceCoreDungeonId.Value != null)
                return true;
            else
                return false;
        }
    }

    /*

    [HarmonyPatch(typeof(InventoryPage), MethodType.Constructor, new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
    public static class InventoryPageArmorConstructorPatch
    {
        public static void Postfix(InventoryPage __instance)
        {
            int add = 0;
            var nextTo = __instance.equipmentIcons.FirstOrDefault(cc => cc.myID == InventoryPage.region_trinkets);
            if (nextTo == null)
            {
                nextTo = __instance.equipmentIcons.FirstOrDefault(cc => cc.myID == InventoryPage.region_hat);
                add = (ModSnS.instance.Helper.ModRegistry.IsLoaded("bcmpinc.WearMoreRings") ? 0 : 0);
            }
            if (nextTo == null)
            {
                ModSnS.instance.Monitor.Log("Failed to find place to put armor slot?", StardewModdingAPI.LogLevel.Warn);
                return;
            }

            __instance.equipmentIcons.Add(
                new ClickableComponent( new Rectangle(nextTo.bounds.Right + 16 + 2 + add, nextTo.bounds.Top, 64, 64), "Armor")
                {
                    myID = 123450102, // TODO: Replace with Nexus mod id prefix
                    upNeighborID = Game1.player.MaxItems - (nextTo.myID == InventoryPage.region_trinkets ? 7 : 8),
                    leftNeighborID = nextTo.myID,
                    rightNeighborID = InventoryPage.region_trashCan,
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

            if (shieldSlot.containsPoint(x, y) && Game1.player.get_armorSlot().Value != null)
            {
                var shieldItem = Game1.player.get_armorSlot().Value;
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
                var shieldItem = Game1.player.get_armorSlot();
                if (Game1.player.CursorSlotItem == null || Game1.player.CursorSlotItem.IsArmorItem())
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
            if (___hoveredItem != null && ___hoveredItem != Game1.player.get_armorSlot().Value)
                return;

            var shieldSlot = __instance.equipmentIcons.FirstOrDefault(cc => cc.myID == 123450102);

            if (shieldSlot is null)
                return;

            if (Game1.player.get_armorSlot().Value != null)
            {
                b.Draw(Game1.menuTexture, shieldSlot.bounds, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
                Game1.player.get_armorSlot().Value.drawInMenu(b, new Vector2(shieldSlot.bounds.X, shieldSlot.bounds.Y), shieldSlot.scale, 1f, 0.866f, StackDrawType.Hide);
            }
            else
            {
                b.Draw(ModSnS.ArmorSlotBackground, shieldSlot.bounds, null, Color.White);
            }
        }
    }
    */
}
