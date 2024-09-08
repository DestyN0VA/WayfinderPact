using System.Collections.Generic;
using CircleOfThornsSMAPI;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SwordAndSorcerySMAPI;

namespace SwordAndSorcerySMAPI
{
    public class BardicsSkill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionBuff;
        public static GenericProfession ProfessionAttack;
        public static GenericProfession ProfessionBuffStrength;
        public static GenericProfession ProfessionBuffDuration;
        public static GenericProfession ProfessionAttackDamage;
        public static GenericProfession ProfessionAttackRange;

        public BardicsSkill()
            : base("DestyNova.SwordAndSorcery.Bardics")
        {
            // TODO: Change icons to bardics

            this.Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/icon.png");
            this.SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/icon.png");

            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(85, 33, 145);

            // Level 5
            BardicsSkill.ProfessionBuff = new GenericProfession(skill: this, id: "BuffSong", name: I18n.Bardics_Profession_Npcbuff, description: () => I18n.Bardics_Level_Song(I18n.Bardics_Song_Npcbuff_Name())+"\n" + I18n.Bardics_Song_Npcbuff_Description())
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/CollegeOfEloquence.png")
            };
            this.Professions.Add(BardicsSkill.ProfessionBuff);

            BardicsSkill.ProfessionAttack = new GenericProfession(skill: this, id: "AttackSong", name: I18n.Bardics_Profession_Attack, description: () => I18n.Bardics_Level_Song(I18n.Bardics_Song_Attack_Name()) + "\n" + I18n.Bardics_Song_Attack_Description())
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/CollegeOfValor.png")
            };
            this.Professions.Add(BardicsSkill.ProfessionAttack);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, BardicsSkill.ProfessionBuff, BardicsSkill.ProfessionAttack));

            // Level 10 - track A
            BardicsSkill.ProfessionBuffStrength = new GenericProfession(skill: this, id: "BuffSongStrength", name: I18n.Bardics_Profession_Npcbuff_Strength_Name, description: I18n.Bardics_Profession_Npcbuff_Strength_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/AnimatedPerformance.png")
            };
            this.Professions.Add(BardicsSkill.ProfessionBuffStrength);

            BardicsSkill.ProfessionBuffDuration = new GenericProfession(skill: this, id: "BuffSondDuration", name: I18n.Bardics_Profession_Npcbuff_Duration_Name, description: I18n.Bardics_Profession_Npcbuff_Duration_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/CreativeCrescendo.png")
            };
            this.Professions.Add(BardicsSkill.ProfessionBuffDuration);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, BardicsSkill.ProfessionBuffStrength, BardicsSkill.ProfessionBuffDuration, BardicsSkill.ProfessionBuff));

            // Level 10 - track B
            BardicsSkill.ProfessionAttackDamage = new GenericProfession(skill: this, id: "AttackSongStrength", name: I18n.Bardics_Profession_Attack_Strength_Name, description: I18n.Bardics_Profession_Attack_Strength_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/WordsOfTerror.png")
            };
            this.Professions.Add(BardicsSkill.ProfessionAttackDamage);

            BardicsSkill.ProfessionAttackRange = new GenericProfession(skill: this, id: "AttackSongRange", name: I18n.Bardics_Profession_Attack_Range_Name, description: I18n.Bardics_Profession_Attack_Range_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/WordsOfMajesty.png")
            };
            this.Professions.Add(BardicsSkill.ProfessionAttackRange);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, BardicsSkill.ProfessionAttackDamage, BardicsSkill.ProfessionAttackRange, BardicsSkill.ProfessionAttack));
        }

        public override string GetName()
        {
            return I18n.Bardics_Name();
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);
            if (level > 10) return; // Walk of Life
            Game1.player.GetFarmerExtData().maxMana.Value += 10;
        }


        public override List<string> GetExtraLevelUpInfo(int level)
        {
            if (level > 10) return []; // Walk of Life
            string[] songMapping =
            {
                I18n.Bardics_Song_Buff_Name() + "\n" + I18n.Bardics_Song_Buff_Description(),
                I18n.Bardics_Song_Battle_Name() + "\n" + I18n.Bardics_Song_Battle_Description(),
                I18n.Bardics_Song_Restoration_Name() + "\n" + I18n.Bardics_Song_Restoration_Description(),
                I18n.Bardics_Song_Protection_Name() + "\n" + I18n.Bardics_Song_Protection_Description(),
                "",
                I18n.Bardics_Song_Time_Name() + "\n" + I18n.Bardics_Song_Time_Description(),
                I18n.Bardics_Song_Horse_Name( Game1.player.horseName.Value ) + "\n" + I18n.Bardics_Song_Horse_Description(),
                I18n.Bardics_Song_Crops_Name() + "\n" + I18n.Bardics_Song_Crops_Description(),
                I18n.Bardics_Song_Obelisk_Name() + "\n" + I18n.Bardics_Song_Obelisk_Description(),
                "",
            };

            List<string> ret = new List<string>();
            if (level % 5 != 0)
                ret.Add(I18n.Level_Manacap(10));

            ret.Add(songMapping[level - 1]);

            return ret;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return I18n.Level_Manacap(level * 10);
        }
        public override bool ShouldShowOnSkillsPage => Game1.player.eventsSeen.Contains("SnS.Ch3.Cirrus.14");
    }
}
