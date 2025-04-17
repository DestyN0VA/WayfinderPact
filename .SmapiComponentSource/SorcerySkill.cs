using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using SpaceCore;

namespace SwordAndSorcerySMAPI
{
    public static partial class Extensions
    {
        public static Texture2D CopySubrect(this Texture2D tex, Rectangle rect)
        {
            var ret = new Texture2D(Game1.graphics.GraphicsDevice, rect.Width, rect.Height);
            Color[] cols = new Color[rect.Width * rect.Height];
            tex.GetData(0, rect, cols, 0, cols.Length);
            ret.SetData(cols);
            return ret;
        }
    }

    public class SorcerySkill : Skills.Skill
    {
        public static GenericProfession ProfessionEssenceDrops { get; set; }
        public static GenericProfession ProfessionSpellDamage { get; set; }
        public static GenericProfession ProfessionPhilosopherStone { get; set; }
        public static GenericProfession ProfessionNoTeleportCost { get; set; }
        public static GenericProfession ProfessionSpellDamage2 { get; set; }
        public static GenericProfession ProfessionAetherBuff { get; set; }

        private class AetherBuffProfession(Skills.Skill skill, string id, Func<string> name, Func<string> description) : GenericProfession(skill, id, name, description)
        {
            public override void DoImmediateProfessionPerk()
            {
                Game1.player.GetFarmerExtData().maxMana.Value += 75;
            }

            public override void UndoImmediateProfessionPerk()
            {
                Game1.player.GetFarmerExtData().maxMana.Value -= 75;
            }
        }

        public SorcerySkill()
            : base("DestyNova.SwordAndSorcery.Witchcraft")
        {
            this.Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/witchcraft/icon.png");
            this.SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/witchcraft/icon.png");

            this.ExperienceCurve = [100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000];

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(204, 0, 0);

            // Level 5
            SorcerySkill.ProfessionEssenceDrops = new GenericProfession(skill: this, id: "EssenceDrops", name: I18n.Witchcraft_Profession_EssenceDrops_Name, description: I18n.Witchcraft_Profession_EssenceDrops_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(16, 0, 16, 16)),
            };
            this.Professions.Add(SorcerySkill.ProfessionEssenceDrops);

            SorcerySkill.ProfessionSpellDamage = new GenericProfession(skill: this, id: "SpellDamage", name: I18n.Witchcraft_Profession_SpellDamage_Name, description: I18n.Witchcraft_Profession_SpellDamage_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(32, 0, 16, 16)),
            };
            this.Professions.Add(SorcerySkill.ProfessionSpellDamage);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, SorcerySkill.ProfessionEssenceDrops, SorcerySkill.ProfessionSpellDamage));

            // Level 10 - track A
            SorcerySkill.ProfessionPhilosopherStone = new GenericProfession(skill: this, id: "PhilosopherStone", name: I18n.Witchcraft_Profession_PhilosophersStone_Name, description: I18n.Witchcraft_Profession_PhilosophersStone_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(48, 0, 16, 16)),
            };
            this.Professions.Add(SorcerySkill.ProfessionPhilosopherStone);

            SorcerySkill.ProfessionNoTeleportCost = new GenericProfession(skill: this, id: "NoTeleportCost", name: I18n.Witchcraft_Profession_NoTeleportCost_Name, description: I18n.Witchcraft_Profession_NoTeleportCost_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(64, 0, 16, 16)),
            };
            this.Professions.Add(SorcerySkill.ProfessionNoTeleportCost);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, SorcerySkill.ProfessionPhilosopherStone, SorcerySkill.ProfessionNoTeleportCost, SorcerySkill.ProfessionEssenceDrops));

            // Level 10 - track B
            SorcerySkill.ProfessionSpellDamage2 = new GenericProfession(skill: this, id: "SpellDamage2", name: I18n.Witchcraft_Profession_SpellDamage2_Name, description: I18n.Witchcraft_Profession_SpellDamage2_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(80, 0, 16, 16)),
            };
            this.Professions.Add(SorcerySkill.ProfessionSpellDamage2);

            SorcerySkill.ProfessionAetherBuff = new AetherBuffProfession(skill: this, id: "AetherBuff", name: I18n.Witchcraft_Profession_AetherBuff_Name, description: I18n.Witchcraft_Profession_AetherBuff_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(96, 0, 16, 16)),
            };
            this.Professions.Add(SorcerySkill.ProfessionAetherBuff);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, SorcerySkill.ProfessionSpellDamage2, SorcerySkill.ProfessionAetherBuff, SorcerySkill.ProfessionSpellDamage));
        }

        public override string GetName()
        {
            return I18n.Witchcraft_Name();
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);

            if (level == 4)
                Game1.player.craftingRecipes.Add("DN.SnS_TeleportCircle", 1);
        }


        public override List<string> GetExtraLevelUpInfo(int level)
        {
            if (level > 10) return []; // Walk of Life

            List<string> ret = [];
            if (level % 5 != 0)
                ret.Add(I18n.Level_Manacap(10));

            switch (level)
            {
                case 1:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_Haste_Name()));
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_MagicMissle_Name()));
                    break;
                case 2:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_PocketChest_Name()));
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_MageArmor_Name()));
                    break;
                case 3:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_FindFamiliar_Name()));
                    ret.Add(I18n.Research_Spell(I18n.FinaleMinigame_Ability_Fireball_Name()));
                    break;
                case 4:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_MirrorImage_Name()));
                    ret.Add(I18n.Research_CraftingRecipe(I18n.TeleportCircle_Name()));
                    break;
                case 6:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_Polymorph_Name()));
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_LightningBolt_Name()));
                    break;
                case 7:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_GhostlyProjection_Name()));
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_Stasis_Name()));
                    break;
                case 8:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_Banishment_Name()));
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_IceBolt_Name()));
                    break;
                case 9:
                    ret.Add(I18n.Research_Spell(I18n.Witchcraft_Spell_PocketDimension_Name()));
                    break;
            }

            return ret;
        }

        public override string GetSkillPageHoverText(int level) => I18n.Level_Manacap(level * 10);

        public override bool ShouldShowOnSkillsPage => Game1.player.eventsSeen.Contains(ModTOP.WitchcraftUnlock);
    }
}
