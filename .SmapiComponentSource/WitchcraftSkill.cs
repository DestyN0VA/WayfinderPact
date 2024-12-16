using System;
using System.Collections.Generic;
using CircleOfThornsSMAPI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

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

    public class WitchcraftSkill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionEssenceDrops;
        public static GenericProfession ProfessionSpellDamage;
        public static GenericProfession ProfessionPhilosopherStone;
        public static GenericProfession ProfessionNoTeleportCost;
        public static GenericProfession ProfessionSpellDamage2;
        public static GenericProfession ProfessionAetherBuff;

        private class AetherBuffProfession : GenericProfession
        {
            public AetherBuffProfession(SpaceCore.Skills.Skill skill, string id, Func<string> name, Func<string> description) : base(skill, id, name, description)
            {
            }

            public override void DoImmediateProfessionPerk()
            {
                Game1.player.GetFarmerExtData().maxMana.Value += 75;
            }

            public override void UndoImmediateProfessionPerk()
            {
                Game1.player.GetFarmerExtData().maxMana.Value -= 75;
            }
        }

        public WitchcraftSkill()
            : base("DestyNova.SwordAndSorcery.Witchcraft")
        {
            this.Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/witchcraft/icon.png");
            this.SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/witchcraft/icon.png");

            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(204, 0, 0);

            // Level 5
            WitchcraftSkill.ProfessionEssenceDrops = new GenericProfession(skill: this, id: "EssenceDrops", name: I18n.Witchcraft_Profession_EssenceDrops_Name, description: I18n.Witchcraft_Profession_EssenceDrops_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(16, 0, 16, 16)),
            };
            this.Professions.Add(WitchcraftSkill.ProfessionEssenceDrops);

            WitchcraftSkill.ProfessionSpellDamage = new GenericProfession(skill: this, id: "SpellDamage", name: I18n.Witchcraft_Profession_SpellDamage_Name, description: I18n.Witchcraft_Profession_SpellDamage_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(32, 0, 16, 16)),
            };
            this.Professions.Add(WitchcraftSkill.ProfessionSpellDamage);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, WitchcraftSkill.ProfessionEssenceDrops, WitchcraftSkill.ProfessionSpellDamage));

            // Level 10 - track A
            WitchcraftSkill.ProfessionPhilosopherStone = new GenericProfession(skill: this, id: "PhilosopherStone", name: I18n.Witchcraft_Profession_PhilosophersStone_Name, description: I18n.Witchcraft_Profession_PhilosophersStone_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(48, 0, 16, 16)),
            };
            this.Professions.Add(WitchcraftSkill.ProfessionPhilosopherStone);

            WitchcraftSkill.ProfessionNoTeleportCost = new GenericProfession(skill: this, id: "NoTeleportCost", name: I18n.Witchcraft_Profession_NoTeleportCost_Name, description: I18n.Witchcraft_Profession_NoTeleportCost_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(64, 0, 16, 16)),
            };
            this.Professions.Add(WitchcraftSkill.ProfessionNoTeleportCost);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, WitchcraftSkill.ProfessionPhilosopherStone, WitchcraftSkill.ProfessionNoTeleportCost, WitchcraftSkill.ProfessionEssenceDrops));

            // Level 10 - track B
            WitchcraftSkill.ProfessionSpellDamage2 = new GenericProfession(skill: this, id: "SpellDamage2", name: I18n.Witchcraft_Profession_SpellDamage2_Name, description: I18n.Witchcraft_Profession_SpellDamage2_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(80, 0, 16, 16)),
            };
            this.Professions.Add(WitchcraftSkill.ProfessionSpellDamage2);

            WitchcraftSkill.ProfessionAetherBuff = new AetherBuffProfession(skill: this, id: "AetherBuff", name: I18n.Witchcraft_Profession_AetherBuff_Name, description: I18n.Witchcraft_Profession_AetherBuff_Description)
            {
                Icon = ModTOP.StuffTexture.CopySubrect(new Rectangle(96, 0, 16, 16)),
            };
            this.Professions.Add(WitchcraftSkill.ProfessionAetherBuff);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, WitchcraftSkill.ProfessionSpellDamage2, WitchcraftSkill.ProfessionAetherBuff, WitchcraftSkill.ProfessionSpellDamage));
        }

        public override string GetName()
        {
            return I18n.Witchcraft_Name();
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);
        }


        public override List<string> GetExtraLevelUpInfo(int level)
        {
            if (level > 10) return []; // Walk of Life

            List<string> ret = new List<string>();
            if (level % 5 != 0)
                ret.Add(I18n.Level_Manacap(10));

            return ret;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return I18n.Level_Manacap(level * 10);
        }
        public override bool ShouldShowOnSkillsPage => Game1.player.eventsSeen.Contains(ModTOP.WitchcraftUnlock);
    }
}
