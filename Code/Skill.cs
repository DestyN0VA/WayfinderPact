using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SwordAndSorcerySMAPI;

namespace CircleOfThornsSMAPI
{
    internal class Skill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionShapeshift;
        public static GenericProfession ProfessionAgriculture;
        public static GenericProfession ProfessionShapeshiftStag;
        public static GenericProfession ProfessionShapeshiftWolf;
        public static GenericProfession ProfessionAgricultureMidgard;
        public static GenericProfession ProfessionAgricultureYggdrasil;

        public Skill()
            : base("DestyNova.SwordAndSorcery.Druidics")
        {
            this.Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/Druid.png");
            this.SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/Druid1.png");

            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(196, 76, 255);

            // Level 5
            Skill.ProfessionShapeshift = new GenericProfession(skill: this, id: "SkilledShapeshifter", name: I18n.Druidics_Profession_Shapeshifting_Name, description: I18n.Druidics_Profession_Shapeshifting_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/SkilledShapeshifter.png")
            };
            this.Professions.Add(Skill.ProfessionShapeshift);

            Skill.ProfessionAgriculture = new GenericProfession(skill: this, id: "AncientAgronomist", name: I18n.Druidics_Profession_Agriculture_Name, description: I18n.Druidics_Profession_Agriculture_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/AncientAgronomist.png")
            };
            this.Professions.Add(Skill.ProfessionAgriculture);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, Skill.ProfessionShapeshift, Skill.ProfessionAgriculture));

            // Level 10 - track A
            Skill.ProfessionShapeshiftStag = new GenericProfession(skill: this, id: "StagPath", name: I18n.Druidics_Profession_ShapeshiftingStag_Name, description: I18n.Druidics_Profession_ShapeshiftingStag_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/StagPath.png")
            };
            this.Professions.Add(Skill.ProfessionShapeshiftStag);

            Skill.ProfessionShapeshiftWolf = new GenericProfession(skill: this, id: "WolfPath", name: I18n.Druidics_Profession_ShapeshiftingWolf_Name, description: I18n.Druidics_Profession_ShapeshiftingWolf_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/WolfPath.png")
            };
            this.Professions.Add(Skill.ProfessionShapeshiftWolf);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, Skill.ProfessionShapeshiftStag, Skill.ProfessionShapeshiftWolf, Skill.ProfessionShapeshift));

            // Level 10 - track B
            Skill.ProfessionAgricultureMidgard = new GenericProfession(skill: this, id: "BranchOfMidgard", name: I18n.Druidics_Profession_AgricultureMidgard_Name, description: I18n.Druidics_Profession_AgricultureMidgard_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/BranchOfMidgard.png")
            };
            this.Professions.Add(Skill.ProfessionAgricultureMidgard);

            Skill.ProfessionAgricultureYggdrasil = new GenericProfession(skill: this, id: "BranchOfYggdrasil", name: I18n.Druidics_Profession_AgricultureYggdrasil_Name, description: I18n.Druidics_Profession_AgricultureYggdrasil_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/skill/BranchOfYggdrasil.png")
            };
            this.Professions.Add(Skill.ProfessionAgricultureYggdrasil);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, Skill.ProfessionAgricultureMidgard, Skill.ProfessionAgricultureYggdrasil, Skill.ProfessionAgriculture));
        }

        public override string GetName()
        {
            return I18n.Druidics_Name();
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            string[][] recipes =
                new string[][]
                {
                    null,
                    new string[] { "Ancient Amaranth Seeds", "Ancient Epiphytic Fern Seeds" },
                    new string[] { "Ancient Glowing Polypore Mushroom Spores" },
                    new string[] { "Ancient Wild Fairy Rose Seeds" },
                    new string[] { "Ancient Elderberry Seeds" },
                    null,
                    new string[] { "Ancient Bottle Gourd Seeds", "swordandsorcery.lavaeelandstirfriedancientbottlegourd" },
                    new string[] { "Ancient Giant Apple Berry Seeds", "swordandsorcery.mushroomsredsauce" },
                    new string[] { "Ancient Azure Detura", "swordandsorcery.ferngreensandpineapple" },
                    new string[] { "Ancient Glowing Huckleberry Seeds", "swordandsorcery.ancienthuckleberryicecream" },
                    null,
                };

            List<string> ret = new List<string>
            {
                I18n.Druidics_Level_Generic(bonus: 1)
            };
            if (recipes[level] != null)
            {
                ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][0], false).DisplayName));
                Game1.player.craftingRecipes.TryAdd(recipes[level][0], 0);
                if (recipes[level].Length == 2)
                {
                    if (level == 1)
                    {
                        Game1.player.craftingRecipes.TryAdd(recipes[level][1], 0);
                        ret.Add(I18n.Recipe_Crafting(new CraftingRecipe(recipes[level][1], false).DisplayName));
                    }
                    else
                    {
                        Game1.player.cookingRecipes.TryAdd(recipes[level][1], 0);
                        ret.Add(I18n.Recipe_Cooking(new CraftingRecipe(recipes[level][1], true).DisplayName));
                    }
                }
            }

            return ret;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return I18n.Druidics_Level_Generic(bonus: level);
        }
    }
}
