using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Collections.Generic;

namespace SwordAndSorcerySMAPI.Framework.ModSkills
{
    public class DruidicsSkill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionShapeshift { get; set; }
        public static GenericProfession ProfessionAgriculture { get; set; }
        public static GenericProfession ProfessionShapeshiftStag { get; set; }
        public static GenericProfession ProfessionShapeshiftWolf { get; set; }
        public static GenericProfession ProfessionAgricultureMidgard { get; set; }
        public static GenericProfession ProfessionAgricultureYggdrasil { get; set; }

        public DruidicsSkill()
            : base("DestyNova.SwordAndSorcery.Druidics")
        {
            Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/Druid.png");
            SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/Druid1.png");

            ExperienceCurve = [100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000];

            ExperienceBarColor = new Microsoft.Xna.Framework.Color(48, 162, 218);

            // Level 5
            ProfessionShapeshift = new GenericProfession(skill: this, id: "SkilledShapeshifter", name: I18n.Druidics_Profession_Shapeshifting_Name, description: I18n.Druidics_Profession_Shapeshifting_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/SkilledShapeshifter.png")
            };
            Professions.Add(ProfessionShapeshift);

            ProfessionAgriculture = new GenericProfession(skill: this, id: "AncientAgronomist", name: I18n.Druidics_Profession_Agriculture_Name, description: I18n.Druidics_Profession_Agriculture_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/AncientAgronomist.png")
            };
            Professions.Add(ProfessionAgriculture);

            ProfessionsForLevels.Add(new ProfessionPair(5, ProfessionShapeshift, ProfessionAgriculture));

            // Level 10 - track A
            ProfessionShapeshiftStag = new GenericProfession(skill: this, id: "StagPath", name: I18n.Druidics_Profession_ShapeshiftingStag_Name, description: I18n.Druidics_Profession_ShapeshiftingStag_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/StagPath.png")
            };
            Professions.Add(ProfessionShapeshiftStag);

            ProfessionShapeshiftWolf = new GenericProfession(skill: this, id: "WolfPath", name: I18n.Druidics_Profession_ShapeshiftingWolf_Name, description: I18n.Druidics_Profession_ShapeshiftingWolf_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/WolfPath.png")
            };
            Professions.Add(ProfessionShapeshiftWolf);

            ProfessionsForLevels.Add(new ProfessionPair(10, ProfessionShapeshiftStag, ProfessionShapeshiftWolf, ProfessionShapeshift));

            // Level 10 - track B
            ProfessionAgricultureMidgard = new GenericProfession(skill: this, id: "BranchOfMidgard", name: I18n.Druidics_Profession_AgricultureMidgard_Name, description: I18n.Druidics_Profession_AgricultureMidgard_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/BranchOfMidgard.png")
            };
            Professions.Add(ProfessionAgricultureMidgard);

            ProfessionAgricultureYggdrasil = new GenericProfession(skill: this, id: "BranchOfYggdrasil", name: I18n.Druidics_Profession_AgricultureYggdrasil_Name, description: I18n.Druidics_Profession_AgricultureYggdrasil_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/BranchOfYggdrasil.png")
            };
            Professions.Add(ProfessionAgricultureYggdrasil);

            ProfessionsForLevels.Add(new ProfessionPair(10, ProfessionAgricultureMidgard, ProfessionAgricultureYggdrasil, ProfessionAgriculture));
        }

        public override string GetName()
        {
            return I18n.Druidics_Name();
        }

        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);

            if (level > 10) return; // Walk of Life

            string[][] recipes =
                [
                    null,
                    ["DN.SnS_ancientamaranth.seed", "DN.SnS_ancientepiphyticfern.seed"],
                    ["DN.SnS_glowingpolyporemushrooms.seed"],
                    ["DN.SnS_ancientwildfairyrose.seed"],
                    ["DN.SnS_ancientelderberry.seed"],
                    null,
                    ["DN.SnS_ancientbottlegourd.seed", "DN.SnS_lavaeelandstirfriedancientbottlegourd"],
                    ["DN.SnS_ancientgiantappleberry.seed", "DN.SnS_mushroomsredsauce"],
                    ["DN.SnS_ancientazuredetura.seed", "DN.SnS_ferngreensandpineapple"],
                    ["DN.SnS_ancientglowinghuckleberry.seed", "DN.SnS_ancienthuckleberryicecream"],
                    null,
                ];

            if (recipes[level] != null)
            {
                if (!Game1.player.knowsRecipe(recipes[level][0]))
                    //ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][0], false).DisplayName));
                    Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                if (recipes[level].Length == 2)
                {
                    if (level == 1)
                    {
                        if (!Game1.player.knowsRecipe(recipes[level][1]))
                            Game1.player.craftingRecipes.TryAdd(recipes[level][1], 0);
                        //ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][1], false).DisplayName));
                    }
                    else
                    {
                        if (!Game1.player.knowsRecipe(recipes[level][1]))
                            Game1.player.cookingRecipes.TryAdd(recipes[level][1], 0);
                        //ret.Add(I18n.Recipe_Cooking(new CraftingRecipe(recipes[level][1], true).DisplayName));
                    }
                }
            }
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            if (level > 10) return []; // Walk of Life

            string[][] recipes =
                [
                    null,
                    ["DN.SnS_ancientamaranth.seed", "DN.SnS_ancientepiphyticfern.seed"],
                    ["DN.SnS_glowingpolyporemushrooms.seed"],
                    ["DN.SnS_ancientwildfairyrose.seed"],
                    ["DN.SnS_ancientelderberry.seed"],
                    null,
                    ["DN.SnS_ancientbottlegourd.seed", "DN.SnS_lavaeelandstirfriedancientbottlegourd"],
                    ["DN.SnS_ancientgiantappleberry.seed", "DN.SnS_mushroomsredsauce"],
                    ["DN.SnS_ancientazuredetura.seed", "DN.SnS_ferngreensandpineapple"],
                    ["DN.SnS_ancientglowinghuckleberry.seed", "DN.SnS_ancienthuckleberryicecream"],
                    null,
                ];

            List<string> ret =
            [
                I18n.Druidics_Level_Generic(bonus: 1)
            ];

            if (level % 5 != 0)
                ret.Add(I18n.Level_Manacap(5));

            if (recipes[level] != null)
            {
                ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][0], false).DisplayName));
                //Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                if (recipes[level].Length >= 2)
                {
                    if (level == 1)
                    {
                        //Game1.player.craftingRecipes.TryAdd(recipes[level][1], 0);
                        ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][1], false).DisplayName));
                    }
                    else
                    {
                        //Game1.player.cookingRecipes.TryAdd(recipes[level][1], 0);
                        ret.Add(I18n.Recipe_Cooking(new CraftingRecipe(recipes[level][1], true).DisplayName));
                    }
                }
                if (recipes[level].Length == 3)
                {
                    ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][2], false).DisplayName));
                }

                if (level == 3)
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_RevivePlant_Name()));
            }

            return ret;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return I18n.Druidics_Level_Generic(bonus: level);
        }

        public override bool ShouldShowOnSkillsPage => Game1.player.eventsSeen.Contains("SnS.Ch2.Hector.16");
    }
}
