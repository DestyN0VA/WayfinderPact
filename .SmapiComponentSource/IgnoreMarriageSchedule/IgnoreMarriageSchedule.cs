using HarmonyLib;
using SpaceCore.Patches;
using StardewValley;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static SwordAndSorcerySMAPI.IgnoreMarriageSchedule.IgnoreMarriageScheduleAssetManager;

namespace SwordAndSorcerySMAPI.IgnoreMarriageSchedule
{
    public class IgnoreMarriageScheduleUtil
    {
        public static List<CodeInstruction> ReplaceIsMarried(List<CodeInstruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (i + 2 <= instructions.Count &&
                    instructions[i].opcode == OpCodes.Ldarg_0 &&
                    instructions[i + 1].opcode == OpCodes.Call && instructions[i + 1].operand as MethodInfo == AccessTools.Method(typeof(NPC), nameof(NPC.isMarried)))
                {
                    instructions[i + 1].operand = AccessTools.Method(typeof(IgnoreMarriageScheduleUtil), nameof(IsMarried));
                }
            }
            return instructions;
        }

        public static bool IsMarried(NPC npc)
        {
            if (IgnoreMarriageAsset.TryGetValue(npc.Name, out var data) && data.IgnoreMarriageSchedule && (string.IsNullOrEmpty(data.CanVisitFarmToday) || !GameStateQuery.CheckConditions(data.CanVisitFarmToday)))
                return false;
            return npc.isMarried();
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.TryLoadSchedule), [])]
    public static class IgnoreMarriageSchedule
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return IgnoreMarriageScheduleUtil.ReplaceIsMarried([.. instructions]);
        }

        static void Postfix(NPC __instance)
        {
            if (!IgnoreMarriageScheduleUtil.IsMarried(__instance))
                __instance.reloadDefaultLocation();
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.OnDayStarted))]
    static class IgnoreMarriageScheduleDefaultMap
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return IgnoreMarriageScheduleUtil.ReplaceIsMarried([.. instructions]);
        }
    }

    [HarmonyPatch(typeof(NPC), "loadCurrentDialogue")]
    static class IgnoreMarriageDialogue
    {
#pragma warning disable IDE0051
        static void Prefix(NPC __instance, ref string __state)
        {
            if (__instance.Name != "Hector")
                return;

            if (Game1.player.spouse == __instance.Name && IgnoreMarriageAsset.TryGetValue(__instance.Name, out var data) && data.IgnoreMarriageDialogue)
            {
                __state = Game1.player.spouse;
                Game1.player.spouse = null;
            }
        }

        static void Postfix(string __state)
        {
            if (__state != null)
                Game1.player.spouse = __state;
        }
#pragma warning restore IDE0051
    }
}
