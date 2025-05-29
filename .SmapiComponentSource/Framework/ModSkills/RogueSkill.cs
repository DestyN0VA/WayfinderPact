using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Skill = SpaceCore.Skills.Skill;

namespace SwordAndSorcerySMAPI.Framework.ModSkills
{

    public class RogueSkill : Skill
    {
        public static GenericProfession ProfessionArmorRecovery { get; set; }
        public static GenericProfession ProfessionBowSecondShot { get; set; }
        public static GenericProfession ProfessionCrafting { get; set; }
        public static GenericProfession ProfessionArmorCap { get; set; }
        public static GenericProfession ProfessionShadowStep { get; set; }
        public static GenericProfession ProfessionHuntersMark { get; set; }

        public RogueSkill()
            : base("DestyNova.SwordAndSorcery.Rogue")
        {
            Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png");
            SkillsPageIcon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png");

            ExperienceCurve = [100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000];

            ExperienceBarColor = new Microsoft.Xna.Framework.Color(252, 121, 27);

            // Level 5
            ProfessionArmorRecovery = new GenericProfession(skill: this, id: "ArmorRecovery", name: I18n.RogueSkill_Profession_ArmorRecovery_Name, description: I18n.RogueSkill_Profession_ArmorRecovery_Description)
            {
                Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/ArtificerSpecialist.png")
            };
            Professions.Add(ProfessionArmorRecovery);

            ProfessionBowSecondShot = new GenericProfession(skill: this, id: "BowSecondShot", name: I18n.RogueSkill_Profession_RogueishArchetype_Name, description: I18n.RogueSkill_Profession_RogueishArchetype_Description)
            {
                Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/RogueishArchetype.png")
            };
            Professions.Add(ProfessionBowSecondShot);

            ProfessionsForLevels.Add(new ProfessionPair(5, ProfessionArmorRecovery, ProfessionBowSecondShot));

            // Level 10 - track A
            ProfessionCrafting = new GenericProfession(skill: this, id: "Crafting", name: I18n.RogueSkill_Profession_FlashOfGenius_Name, description: I18n.RogueSkill_Profession_FlashOfGenius_Description)
            {
                Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/FlashOfGenius.png")
            };
            Professions.Add(ProfessionCrafting);

            ProfessionArmorCap = new GenericProfession(skill: this, id: "ArmorCap", name: I18n.RogueSkill_Profession_ArmorProficiency_Name, description: I18n.RogueSkill_Profession_ArmorProficiency_Description)
            {
                Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/ArmorProficiency.png")
            };
            Professions.Add(ProfessionArmorCap);

            ProfessionsForLevels.Add(new ProfessionPair(10, ProfessionCrafting, ProfessionArmorCap, ProfessionArmorRecovery));

            // Level 10 - track B
            ProfessionShadowStep = new GenericProfession(skill: this, id: "ShadowStep", name: I18n.Ability_Shadowstep_Name, description: I18n.Ability_Shadowstep_Description)
            {
                Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/Shadowstep.png")
            };
            Professions.Add(ProfessionShadowStep);

            ProfessionHuntersMark = new GenericProfession(skill: this, id: "HuntersMark", name: I18n.RogueSkill_Profession_HuntersMark_Name, description: I18n.RogueSkill_Profession_HuntersMark_Description)
            {
                Icon = ModSnS.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/HuntersMark.png")
            };
            Professions.Add(ProfessionHuntersMark);

            ProfessionsForLevels.Add(new ProfessionPair(10, ProfessionShadowStep, ProfessionHuntersMark, ProfessionBowSecondShot));
        }

        public override string GetName()
        {
            return I18n.RogueSkill_Name();
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            var RogueSkill = new RogueSkill();
            string[][] recipes =
                [
                    null,
                    ["DN.SnS_ClothArmor", "DN.SnS_Bow", "DN.SnS_Arrow"],
                    null,
                    ["DN.SnS_CopperArmor", "DN.SnS_FirestormArrow", "DN.SnS_IcicleArrow"],
                    ["DN.SnS_IronArmor"],
                    null,
                    ["DN.SnS_WindwakerArrow"],
                    ["DN.SnS_GoldArmor"],
                    ["DN.SnS_IridiumArmor", "DN.SnS_RicochetArrow"],
                    ["DN.SnS_RadioactiveArmor", "DN.SnS_StygiumArmor", "DN.SnS_ElysiumArmor", "DN.SnS_LightbringerArrow"],
                    null,
                ];
            for (int level = 1; level <= Game1.player.GetCustomSkillLevel(RogueSkill); ++level)
            {
                if (recipes[level] != null)
                {
                    Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                    if (recipes[level].Length == 2)
                    {
                        if (level == 1)
                        {
                            Game1.player.craftingRecipes.TryAdd(recipes[level][1], 0);
                        }
                        else
                        {
                            Game1.player.cookingRecipes.TryAdd(recipes[level][1], 0);
                        }
                    }
                }
            }
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);

            if (level > 10) return; // Walk of Life

            Game1.player.maxHealth += 3;

            string[][] craftingRecipes =
            [
                [],
                ["DN.SnS_ClothArmor", "DN.SnS_Bow", "DN.SnS_Arrow"], //1
                [], //2
                ["DN.SnS_CopperArmor", "DN.SnS_FirestormArrow", "DN.SnS_IcicleArrow"], //3
                ["DN.SnS_IronArmor"], //4
                [], //5
                ["DN.SnS_WindwakerArrow"], //6
                ["DN.SnS_GoldArmor"], //7
                ["DN.SnS_IridiumArmor", "DN.SnS_RicochetArrow"], //8
                ["DN.SnS_RadioactiveArmor", "DN.SnS_StygiumArmor", "DN.SnS_ElysiumArmor", "DN.SnS_LightbringerArrow"], //9
                [], //10
            ];

            foreach (var recipe in craftingRecipes[level])
            {
                if (!Game1.player.knowsRecipe(recipe))
                    Game1.player.craftingRecipes.Add(recipe, 0);
            }
        }


        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> ret = [];

            if (level > 10) return ret;

            if (level % 5 != 0)
                ret.Add(I18n.Level_Health(3) + "\n");

            switch (level)
            {
                case 1:
                    ret.Add(I18n.RogueSkill_Unlock_1());
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ClothArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_BowIsBeingStupid());
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_Arrow", false).DisplayName));
                    break;
                case 2:
                    ret.Add(I18n.RogueSkill_Unlock_2().Replace('^', '\n'));
                    break;
                case 3:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_CopperArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_FirestormArrow", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_IcicleArrow", false).DisplayName));
                    break;
                case 4:
                    ret.Add(I18n.RogueSkill_Unlock_4().Replace('^', '\n'));
                    ret.Add("\n" + I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_IronArmor", false).DisplayName));
                    break;
                case 6:
                    ret.Add(I18n.RogueSkill_Unlock_6() + "\n");
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_WindwakerArrow", false).DisplayName));
                    break;
                case 7:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_GoldArmor", false).DisplayName));
                    break;
                case 8:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_IridiumArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_RicochetArrow", false).DisplayName));
                    break;
                case 9:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_RadioactiveArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_StygiumArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ElysiumArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_LightbringerArrow", false).DisplayName));
                    break;
            }

            return ret;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return I18n.Level_Health(3 * level);
        }
        public override bool ShouldShowOnSkillsPage => Game1.player.eventsSeen.Contains("SnS.Ch1.Mateo.18");
    }

    [HarmonyPatch(typeof(LevelUpMenu), nameof(LevelUpMenu.RevalidateHealth))]
    public static class LevelUpMenuRevalidateHealthPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            var ret = new List<CodeInstruction>();

            int emhIndex = ModSnS.SpaceCore.GetLocalIndexForMethod(original, "expected_max_health")[0];

            bool inserted = false;
            foreach (var insn in insns)
            {
                if (insn.opcode == OpCodes.Ldfld && (FieldInfo)insn.operand == AccessTools.Field(typeof(Farmer), nameof(Farmer.maxHealth)))
                {
                    if (!inserted)
                    {
                        ret.InsertRange(ret.Count - 1,
                            [
                                new CodeInstruction(OpCodes.Ldloc, emhIndex),
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LevelUpMenuRevalidateHealthPatch), nameof(GetExtraHealth) ) ),
                                new CodeInstruction(OpCodes.Add),
                                new CodeInstruction(OpCodes.Stloc, emhIndex)
                            ]);
                        inserted = true;
                    }
                }

                ret.Add(insn);
            }

            return ret;
        }

        public static int GetExtraHealth(Farmer farmer)
        {
            return farmer.GetCustomSkillLevel(ModSnS.RogueSkill) * 3;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.gainExperience))]
    public static class FarmerArtificerExpInterceptPatch
    {
        public static void Postfix(Farmer __instance, int which, int howMuch)
        {
            if (!__instance.eventsSeen.Contains("SnS.Ch1.Mateo.18")) // TODO: Change event
                return;
            if (which != Farmer.combatSkill && which != Farmer.miningSkill)
                return;

            var data = __instance.GetFarmerExtData();
            float exp = data.expRemainderRogue.Value + howMuch / 2f;
            __instance.AddCustomSkillExperience(ModSnS.RogueSkill, (int)MathF.Truncate(exp));
            data.expRemainderRogue.Value = exp - MathF.Truncate(exp);
        }
    }
}
