using System;
using System.Collections.Generic;
using CircleOfThornsSMAPI;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.Menus;
using SwordAndSorcerySMAPI;

namespace SwordAndSorcerySMAPI
{
    public class RogueSkill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionArmorRecovery;
        public static GenericProfession ProfessionBowSecondShot;
        public static GenericProfession ProfessionCrafting;
        public static GenericProfession ProfessionArmorCap;
        public static GenericProfession ProfessionShadowStep;
        public static GenericProfession ProfessionHuntersMark;

        public RogueSkill()
            : base("DestyNova.SwordAndSorcery.Rogue")
        {
            this.Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png");
            this.SkillsPageIcon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png");

            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(252, 121, 27);

            // Level 5
            RogueSkill.ProfessionArmorRecovery = new GenericProfession(skill: this, id: "ArmorRecovery", name: I18n.RogueSkill_Profession_ArmorRecovery_Name, description: I18n.RogueSkill_Profession_ArmorRecovery_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png")
            };
            this.Professions.Add(RogueSkill.ProfessionArmorRecovery);

            RogueSkill.ProfessionBowSecondShot = new GenericProfession(skill: this, id: "BowSecondShot", name: I18n.RogueSkill_Profession_RogueishArchetype_Name, description: I18n.RogueSkill_Profession_RogueishArchetype_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png")
            };
            this.Professions.Add(RogueSkill.ProfessionBowSecondShot);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, RogueSkill.ProfessionArmorRecovery, RogueSkill.ProfessionBowSecondShot));

            // Level 10 - track A
            RogueSkill.ProfessionCrafting = new GenericProfession(skill: this, id: "Crafting", name: I18n.RogueSkill_Profession_FlashOfGenius_Name, description: I18n.RogueSkill_Profession_FlashOfGenius_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png")
            };
            this.Professions.Add(RogueSkill.ProfessionCrafting);

            RogueSkill.ProfessionArmorCap = new GenericProfession(skill: this, id: "ArmorCap", name: I18n.RogueSkill_Profession_ArmorProficiency_Name, description: I18n.RogueSkill_Profession_ArmorProficiency_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png")
            };
            this.Professions.Add(RogueSkill.ProfessionArmorCap);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, RogueSkill.ProfessionCrafting, RogueSkill.ProfessionArmorCap, RogueSkill.ProfessionArmorRecovery));

            // Level 10 - track B
            RogueSkill.ProfessionShadowStep = new GenericProfession(skill: this, id: "ShadowStep", name: I18n.Ability_Shadowstep_Name, description: I18n.Ability_Shadowstep_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png")
            };
            this.Professions.Add(RogueSkill.ProfessionShadowStep);

            RogueSkill.ProfessionHuntersMark = new GenericProfession(skill: this, id: "HuntersMark", name: I18n.RogueSkill_Profession_HuntersMark_Name, description: I18n.RogueSkill_Profession_HuntersMark_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/rogue/icon.png")
            };
            this.Professions.Add(RogueSkill.ProfessionHuntersMark);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, RogueSkill.ProfessionShadowStep, RogueSkill.ProfessionHuntersMark, RogueSkill.ProfessionBowSecondShot));
        }

        public override string GetName()
        {
            return I18n.RogueSkill_Name();
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);
            Game1.player.maxHealth += 3;

            string[][] craftingRecipes =
            [
                [],
                ["DN.SnS_ClothArmor", "DN.SnS_Bow", "DN.SnS_Arrow"],
                ["DN.SnS_CopperArmor", "DN.SnS_IronArmor", "DN.SnS_GoldArmor", "DN.SnS_IridiumArmor", "DN.SnS_RadioactiveArmor"],
                ["DN.SnS_FirestormArrow", "DN.SnS_IcicleArrow"],
                ["DN.SnS_ExquisiteEmerald", "DN.SnS_ExquisiteRuby", "DN.SnS_ExquisiteTopaz", "DN.SnS_ExquisiteAquamarine", "DN.SnS_ExquisiteAmethyst", "DN.SnS_ExquisiteJade", "DN.SnS_ExquisiteDiamond"],
                [],
                ["DN.SnS_WindwakerArrow"],
                ["DN.SnS_RicochetArrow"],
                ["DN.SnS_LightbringerArrow"],
                [],
            ];

            foreach (var recipe in craftingRecipes[level])
            {
                Game1.player.craftingRecipes.Add(recipe, 0);
            }
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> ret = new List<string>();

            switch ( level )
            {
                case 1:
                    ret.Add(I18n.RogueSkill_Unlock_1());
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ClothArmor", false).DisplayName));
                    break;
                case 2:
                    ret.Add(I18n.RogueSkill_Unlock_2().Replace('^','\n'));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_CopperArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_IronArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_GoldArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_IridiumArmor", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_RadioactiveArmor", false).DisplayName));
                    break;
                case 3:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_FirestormArrow", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_IcicleArrow", false).DisplayName));
                    break;
                case 4:
                    ret.Add(I18n.RogueSkill_Unlock_4().Replace('^', '\n'));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteEmerald", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteRuby", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteTopaz", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteAquamarine", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteAmethyst", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteJade", false).DisplayName));
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_ExquisiteDiamond", false).DisplayName));
                    break;
                case 6:
                    ret.Add(I18n.RogueSkill_Unlock_6());
                    break;
                case 7:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_WindwakerArrow", false).DisplayName));
                    break;
                case 8:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_RicochetArrow", false).DisplayName));
                    break;
                case 9:
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe("DN.SnS_LightbringerArrow", false).DisplayName));
                    break;
            }

            if (level % 5 != 0)
                ret.Add(I18n.Level_Health(3));

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
        public static void Postfix(Farmer farmer)
        {
            int amt = farmer.GetCustomSkillLevel(ModSnS.RogueSkill) * 3;
            farmer.maxHealth += amt;
            if (farmer.health == farmer.maxHealth - amt)
                farmer.health += amt;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.gainExperience))]
    public static class FarmerExpInterceptPatch
    {
        public static void Postfix(Farmer __instance, int which, int howMuch)
        {
            if (!__instance.eventsSeen.Contains("SnS.Ch1.Mateo.18")) // TODO: Change event
                return;
            if (which != Farmer.combatSkill)
                return;

            var data = __instance.GetFarmerExtData();
            float exp = data.expRemainderRogue.Value + howMuch / 2f;
            __instance.AddCustomSkillExperience(ModSnS.RogueSkill, (int)MathF.Truncate(exp));
            data.expRemainderRogue.Value = exp - MathF.Truncate(exp);
        }
    }
}
