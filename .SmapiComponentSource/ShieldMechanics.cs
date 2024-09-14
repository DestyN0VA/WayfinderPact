using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.FarmerRenderer;

namespace SwordAndSorcerySMAPI
{

    [HarmonyPatch(typeof(MeleeWeapon), "specialCooldown")]
    public static class ShieldThrowSpecialCooldownPatch
    {
        public static void Postfix(MeleeWeapon __instance, ref int __result)
        {
            if (!__instance.IsShieldItem())
                return;

            __result = (int)ModSnS.State.ThrowCooldown;
        }
    }

    [HarmonyPatch(typeof(MeleeWeapon), "doAnimateSpecialMove")]
    public static class ShieldThrowAnimateSpecialMovePatch
    {
        public static bool Prefix(MeleeWeapon __instance)
        {
            if (!__instance.IsShieldItem())
                return true;

            Vector2 diff = ModSnS.instance.Helper.Input.GetCursorPosition().AbsolutePixels - Game1.player.StandingPixel.ToVector2();
            if (diff.Length() > 0 && diff.Length() > 5 * Game1.tileSize)
            {
                diff.Normalize();
                diff = diff * 5 * Game1.tileSize;
            }
            if (diff.Length() < Game1.tileSize || Game1.options.gamepadControls)
            {
                Vector2[] facings = [-Vector2.UnitY, Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX];
                diff = facings[Game1.player.FacingDirection] * Game1.tileSize * 5;
            }
            Vector2 target = Game1.player.Position + diff;
            float damageMult = 0.5f;
            int bounceCount = 1;
            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldThrowHit2))
            {
                ++bounceCount;
            }
            if (Game1.player.HasCustomProfession(PaladinSkill.ProfessionShieldThrowHit3))
            {
                ++bounceCount;
                damageMult = 1;
            }
            ModSnS.State.MyThrown.Add(new ThrownShield(Game1.player, (int)((__instance.minDamage.Value + __instance.maxDamage.Value) / 2 * damageMult), target, 15, __instance.QualifiedItemId, bounceCount));
            Game1.currentLocation.projectiles.Add(ModSnS.State.MyThrown.Last());

            ModSnS.State.ThrowCooldown = 3500;
            if (__instance.lastUser.professions.Contains(Farmer.acrobat))
                ModSnS.State.ThrowCooldown /= 2;
            if (__instance.hasEnchantmentOfType<ArtfulEnchantment>())
                ModSnS.State.ThrowCooldown /= 2;

            Game1.player.playNearbySoundLocal("daggerswipe");

            AnimatedSprite.endOfAnimationBehavior endOfAnimFunc = __instance.triggerDefenseSwordFunction;
            switch (__instance.lastUser.FacingDirection)
            {
                case 0:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(252, 250, 1, endOfAnimFunc);
                    __instance.Update(0, 0, __instance.lastUser);
                    break;
                case 1:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(243, 250, 1, endOfAnimFunc);
                    __instance.Update(1, 0, __instance.lastUser);
                    break;
                case 2:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(234, 250, 1, endOfAnimFunc);
                    __instance.Update(2, 0, __instance.lastUser);
                    break;
                case 3:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(259, 250, 1, endOfAnimFunc);
                    __instance.Update(3, 0, __instance.lastUser);
                    break;
            }

            ModSnS.instance.Helper.Reflection.GetMethod(__instance, "beginSpecialMove").Invoke(__instance.lastUser);

            return false;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.isWearingRing))]
    public static class FarmerShieldThornsPatch
    {
        public static void Postfix(Farmer __instance, string itemId, ref bool __result)
        {
            if (itemId != "839")
                return;

            if (__instance.HasCustomProfession(PaladinSkill.ProfessionShieldRetribution) &&
                __instance.GetArmorItem().GetArmorAmount() - __instance.GetFarmerExtData().armorUsed.Value <= 0)
            {
                __result = true;
            }
        }
    }

    /*
    public static partial class Extensions
    {
        public static bool IsShield(this Item __instance)
        {
            List<string> ids = new[]
            {
                "(O)DestyNova.SwordAndSorcery_BrokenHeroRelic",
                "(O)DestyNova.SwordAndSorcery_RepairedHeroRelic",
                "(O)DestyNova.SwordAndSorcery_LegendaryHeroRelic"
            }.ToList();
            return ids.Contains(__instance.QualifiedItemId);
        }

        public static float GetBlockCooldown(this Item __instance)
        {
            switch (__instance.QualifiedItemId)
            {
                case "(O)DestyNova.SwordAndSorcery_BrokenHeroRelic":
                    return 25;

                case "(O)DestyNova.SwordAndSorcery_RepairedHeroRelic":
                    return 20;

                case "(O)DestyNova.SwordAndSorcery_LegendaryHeroRelic":
                    return 15;
            }

            return 0;
        }

        public static float GetBlockRatio(this Item __instance)
        {
            switch (__instance.QualifiedItemId)
            {
                case "(O)DestyNova.SwordAndSorcery_BrokenHeroRelic":
                    return 0.85f;

                case "(O)DestyNova.SwordAndSorcery_RepairedHeroRelic":
                    return 0.70f;

                case "(O)DestyNova.SwordAndSorcery_LegendaryHeroRelic":
                    return 0.55f;
            }

            return 1;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public static class FarmerShieldDamagePatch
    {
        public static bool Prefix(Farmer __instance, ref int damage, bool overrideParry)
        {
            if (__instance != Game1.player || overrideParry ||
                Game1.player.get_armorSlot().Value == null ||
                ModSnS.State.BlockCooldown > 0 || ModSnS.State.MyThrown != null)
                return true;

            var shield = Game1.player.get_armorSlot().Value;

            ModSnS.State.BlockCooldown = shield.GetBlockCooldown();

            __instance.playNearbySoundAll("parry");

            damage = (int)(damage * shield.GetBlockRatio());

            return true;
        }
    }
    */
}
