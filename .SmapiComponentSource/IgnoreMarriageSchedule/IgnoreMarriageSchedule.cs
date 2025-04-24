using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static SwordAndSorcerySMAPI.IgnoreMarriageSchedule.IgnoreMarriageScheduleAssetManager;
using static SwordAndSorcerySMAPI.IgnoreMarriageSchedule.IgnoreMarriageScheduleAssetModel;
using static SwordAndSorcerySMAPI.IgnoreMarriageSchedule.IgnoreMarriageScheduleUtil;

namespace SwordAndSorcerySMAPI.IgnoreMarriageSchedule
{
    public class IgnoreMarriageScheduleUtil
    {
        public static void DayStarted(object sender, DayStartedEventArgs e)
        {
            foreach (var ignore in IgnoreMarriageAsset.Keys)
            {
                NPC npc = Game1.getCharacterFromName(ignore);
                if (!npc.isMarried())
                    continue;

                FarmVisit Visit = GetFarmVisitToday(IgnoreMarriageAsset[npc.Name].FarmVisits, npc.getSpouse());
                if (Visit != FarmVisit.None)
                {
                    Vector2 Pos = Vector2.Zero;
                    Point Pos2 = Point.Zero;
                    FarmHouse house = Game1.RequireLocation<FarmHouse>(npc.getSpouse().homeLocation.Value);
                    switch (Visit)
                    {
                        case FarmVisit.Porch:
                            Pos2 = house.getPorchStandingSpot();
                            Pos = Utility.PointToVector2(Pos2) * 64;
                            break;
                        case FarmVisit.SpousePatio:
                            npc.setUpForOutdoorPatioActivity();
                            Pos = npc.Position;
                            Pos2 = npc.TilePoint;
                            break;
                        case FarmVisit.SpouseRoom:
                            Pos2 = house.GetSpouseRoomSpot();
                            Pos = Utility.PointToVector2(Pos2) * 64;
                            break;
                        case FarmVisit.Farmhouse:
                            Pos2 = house.getSpouseBedSpot(npc.Name);
                            Pos = Utility.PointToVector2(Pos2) * 64;
                            break;
                    }
                    Game1.warpCharacter(npc, Visit == FarmVisit.Porch || Visit == FarmVisit.SpousePatio ? "Farm" : npc.getSpouse().homeLocation.Value, Pos);
                    npc.setTilePosition(Pos2);
                }
            }
        }

        public static bool IgnoresMarriage(NPC npc, bool ignoreVisit = false)
        {
            if (IgnoreMarriageAsset.TryGetValue(npc.Name, out var data) && data.IgnoreMarriageSchedule)
                if (!ignoreVisit && GetFarmVisitToday(data.FarmVisits, npc.getSpouse()) != FarmVisit.None)
                    return false;
                else
                    return true;
            return false;
        }

        public static FarmVisit GetFarmVisitToday(FarmVisitsModel data, Farmer Spouse)
        {
            List<FarmVisit> VisitsToday = [];

            TryAddFarmVisit(data.Porch, FarmVisit.Porch, ref VisitsToday, Spouse);
            TryAddFarmVisit(data.SpousePatio, FarmVisit.SpousePatio, ref VisitsToday, Spouse);
            TryAddFarmVisit(data.SpouseRoom, FarmVisit.SpouseRoom, ref VisitsToday, Spouse);
            TryAddFarmVisit(data.Farmhouse, FarmVisit.Farmhouse, ref VisitsToday, Spouse);

            if (VisitsToday.Count == 0)
                return FarmVisit.None;

            if (data.PreferredOrder != null)
            {
                var Order = GetPreferredOrderList(data.PreferredOrder);

                foreach (var Visit in Order)
                {
                    if (VisitsToday.Contains(Visit))
                        return Visit;
                }
            }
            
            return VisitsToday[Game1.random.Next(VisitsToday.Count)];
        }

        public static void TryAddFarmVisit(string Condition, FarmVisit FarmVisit, ref List<FarmVisit> list, Farmer spouse)
        {
            if (!string.IsNullOrEmpty(Condition) && GameStateQuery.CheckConditions(Condition, player: spouse, random: Utility.CreateRandom(Game1.uniqueIDForThisGame.GetHashCode(), Game1.seasonIndex, Game1.dayOfMonth, Game1.year)))
                list.Add(FarmVisit);
        }

        public static List<FarmVisit> GetPreferredOrderList(string rawOrder)
        {
            List<FarmVisit> FinalOrder = [];
            List<string> Order = [.. rawOrder.Split(',')];
            ;
            foreach (string Visit in Order)
            {
                string fVisit = Visit.Trim();
                if (fVisit.EqualsIgnoreCase("Porch"))
                    FinalOrder.Add(FarmVisit.Porch);
                else if (fVisit.EqualsIgnoreCase("SpousePatio"))
                    FinalOrder.Add(FarmVisit.SpousePatio);
                else if (fVisit.EqualsIgnoreCase("SpouseRoom"))
                    FinalOrder.Add(FarmVisit.SpouseRoom);
                else if (fVisit.EqualsIgnoreCase("Farmhouse"))
                    FinalOrder.Add(FarmVisit.Farmhouse);
            }
            return FinalOrder;
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.TryLoadSchedule), [])]
    public static class IgnoreMarriageSchedule
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insns)
        {
            List<CodeInstruction> instructions = [.. insns];
            for (int i = 0; i < instructions.Count; i++)
            {
                if (i + 2 <= instructions.Count &&
                    instructions[i].opcode == OpCodes.Ldarg_0 &&
                    instructions[i + 1].opcode == OpCodes.Call && instructions[i + 1].operand as MethodInfo == AccessTools.Method(typeof(NPC), nameof(NPC.isMarried)))
                {
                    instructions[i + 1].operand = AccessTools.Method(typeof(IgnoreMarriageSchedule), nameof(IsMarried));
                }
            }
            return instructions;
        }

        public static bool IsMarried(NPC npc)
        {
            if (IgnoreMarriageAsset.TryGetValue(npc.Name, out var data) && data.IgnoreMarriageSchedule && GetFarmVisitToday(data.FarmVisits, npc.getSpouse()) == FarmVisit.None)
                return false;
            return npc.isMarried();
        }

        static void Postfix(NPC __instance)
        {
            if (__instance.isMarried() && !IgnoresMarriage(__instance, true))
            {
                FarmVisit Visit = GetFarmVisitToday(IgnoreMarriageAsset[__instance.Name].FarmVisits, __instance.getSpouse());
                if (Visit == FarmVisit.None)
                    __instance.reloadDefaultLocation();
            }
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.OnDayStarted))]
    static class IgnoreMarriageScheduleDefaultMap
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insns)
        {
            List<CodeInstruction> instructions = [.. insns];
            for (int i = 0; i < instructions.Count; i++)
            {
                if (i + 2 <= instructions.Count &&
                    instructions[i].opcode == OpCodes.Ldarg_0 &&
                    instructions[i + 1].opcode == OpCodes.Call && instructions[i + 1].operand as MethodInfo == AccessTools.Method(typeof(NPC), nameof(NPC.isMarried)))
                {
                    instructions[i + 1].operand = AccessTools.Method(typeof(IgnoreMarriageScheduleDefaultMap), nameof(IsMarried));
                }
            }
            return instructions;
        }

        public static bool IsMarried(NPC npc)
        {
            if (IgnoreMarriageAsset.TryGetValue(npc.Name, out var data) && data.IgnoreMarriageSchedule)
                return false;
            return npc.isMarried();
        }
    }

    [HarmonyPatch(typeof(NPC), "loadCurrentDialogue")]
    static class IgnoreMarriageDialogue
    {
#pragma warning disable IDE0051
        static void Prefix(NPC __instance, ref string __state)
        {
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
