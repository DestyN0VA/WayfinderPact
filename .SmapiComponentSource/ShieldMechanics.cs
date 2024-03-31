using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.FarmerRenderer;

namespace SwordAndSorcerySMAPI
{
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
