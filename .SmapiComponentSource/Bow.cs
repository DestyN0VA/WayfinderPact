using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceCore;
using StardewValley;
using StardewValley.GameData.Weapons;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.Projectiles.BasicProjectile;

namespace SwordAndSorcerySMAPI
{
    internal class BowCraftingRecipe : CustomCraftingRecipe
    {
        public override string Description => ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_Bow").Description;

        public override Texture2D IconTexture => ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_Bow").GetTexture();

        public override Rectangle? IconSubrect => ItemRegistry.GetDataOrErrorItem("(W)DN.SnS_Bow").GetSourceRect();

        private IngredientMatcher[] ingreds = [new ObjectIngredientMatcher("(O)709", 10)];
        public override IngredientMatcher[] Ingredients => ingreds;

        public override Item CreateResult()
        {
            return ItemRegistry.Create("(W)DN.SnS_Bow");
        }
    }

    public static partial class Extensions
    {
        public static bool IsBow(this Slingshot slingshot)
        {
            if (((ItemRegistry.GetDataOrErrorItem(slingshot.QualifiedItemId).RawData as WeaponData)?.CustomFields?.ContainsKey("Bow") ?? false))
                return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(WeaponDataDefinition), nameof(WeaponDataDefinition.CreateItem))]
    public static class WeaponDataDefinitionCreateBowPatch
    {
        public static bool Prefix(ParsedItemData data, ref Item __result)
        {
            if (data?.RawData is WeaponData wdata && (wdata.CustomFields?.TryGetValue("Bow", out string isBow) ?? false) &&
                isBow.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                __result = new Slingshot(data.ItemId);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.PerformFire))]
    public static class SlingshowBowProjectileEffectsPatch
    {
        public static void Prefix(Slingshot __instance, GameLocation location, Farmer who, ref object __state)
        {
            location.projectiles.OnValueAdded += Projectiles_OnValueAdded;
            __state = __instance.attachments[0]?.getOne();
        }
        public static void Postfix(Slingshot __instance, GameLocation location, Farmer who, object __state)
        {
            if (Game1.player.HasCustomProfession(RogueSkill.ProfessionBowSecondShot) && Game1.random.NextDouble() <= 0.25)
            {
                var ammo = __state as StardewValley.Object;
                int dmg = __instance.GetAmmoDamage(ammo);
                int mouseX = __instance.aimPos.X;
                int mouseY = __instance.aimPos.Y;
                Vector2 v = Utility.getVelocityTowardPoint(__instance.GetShootOrigin(who), __instance.AdjustForHeight(new Vector2(mouseX, mouseY)), (float)(15 + Game1.random.Next(4, 6)) * (1f + who.buffs.WeaponSpeedMultiplier));
                if (!Game1.options.useLegacySlingshotFiring)
                {
                    v.X *= -1f;
                    v.Y *= -1f;
                }
                location.projectiles.Add(new BasicProjectile((int)(1 * (float)(dmg + Game1.random.Next(-(dmg / 2), dmg + 2)) * (1f + who.buffs.AttackMultiplier)), -1, 0, 0, (float)(Math.PI / (double)(64f + (float)Game1.random.Next(-63, 64))), 0f - v.X, 0f - v.Y, __instance.GetShootOrigin(who) - new Vector2(32f, 32f), __instance.GetAmmoCollisionSound(ammo), null, null, explode: false, damagesMonsters: true, location, who, __instance.GetAmmoCollisionBehavior(ammo), ammo.ItemId)
                {
                    IgnoreLocationCollision = (Game1.currentLocation.currentEvent != null || Game1.currentMinigame != null)
                });
            }
            location.projectiles.OnValueAdded -= Projectiles_OnValueAdded;
        }

        private static void Projectiles_OnValueAdded(StardewValley.Projectiles.Projectile value)
        {
            value.ignoreObjectCollisions.Value = true;
            if (value.itemId.Value == "(O)DN.SnS_RicochetArrow")
                value.bouncesLeft.Value = 5;
        }
    }

    [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.GetAmmoDamage))]
    public static class SlingshowBowAmmoDamagePatch
    {
        public static void Postfix(Slingshot __instance, StardewValley.Object ammunition, ref int __result)
        {
            if (__instance.IsBow())
                __result = 25 + (5 * Game1.player.GetCustomSkillLevel(ModSnS.RogueSkill));
        }
    }

    [HarmonyPatch(typeof(BasicProjectile), nameof(BasicProjectile.behaviorOnCollisionWithMonster))]
    public static class BasicProjectileBowAmmoCollisionPatch
    {
        public static void Postfix(BasicProjectile __instance, NPC n, GameLocation location)
        {
            if (n is not Monster m)
                return;

            switch (__instance.itemId.Value)
            {
                case "(O)DN.SnS_FirestormArrow":
                    FirestormAffector(__instance, m);
                    break;
                case "(O)DN.SnS_IcicleArrow":
                    IcicleAffector(__instance, m);
                    break;
                case "(O)DN.SnS_WindwakerArrow":
                    WindwakerAffector(__instance, m);
                    break;
                case "(O)DN.SnS_LightbringerArrow":
                    LightbringerAffector(__instance, m);
                    break;
            }
        }

        private static void FirestormAffector(BasicProjectile proj, Monster m)
        {
            DelayedAction.functionAfterDelay(() => { if (m.health.Value > 0) m.currentLocation.damageMonster(m.GetBoundingBox(), proj.damageToFarmer.Value / 2, proj.damageToFarmer.Value / 2 + 1, isBomb: true, proj.GetPlayerWhoFiredMe(m.currentLocation)); }, 1000);
            DelayedAction.functionAfterDelay(() => { if (m.health.Value > 0) m.currentLocation.damageMonster(m.GetBoundingBox(), proj.damageToFarmer.Value / 2, proj.damageToFarmer.Value / 2 + 1, isBomb: true, proj.GetPlayerWhoFiredMe(m.currentLocation)); }, 2000);
            DelayedAction.functionAfterDelay(() => { if (m.health.Value > 0) m.currentLocation.damageMonster(m.GetBoundingBox(), proj.damageToFarmer.Value / 2, proj.damageToFarmer.Value / 2 + 1, isBomb: true, proj.GetPlayerWhoFiredMe(m.currentLocation)); }, 3000);
        }

        private static void IcicleAffector(BasicProjectile proj, Monster m)
        {
            m.stunTime.Value = 500 + Game1.random.Next(1000);
            Game1.Multiplayer.broadcastSprites(m.currentLocation, new TemporaryAnimatedSprite(Game1.mouseCursors2Name, new Rectangle(118, 227, 16, 13), new Vector2(0, 0), false, 0f, Color.White) { layerDepth = (m.StandingPixel.Y + 2) / 10000f, animationLength = 1, interval = m.stunTime.Value, scale = Game1.pixelZoom, id = (int)(m.position.X * 777 + m.position.Y * 77777), positionFollowsAttachedCharacter = true, attachedCharacter = m });
        }

        private static void WindwakerAffector(BasicProjectile proj, Monster m)
        {
            m.setTrajectory(new Vector2(m.xVelocity * 3, m.yVelocity * 3));
        }

        private static void LightbringerAffector(BasicProjectile proj, Monster m)
        {
            for (int i = 0; i < 10; ++i)
            {
                Vector2 pos = m.GetBoundingBox().Center.ToVector2();
                pos.X += Game1.random.Next(-m.GetBoundingBox().Width / 2, m.GetBoundingBox().Width / 2 + 1);
                pos.Y += Game1.random.Next(-m.GetBoundingBox().Height / 2, m.GetBoundingBox().Height / 2 + 1);
                Game1.Multiplayer.broadcastSprites(m.currentLocation, new TemporaryAnimatedSprite(Game1.mouseCursors1_6Name, new Rectangle(304, 364 + 33, 11, 11), 75, 12, 1, pos, false, false, -1, 0f, Color.White, Game1.pixelZoom, 0, (float)(Game1.random.NextDouble() * Math.PI), 0, false));
            }
            m.currentLocation.characters.Remove(m);
        }
    }

    [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.canThisBeAttached))]
    public static class SlingshowBowAmmoAttachPatch
    {
        public static void Postfix(Slingshot __instance, StardewValley.Object o, ref bool __result)
        {
            if (__instance.IsBow())
            {
                __result = o.HasContextTag("arrow_item");
            }
        }
    }

    [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.drawInMenu), [typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof( StackDrawType), typeof( Color), typeof(bool)] )]
    public static class SlingshotBowDrawPatch
    {
        public static bool Prefix(Slingshot __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (!__instance.IsBow())
                return true;

            __instance.AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
            spriteBatch.Draw(ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).GetTexture(), location + new Vector2(32f, 29f), ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).GetSourceRect(), color * transparency, 0f, new Vector2(8f, 8f), scaleSize * 4f, SpriteEffects.None, layerDepth);
            if (drawStackNumber != 0 && __instance.attachments?[0] != null)
            {
                Utility.drawTinyDigits(__instance.attachments[0].Stack, spriteBatch, location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(__instance.attachments[0].Stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f), 3f * scaleSize, 1f, Color.White);
            }
            __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);

            return false;
        }
    }
}
