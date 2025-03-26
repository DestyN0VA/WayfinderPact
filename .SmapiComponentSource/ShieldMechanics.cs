using HarmonyLib;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;
using System.Linq;

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

    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.getCategoryName))]
    public static class ShieldCategoryName
    {
        public static void Postfix(MeleeWeapon __instance, ref string __result)
        {
            if (__instance.GetData()?.CustomFields?.ContainsKey("DN.SnS_Shield") ?? false)
            {
                __result = I18n.ShieldCategory();
            }
            if (__instance.GetData()?.CustomFields?.ContainsKey("DN.SnS_Boomerang") ?? false)
            {
                __result = I18n.BoomerangeCategory();
            }
        }
    }

    [HarmonyPatch(typeof(Tool), nameof(Tool.getCategoryColor))]
    public static class ShieldCategoryColor
    {
        public static void Postfix(Tool __instance, ref Color __result)
        {
            if (__instance is MeleeWeapon && ((__instance as MeleeWeapon).GetData()?.CustomFields?.ContainsKey("DN.SnS_Shield") ?? false))
            {
                __result = Color.BlueViolet;
            }
            if (__instance is MeleeWeapon && ((__instance as MeleeWeapon).GetData()?.CustomFields?.ContainsKey("DN.SnS_Boomerang") ?? false))
            {
                __result = Color.MonoGameOrange;
            }
        }
    }

    [HarmonyPatch(typeof(MeleeWeapon), "doAnimateSpecialMove")]
    public static class ShieldThrowAnimateSpecialMovePatch
    {
        public static bool Prefix(MeleeWeapon __instance)
        {
            if (!__instance.IsShieldItem() || __instance.lastUser != Game1.player)
                return true;

            Vector2 diff = ModSnS.Instance.Helper.Input.GetCursorPosition().AbsolutePixels - Game1.player.StandingPixel.ToVector2();
            if (diff.Length() > 0 && diff.Length() > 8 * Game1.tileSize)
            {
                diff.Normalize();
                diff = diff * 8 * Game1.tileSize;
            }
            if (diff.Length() < Game1.tileSize || Game1.options.gamepadControls)
            {
                Vector2[] facings = [-Vector2.UnitY, Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX];
                diff = facings[Game1.player.FacingDirection] * Game1.tileSize * 8;
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
            if (__instance.lastUser?.professions.Contains(Farmer.acrobat) ?? false)
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
                default:
                    ((FarmerSprite)__instance.lastUser.Sprite).animateOnce(259, 250, 1, endOfAnimFunc);
                    __instance.Update(3, 0, __instance.lastUser);
                    break;
            }

            ModSnS.Instance.Helper.Reflection.GetMethod(__instance, "beginSpecialMove").Invoke(__instance.lastUser);

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
}
