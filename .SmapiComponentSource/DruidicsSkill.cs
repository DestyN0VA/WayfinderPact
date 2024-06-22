using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SwordAndSorcerySMAPI;

namespace CircleOfThornsSMAPI
{
    public class DruidicsSkill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionShapeshift;
        public static GenericProfession ProfessionAgriculture;
        public static GenericProfession ProfessionShapeshiftStag;
        public static GenericProfession ProfessionShapeshiftWolf;
        public static GenericProfession ProfessionAgricultureMidgard;
        public static GenericProfession ProfessionAgricultureYggdrasil;

        public DruidicsSkill()
            : base("DestyNova.SwordAndSorcery.Druidics")
        {
            this.Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/Druid.png");
            this.SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/Druid1.png");

            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(48, 162, 218);

            // Level 5
            DruidicsSkill.ProfessionShapeshift = new GenericProfession(skill: this, id: "SkilledShapeshifter", name: I18n.Druidics_Profession_Shapeshifting_Name, description: I18n.Druidics_Profession_Shapeshifting_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/SkilledShapeshifter.png")
            };
            this.Professions.Add(DruidicsSkill.ProfessionShapeshift);

            DruidicsSkill.ProfessionAgriculture = new GenericProfession(skill: this, id: "AncientAgronomist", name: I18n.Druidics_Profession_Agriculture_Name, description: I18n.Druidics_Profession_Agriculture_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/AncientAgronomist.png")
            };
            this.Professions.Add(DruidicsSkill.ProfessionAgriculture);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, DruidicsSkill.ProfessionShapeshift, DruidicsSkill.ProfessionAgriculture));

            // Level 10 - track A
            DruidicsSkill.ProfessionShapeshiftStag = new GenericProfession(skill: this, id: "StagPath", name: I18n.Druidics_Profession_ShapeshiftingStag_Name, description: I18n.Druidics_Profession_ShapeshiftingStag_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/StagPath.png")
            };
            this.Professions.Add(DruidicsSkill.ProfessionShapeshiftStag);

            DruidicsSkill.ProfessionShapeshiftWolf = new GenericProfession(skill: this, id: "WolfPath", name: I18n.Druidics_Profession_ShapeshiftingWolf_Name, description: I18n.Druidics_Profession_ShapeshiftingWolf_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/WolfPath.png")
            };
            this.Professions.Add(DruidicsSkill.ProfessionShapeshiftWolf);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, DruidicsSkill.ProfessionShapeshiftStag, DruidicsSkill.ProfessionShapeshiftWolf, DruidicsSkill.ProfessionShapeshift));

            // Level 10 - track B
            DruidicsSkill.ProfessionAgricultureMidgard = new GenericProfession(skill: this, id: "BranchOfMidgard", name: I18n.Druidics_Profession_AgricultureMidgard_Name, description: I18n.Druidics_Profession_AgricultureMidgard_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/BranchOfMidgard.png")
            };
            this.Professions.Add(DruidicsSkill.ProfessionAgricultureMidgard);

            DruidicsSkill.ProfessionAgricultureYggdrasil = new GenericProfession(skill: this, id: "BranchOfYggdrasil", name: I18n.Druidics_Profession_AgricultureYggdrasil_Name, description: I18n.Druidics_Profession_AgricultureYggdrasil_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/druidics/BranchOfYggdrasil.png")
            };
            this.Professions.Add(DruidicsSkill.ProfessionAgricultureYggdrasil);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, DruidicsSkill.ProfessionAgricultureMidgard, DruidicsSkill.ProfessionAgricultureYggdrasil, DruidicsSkill.ProfessionAgriculture));
        }

        public override string GetName()
        {
            return I18n.Druidics_Name();
        }

        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);
            string[][] recipes =
                new string[][]
                {
                    null,
                    new string[] { "Ancient Amaranth Seeds", "Ancient Epiphytic Fern Seeds" },
                    new string[] { "Ancient Glowing Polypore Mushroom Spores" },
                    new string[] { "Ancient Wild Fairy Rose Seeds" },
                    new string[] { "Ancient Elderberry Seeds" },
                    null,
                    new string[] { "Ancient Bottle Gourd Seeds", "DN.SnS_lavaeelandstirfriedancientbottlegourd" },
                    new string[] { "Ancient Giant Apple Berry Seeds", "DN.SnS_mushroomsredsauce" },
                    new string[] { "Ancient Azure Detura", "DN.SnS_ferngreensandpineapple" },
                    new string[] { "Ancient Glowing Huckleberry Seeds", "DN.SnS_ancienthuckleberryicecream" },
                    null,
                };

            var ext = SwordAndSorcerySMAPI.Extensions.GetFarmerExtData(Game1.player);
            ext.maxMana.Value += 5;

            if (recipes[level] != null)
            {
                //ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][0], false).DisplayName));
                Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                if (recipes[level].Length == 2)
                {
                    if (level == 1)
                    {
                        Game1.player.craftingRecipes.TryAdd(recipes[level][1], 0);
                        //ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][1], false).DisplayName));
                    }
                    else
                    {
                        Game1.player.cookingRecipes.TryAdd(recipes[level][1], 0);
                        //ret.Add(I18n.Recipe_Cooking(new CraftingRecipe(recipes[level][1], true).DisplayName));
                    }
                }
            }
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            string[][] recipes =
                new string[][]
                {
                    null,
                    new string[] { "Ancient Amaranth Seeds" },
                    new string[] { "Ancient Glowing Polypore Mushroom Spores", "Ancient Epiphytic Fern Seeds" },
                    new string[] { "Ancient Wild Fairy Rose Seeds" },
                    new string[] { "Ancient Elderberry Seeds" },
                    null,
                    new string[] { "Ancient Bottle Gourd Seeds", "DN.SnS_lavaeelandstirfriedancientbottlegourd" },
                    new string[] { "Ancient Giant Apple Berry Seeds", "DN.SnS_mushroomsredsauce" },
                    new string[] { "Ancient Azure Detura", "DN.SnS_ferngreensandpineapple" },
                    new string[] { "Ancient Glowing Huckleberry Seeds", "DN.SnS_ancienthuckleberryicecream" },
                    null,
                };

            List<string> ret = new List<string>
            {
                I18n.Druidics_Level_Generic(bonus: 1)
            };

            if (level % 5 != 0)
                ret.Add(I18n.Level_Manacap(5));

            if (recipes[level] != null)
            {
                ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][0], false).DisplayName));
                //Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                if (recipes[level].Length >= 2)
                {
                    if (level == 2)
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
