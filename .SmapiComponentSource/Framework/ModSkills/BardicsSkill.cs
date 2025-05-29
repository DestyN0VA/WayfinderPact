using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Collections.Generic;
using Skill = SpaceCore.Skills.Skill;

namespace SwordAndSorcerySMAPI.Framework.ModSkills
{
    public class BardicsSkill : Skill
    {
        public static GenericProfession ProfessionBuff { get; set; }
        public static GenericProfession ProfessionAttack { get; set; }
        public static GenericProfession ProfessionBuffStrength { get; set; }
        public static GenericProfession ProfessionBuffDuration { get; set; }
        public static GenericProfession ProfessionAttackDamage { get; set; }
        public static GenericProfession ProfessionAttackRange { get; set; }

        public BardicsSkill()
            : base("DestyNova.SwordAndSorcery.Bardics")
        {
            // TODO: Change icons to bardics

            Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/icon.png");
            SkillsPageIcon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/icon.png");

            ExperienceCurve = [100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000];

            ExperienceBarColor = new Microsoft.Xna.Framework.Color(85, 33, 145);

            // Level 5
            ProfessionBuff = new GenericProfession(skill: this, id: "BuffSong", name: I18n.Bardics_Profession_Npcbuff, description: () => I18n.Bardics_Song_Npcbuff_Name() + I18n.Bardics_Level_Song_Dash() + I18n.Bardics_Song_Npcbuff_Description_NoDetails())
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/CollegeOfEloquence.png")
            };
            Professions.Add(ProfessionBuff);

            ProfessionAttack = new GenericProfession(skill: this, id: "AttackSong", name: I18n.Bardics_Profession_Attack, description: () => I18n.Bardics_Song_Attack_Name() + I18n.Bardics_Level_Song_Dash() + I18n.Bardics_Song_Attack_Description_NoDetails())
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/CollegeOfValor.png")
            };
            Professions.Add(ProfessionAttack);

            ProfessionsForLevels.Add(new ProfessionPair(5, ProfessionBuff, ProfessionAttack));

            // Level 10 - track A
            ProfessionBuffStrength = new GenericProfession(skill: this, id: "BuffSongStrength", name: I18n.Bardics_Profession_Npcbuff_Strength_Name, description: I18n.Bardics_Profession_Npcbuff_Strength_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/AnimatedPerformance.png")
            };
            Professions.Add(ProfessionBuffStrength);

            ProfessionBuffDuration = new GenericProfession(skill: this, id: "BuffSondDuration", name: I18n.Bardics_Profession_Npcbuff_Duration_Name, description: I18n.Bardics_Profession_Npcbuff_Duration_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/CreativeCrescendo.png")
            };
            Professions.Add(ProfessionBuffDuration);

            ProfessionsForLevels.Add(new ProfessionPair(10, ProfessionBuffStrength, ProfessionBuffDuration, ProfessionBuff));

            // Level 10 - track B
            ProfessionAttackDamage = new GenericProfession(skill: this, id: "AttackSongStrength", name: I18n.Bardics_Profession_Attack_Strength_Name, description: I18n.Bardics_Profession_Attack_Strength_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/WordsOfTerror.png")
            };
            Professions.Add(ProfessionAttackDamage);

            ProfessionAttackRange = new GenericProfession(skill: this, id: "AttackSongRange", name: I18n.Bardics_Profession_Attack_Range_Name, description: I18n.Bardics_Profession_Attack_Range_Description)
            {
                Icon = ModCoT.Instance.Helper.ModContent.Load<Texture2D>("assets/bardics/WordsOfMajesty.png")
            };
            Professions.Add(ProfessionAttackRange);

            ProfessionsForLevels.Add(new ProfessionPair(10, ProfessionAttackDamage, ProfessionAttackRange, ProfessionAttack));
        }

        public override string GetName()
        {
            return I18n.Bardics_Name();
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);
        }


        public override List<string> GetExtraLevelUpInfo(int level)
        {
            if (level > 10) return []; // Walk of Life
            string[] songMapping =
            [
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Buff_Name() + "\n\n" + I18n.Bardics_Song_Buff_Description(),
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Battle_Name() + "\n\n" + I18n.Bardics_Song_Battle_Description(),
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Restoration_Name() + "\n\n" + I18n.Bardics_Song_Restoration_Description(),
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Protection_Name() + "\n\n" + I18n.Bardics_Song_Protection_Description(),
                "",
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Time_Name() + "\n\n" + I18n.Bardics_Song_Time_Description(),
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Horse_Name( Game1.player.horseName.Value ?? I18n.Cptoken_Horse() ) + "\n\n" + I18n.Bardics_Song_Horse_Description(),
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Crops_Name() + "\n\n" + I18n.Bardics_Song_Crops_Description(),
                I18n.Bardics_Level_Song_Learned() + I18n.Bardics_Song_Obelisk_Name() + "\n\n" + I18n.Bardics_Song_Obelisk_Description(),
                "",
            ];

            List<string> ret = [];
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
