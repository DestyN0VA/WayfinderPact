using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Reflection;
using CircleOfThornsSMAPI;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using SwordAndSorcerySMAPI;

namespace SwordAndSorcerySMAPI
{
    public class PaladinSkill : SpaceCore.Skills.Skill
    {
        public static GenericProfession ProfessionShieldThrowHit2;
        public static GenericProfession ProfessionShieldArmor1;
        public static GenericProfession ProfessionShieldThrowLightning;
        public static GenericProfession ProfessionShieldThrowHit3;
        public static GenericProfession ProfessionShieldArmor2;
        public static GenericProfession ProfessionShieldRetribution;

        public PaladinSkill()
            : base("DestyNova.SwordAndSorcery.Paladin")
        {
            this.Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/icon.png");
            this.SkillsPageIcon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/icon.png");

            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(252, 121, 27);

            // Level 5
            PaladinSkill.ProfessionShieldThrowHit2 = new GenericProfession(skill: this, id: "ThrowHit2", name: I18n.PaladinSkill_Profession_ShieldThrowHit2_Name, description: I18n.PaladinSkill_Profession_ShieldThrowHit2_Description)
            {
                Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/OnYourLeft.png")
            };
            this.Professions.Add(PaladinSkill.ProfessionShieldThrowHit2);

            PaladinSkill.ProfessionShieldArmor1 = new GenericProfession(skill: this, id: "Armor1", name: I18n.PaladinSkill_Profession_ShieldArmor1_Name, description: I18n.PaladinSkill_Profession_ShieldArmor1_Description)
            {
                Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/FightToEndTheFight.png")
            };
            this.Professions.Add(PaladinSkill.ProfessionShieldArmor1);

            this.ProfessionsForLevels.Add(new ProfessionPair(5, PaladinSkill.ProfessionShieldThrowHit2, PaladinSkill.ProfessionShieldArmor1));

            // Level 10 - track A
            PaladinSkill.ProfessionShieldThrowLightning = new GenericProfession(skill: this, id: "ThrowLightning", name: I18n.PaladinSkill_Profession_ShieldThrowLightning_Name, description: I18n.PaladinSkill_Profession_ShieldThrowLightning_Description)
            {
                Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/TheSunWillShineOnUsAgain.png")
            };
            this.Professions.Add(PaladinSkill.ProfessionShieldThrowLightning);

            PaladinSkill.ProfessionShieldThrowHit3 = new GenericProfession(skill: this, id: "ThrowHit3", name: () => I18n.PaladinSkill_Profession_ShieldThrowHit3_Name(Game1.player.Name), description: I18n.PaladinSkill_Profession_ShieldThrowHit3_Description)
            {
                Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/TheyHaveAnArmyWeHaveAFarmerName.png")
            };
            this.Professions.Add(PaladinSkill.ProfessionShieldThrowHit3);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, PaladinSkill.ProfessionShieldThrowLightning, PaladinSkill.ProfessionShieldThrowHit3, PaladinSkill.ProfessionShieldThrowHit2));

            // Level 10 - track B
            PaladinSkill.ProfessionShieldArmor2 = new GenericProfession(skill: this, id: "Armor2", name: I18n.PaladinSkill_Profession_ShieldArmor2_Name, description: I18n.PaladinSkill_Profession_ShieldArmor2_Description)
            {
                Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/ICanDoThisAllDay.png")
            };
            this.Professions.Add(PaladinSkill.ProfessionShieldArmor2);

            PaladinSkill.ProfessionShieldRetribution = new GenericProfession(skill: this, id: "Retribution", name: I18n.PaladinSkill_Profession_ShieldRetribution_Name, description: I18n.PaladinSkill_Profession_ShieldRetribution_Description)
            {
                Icon = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/paladin/YouGetHurtHurtEmBack.png")
            };
            this.Professions.Add(PaladinSkill.ProfessionShieldRetribution);

            this.ProfessionsForLevels.Add(new ProfessionPair(10, PaladinSkill.ProfessionShieldArmor2, PaladinSkill.ProfessionShieldRetribution, PaladinSkill.ProfessionShieldArmor1));
        }

        public override string GetName()
        {
            return I18n.PaladinSkill_Name();
        }
        public override void DoLevelPerk(int level)
        {
            base.DoLevelPerk(level);

            if (level > 10) return; // Walk of Life

            Game1.player.maxHealth += 5;
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> ret = new List<string>();

            if (level > 10) return ret;

            switch ( level )
            {
                case 2:
                    ret.Add(I18n.PaladinSkill_Unlock_2().Replace('^','\n'));
                    break;
                case 4:
                    ret.Add(I18n.PaladinSkill_Unlock_4().Replace('^', '\n'));
                    break;
                case 6:
                    ret.Add(I18n.PaladinSkill_Unlock_6().Replace('^', '\n'));
                    break;
                case 8:
                    ret.Add(I18n.PaladinSkill_Unlock_8().Replace('^', '\n'));
                    break;
            }

            if (level % 5 != 0)
                ret.Add(I18n.Level_Health(5));

            return ret;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return I18n.Level_Health(5 * level);
        }
        public override bool ShouldShowOnSkillsPage => Game1.player.eventsSeen.Any(e => e.StartsWith("SnS.Ch4.Intermission."));
    }

    [HarmonyPatch(typeof(LevelUpMenu), nameof(LevelUpMenu.RevalidateHealth))]
    public static class LevelUpMenuRevalidateHealthPatchAgain
    {
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            var ret = new List<CodeInstruction>();

            int emhIndex = ModSnS.sc.GetLocalIndexForMethod(original, "expected_max_health")[0];

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
                                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LevelUpMenuRevalidateHealthPatchAgain), nameof(GetExtraHealth) ) ),
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
            return farmer.GetCustomSkillLevel(ModTOP.PaladinSkill) * 5;
        }
    }

    [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.OnMoneyRewardClaimed))]
    public static class PaladinExpPatch1
    {
        public static void Postfix()
        {
            if (!ModTOP.PaladinSkill.ShouldShowOnSkillsPage)
                return;
            Game1.player.AddCustomSkillExperience(ModTOP.PaladinSkill, 250);
        }
    }

    [HarmonyPatch(typeof(Quest), nameof(Quest.OnMoneyRewardClaimed))]
    public static class PaladinExpPatch2
    {
        public static void Postfix()
        {
            if (!ModTOP.PaladinSkill.ShouldShowOnSkillsPage)
                return;
            Game1.player.AddCustomSkillExperience(ModTOP.PaladinSkill, 100);
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.receiveGift))]
    public static class PaladinExpPatch3
    {
        public static void Postfix(NPC __instance, StardewValley.Object o)
        {
            if (!ModTOP.PaladinSkill.ShouldShowOnSkillsPage)
                return;

            int taste = __instance.getGiftTasteForThisItem(o);
            switch (taste)
            {
                case NPC.gift_taste_stardroptea:
                    Game1.player.AddCustomSkillExperience(ModTOP.PaladinSkill, 250 / 5);
                    break;
                case NPC.gift_taste_love:
                    Game1.player.AddCustomSkillExperience(ModTOP.PaladinSkill, 80 / 5);
                    break;
                case NPC.gift_taste_like:
                    Game1.player.AddCustomSkillExperience(ModTOP.PaladinSkill, 45 / 5);
                    break;
                case NPC.gift_taste_neutral:
                    Game1.player.AddCustomSkillExperience(ModTOP.PaladinSkill, 20 / 5);
                    break;
            }
        }
    }
}