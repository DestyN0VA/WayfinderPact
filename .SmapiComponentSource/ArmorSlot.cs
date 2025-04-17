using HarmonyLib;
using Netcode;
using SpaceCore.Dungeons;
using StardewValley;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SwordAndSorcerySMAPI
{
    // Legacy data, now uses SpaceCore
    public static class Farmer_ArmorSlot
    {
        internal class Holder { public readonly NetRef<Item> Value = []; }

        internal static ConditionalWeakTable<Farmer, Holder> values = [];

        public static void set_armorSlot(this Farmer farmer, NetRef<Item> newVal)
        {
            // We don't actually want a setter for this one, since it should be readonly
            // Net types are weird
            // Or do we? Serialization
        }

        public static NetRef<Item> get_armorSlot(this Farmer farmer)
        {
            var holder = values.GetOrCreateValue(farmer);
            return holder.Value;
        }

        public static Item GetArmorItem(this Farmer farmer)
        {
            return ModSnS.SpaceCore.GetItemInEquipmentSlot(farmer, $"{ModSnS.Instance.ModManifest.UniqueID}_Armor") ?? null;
        }

        public static void SetArmorItem(this Farmer farmer, Item item)
        {
            ModSnS.SpaceCore.SetItemInEquipmentSlot(farmer, $"{ModSnS.Instance.ModManifest.UniqueID}_Armor", item);
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
            return Game1.currentLocation.NameOrUniqueName == "EastScarp_DuskspireLair" || Game1.currentLocation.GetDungeonExtData().spaceCoreDungeonId.Value != null;
        }
    }
}
