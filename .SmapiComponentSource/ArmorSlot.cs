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
}
