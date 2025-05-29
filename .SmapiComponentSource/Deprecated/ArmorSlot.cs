using HarmonyLib;
using Netcode;
using SpaceCore.Dungeons;
using StardewValley;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SwordAndSorcerySMAPI.Deprecated
{
    // Legacy data used for migration, now uses SpaceCore
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
}
