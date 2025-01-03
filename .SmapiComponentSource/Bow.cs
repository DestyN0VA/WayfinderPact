using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Weapons;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Object = StardewValley.Object;
using static StardewValley.FarmerRenderer;
using static StardewValley.FarmerSprite;
using StardewValley.Minigames;
using StardewValley.SaveMigrations;
using StardewValley.Objects.Trinkets;
using System.Linq;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using System.Net;

namespace SwordAndSorcerySMAPI
{
    public static partial class Extensions
    {
        public static bool IsBow(this Slingshot slingshot)
        {
            if (((ItemRegistry.GetDataOrErrorItem(slingshot.QualifiedItemId).RawData as WeaponData)?.CustomFields?.ContainsKey("Bow") ?? false))
                return true;
            return false;
        }
        public static bool IsGun(this Slingshot slingshot)
        {
            if (slingshot.IsBow() && ((ItemRegistry.GetDataOrErrorItem(slingshot.QualifiedItemId).RawData as WeaponData)?.CustomFields?.ContainsKey("Bullets") ?? false))
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
    public static class SlingshotBowProjectileEffectsPatch
    {
        public static void Prefix(Slingshot __instance, GameLocation location, ref object __state)
        {
            if (!__instance.IsBow())
                return;

            location.projectiles.OnValueAdded += Projectiles_OnValueAdded;
            __state = __instance.attachments[0]?.getOne();
        }
        public static void Postfix(Slingshot __instance, GameLocation location, Farmer who, object __state)
        {
            if (!__instance.IsBow())
                return;

            if (__instance.IsGun())
            {
                who.playNearbySoundLocal("sns_gunshot");
            }

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
                DelayedAction.functionAfterDelay(() =>
                {
                    location.projectiles.OnValueAdded += Projectiles_OnValueAdded;
                    location.projectiles.Add(new BasicProjectile((int)(1 * (float)(dmg + Game1.random.Next(-(dmg / 2), dmg + 2)) * (1f + who.buffs.AttackMultiplier)), -1, 0, 0, (float)(Math.PI / (double)(64f + (float)Game1.random.Next(-63, 64))), 0f - v.X, 0f - v.Y, __instance.GetShootOrigin(who) - new Vector2(32f, 32f), __instance.GetAmmoCollisionSound(ammo), null, null, explode: false, damagesMonsters: true, location, who, __instance.GetAmmoCollisionBehavior(ammo), ammo.ItemId)
                    {
                        IgnoreLocationCollision = (Game1.currentLocation.currentEvent != null || Game1.currentMinigame != null)
                    });
                    location.projectiles.OnValueAdded -= Projectiles_OnValueAdded;
                }, 150);
            }
            location.projectiles.OnValueAdded -= Projectiles_OnValueAdded;
        }
        private static void Projectiles_OnValueAdded(Projectile value)
        {
            value.boundingBoxWidth.Value = 48 - 8;
            if (value is BasicProjectile basic && (basic.itemId?.Value?.ToLower().Contains("stygium") ?? false)) 
                basic.damageToFarmer.Value = (int)(basic.damageToFarmer.Value * 1.5);
            if (value.itemId?.Value?.Contains("Bullet") ?? false) // Hack
            {
                value.xVelocity.Value *= 2;
                value.yVelocity.Value *= 2;
            }
            else
            {
                value.rotationVelocity.Value = 0;
                value.startingRotation.Value = MathF.Atan2(value.yVelocity.Value, value.xVelocity.Value) + 0.785398f;
            }
            value.ignoreObjectCollisions.Value = true;
            if ((value.itemId?.Value?.Equals("(O)DN.SnS_RicochetArrow") ?? false) || (value.itemId?.Value?.Equals("(O)DN.SnS_RicochetBullet") ?? false))
                value.bouncesLeft.Value = 5;
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.update))]
    public static class RicochetArrowRotationUpdate
    {
        public static void Postfix(Projectile __instance)
        {
            if (!__instance.itemId?.Value?.ContainsIgnoreCase("arrow") ?? true) return;

            IReflectedProperty<float> rotation = ModSnS.instance.Helper.Reflection.GetProperty<float>(__instance, "rotation", true);

            if (rotation != null)
            {
                rotation.SetValue(MathF.Atan2(__instance.yVelocity.Value, __instance.xVelocity.Value) + 0.785398f);
            }
        }
    }

    [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.GetAmmoDamage))]
    public static class SlingshowBowAmmoDamagePatch
    {
        public static void Postfix(Slingshot __instance, StardewValley.Object ammunition, ref int __result)
        {
            if (__instance.IsBow())
                __result = 25 + (5 * Game1.player.GetCustomSkillLevel(ModSnS.RogueSkill));
            if (__instance.ItemId == "DN.SnS_longlivetheking_gun")
            {
                __result = (int)(__result * 2.5f);
            }
            if (Game1.player.CurrentTool?.QualifiedItemId == "(W)DN.SnS_ArtificerShield" || Game1.player.GetOffhand()?.QualifiedItemId == "(W)DN.SnS_ArtificerShield")
            {
                __result = (int)(__result * 1.75f);
            }
        }
    }

    [HarmonyPatch(typeof(BasicProjectile), nameof(BasicProjectile.behaviorOnCollisionWithMonster))]
    public static class BasicProjectileBowAmmoCollisionPatch
    {
        public static void Postfix(BasicProjectile __instance, NPC n)
        {
            if (n is not Monster m)
                return;

            switch (__instance.itemId.Value)
            {
                case "(O)DN.SnS_FirestormArrow":
                case "(O)DN.SnS_FirestormBullet":
                    FirestormAffector(__instance, m);
                    break;
                case "(O)DN.SnS_IcicleArrow":
                case "(O)DN.SnS_IcicleBullet":
                    IcicleAffector(m);
                    break;
                case "(O)DN.SnS_WindwakerArrow":
                case "(O)DN.SnS_WindwakerBullet":
                    WindwakerAffector(m);
                    break;
                case "(O)DN.SnS_LightbringerArrow":
                case "(O)DN.SnS_LightbringerBullet":
                    LightbringerAffector(m);
                    break;
            }
        }

        private static void FirestormAffector(BasicProjectile proj, Monster m)
        {
            DelayedAction.functionAfterDelay(() => { if (m.Health > 0) m.currentLocation.damageMonster(m.GetBoundingBox(), proj.damageToFarmer.Value / 2, proj.damageToFarmer.Value / 2 + 1, isBomb: true, proj.GetPlayerWhoFiredMe(m.currentLocation)); }, 1000);
            DelayedAction.functionAfterDelay(() => { if (m.Health > 0) m.currentLocation.damageMonster(m.GetBoundingBox(), proj.damageToFarmer.Value / 2, proj.damageToFarmer.Value / 2 + 1, isBomb: true, proj.GetPlayerWhoFiredMe(m.currentLocation)); }, 2000);
            DelayedAction.functionAfterDelay(() => { if (m.Health > 0) m.currentLocation.damageMonster(m.GetBoundingBox(), proj.damageToFarmer.Value / 2, proj.damageToFarmer.Value / 2 + 1, isBomb: true, proj.GetPlayerWhoFiredMe(m.currentLocation)); }, 3000);
        }

        private static void IcicleAffector(Monster m)
        {
            if (m is not DuskspireMonster || m.stunTime.Value <= 0)
            {
                m.stunTime.Value = 500 + Game1.random.Next(1000);
                Game1.Multiplayer.broadcastSprites(m.currentLocation, new TemporaryAnimatedSprite(Game1.mouseCursors2Name, new Rectangle(118, 227, 16, 13), new Vector2(0, 0), false, 0f, Color.White) { layerDepth = (m.StandingPixel.Y + 2) / 10000f, animationLength = 1, interval = m.stunTime.Value, scale = Game1.pixelZoom, id = (int)(m.position.X * 777 + m.position.Y * 77777), positionFollowsAttachedCharacter = true, attachedCharacter = m });
            }
        }

        private static void WindwakerAffector(Monster m)
        {
            m.setTrajectory(new Vector2(m.xVelocity * 3, m.yVelocity * 3));
        }

        private static void LightbringerAffector(Monster m)
        {
            if (m is DuskspireMonster)
            {
                Game1.showRedMessage(I18n.Arrow_Lightbringer_Duskspire());
                return;
            }

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
    public static class SlingshowBowAmmoAttachPatch1
    {
        public static void Postfix(Slingshot __instance, StardewValley.Object o, ref bool __result)
        {
            if (__instance.ItemId.EqualsIgnoreCase("DN.SnS_longlivetheking_gun"))
            {
                __instance.AttachmentSlotsCount = 2;
                NetObjectArray<StardewValley.Object> netObjectArray = __instance.attachments;
                if (netObjectArray != null && netObjectArray.Count > 0)
                {
                    for (int slot = 0; slot < __instance.attachments.Length; slot++)
                    {
                        if (KeychainsAndTrinkets.CanThisBeAttached(o, slot))
                        {
                            __result = true;
                            break;
                        }
                    }
                }
            }
            else if (__instance.IsGun())
            {
                __result = o.HasContextTag("bullet_item");
            }
            else if (__instance.IsBow())
            {
                __result = o.HasContextTag("arrow_item");
            }
        }
    }
    [HarmonyPatch(typeof(Tool), nameof(Tool.canThisBeAttached), [typeof(Object)])]
    public static class LLTKBulletsAndKeychainAttaching
    {
        public static void Postfix(Tool __instance, Object o, ref bool __result)
        {
            if (__instance.ItemId.EqualsIgnoreCase("DN.SnS_longlivetheking"))
            {
                __instance.AttachmentSlotsCount = 2;
                NetObjectArray<Object> netObjectArray = __instance.attachments;
                if (netObjectArray != null && netObjectArray.Count > 0)
                {
                    for (int slot = 0; slot < __instance.attachments.Length; slot++)
                    {
                        if (KeychainsAndTrinkets.CanThisBeAttached(o, slot))
                        {
                            __result = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Tool), nameof(Tool.drawAttachments))]
    public static class LLTKAttachmentSlots
    {
        public static bool Prefix(Tool __instance, SpriteBatch b, int x, int y)
        {
            if (!__instance.ItemId.EqualsIgnoreCase("DN.SnS_longlivetheking_gun") && !__instance.ItemId.EqualsIgnoreCase("DN.SnS_longlivetheking")) return true;

            __instance.AttachmentSlotsCount = 2;
            if (__instance is not MeleeWeapon)
                y += (__instance.enchantments.Count > 0) ? 8 : 4;
            else
            {
                y -= 132;
                x += 260;
            }

            ModSnS.instance.Helper.Reflection.GetMethod(__instance, "DrawAttachmentSlot", true).Invoke([0, b, x, y]);
            y += 68;
            ModSnS.instance.Helper.Reflection.GetMethod(__instance, "DrawAttachmentSlot", true).Invoke([1, b, x, y]);
            return false;
        }
    }

    [HarmonyPatch(typeof(Tool), nameof(Tool.attach))]
    public static class LLTKAttachSlots
    {
        public static bool Prefix(Tool __instance, Object o, ref Object __result)
        {
            if (!__instance.ItemId.EqualsIgnoreCase("DN.SnS_longlivetheking_gun") && !__instance.ItemId.EqualsIgnoreCase("DN.SnS_longlivetheking")) return true;
            __instance.AttachmentSlotsCount = 2;

            KeychainsAndTrinkets.TryAttach(__instance, o, out Object attached, out Object onHand, out int? Slot);

            if (Slot.HasValue)
            {
                __instance.attachments[Slot.Value] = attached;
            }
            __result = onHand;
            KeychainsAndTrinkets.HandleTrinketEquipUnequip(attached, onHand);

            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.receiveLeftClick))]
    public static class InventoryPageNoTrinketKeychainInTrinketSlot
    {
        public static bool Prefix(InventoryPage __instance, int x, int y)
        {
            foreach (ClickableComponent c in __instance.equipmentIcons)
            {
                if (!c.containsPoint(x, y))
                    continue;
                if (c.name == "Trinket" && Game1.player.CursorSlotItem is Trinket t && (t.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))
                    return false;
            }

            if (__instance.inventory.getItemAt(x, y) is Trinket trinket && (trinket.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false) && Game1.oldKBState.IsKeyDown(Keys.LeftShift))
                return false;

            if (ModSnS.instance.Helper.Reflection.GetMethod(__instance, "checkHeldItem", true).Invoke<bool>([null]) && Game1.oldKBState.IsKeyDown(Keys.LeftShift))
                if (ModSnS.instance.Helper.Reflection.GetMethod(__instance, "checkHeldItem", true).Invoke<bool>((Item i) => i is Trinket t && (t.GetTrinketData()?.CustomFields?.Keys?.Any(k => k.EqualsIgnoreCase("keychain_item")) ?? false))) 
                    return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.drawInMenu), [typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool)])]
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

    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.drawInMenu), [typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool)])]
    public static class MeleeWeaponSetAttachmentCountForLLTK
    {
        public static void Postfix(MeleeWeapon __instance)
        {
            if (__instance?.ItemId?.EqualsIgnoreCase("DN.SnS_longlivetheking") ?? false) __instance.AttachmentSlotsCount = 2;
        }
    }

    [HarmonyPatch(typeof(Tool), "GetAttachmentSlotSprite")]
    public static class LLTKSlotSprites
    {
        public static void Postfix(Tool __instance, int slot, out Texture2D texture, out Rectangle sourceRect)
        {
            texture = Game1.menuTexture;
            if (__instance.QualifiedItemId == "(W)DN.SnS_longlivetheking")
                sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, __instance.attachments[slot] != null ? 10 : (slot != 0 ? 70 : 43));
            else
                sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
        }
    }
    /*
            if (base.attachments[0] == null)
            {
                sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 43);
            }*/

    [HarmonyPatch(typeof(Slingshot), "GetAttachmentSlotSprite")]
    public static class LLTKGunSlotSprites
    {
        public static void Postfix(Slingshot __instance, int slot, out Texture2D texture, out Rectangle sourceRect)
        {
            texture = Game1.menuTexture;
            if (__instance.QualifiedItemId == "(W)DN.SnS_longlivetheking_gun")
                sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, __instance.attachments[slot] != null ? 10 : (slot != 0 ? 70 : 43));
            else 
                sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, __instance.attachments[0] == null ? 43 : 10);
        }
    }

    [HarmonyPatch(typeof(FarmerRenderer), nameof(FarmerRenderer.draw), [typeof(SpriteBatch), typeof(AnimationFrame), typeof(int), typeof(Rectangle), typeof(Vector2), typeof(Vector2), typeof(float), typeof(int), typeof(Color), typeof(float), typeof(float), typeof(Farmer)])]
    public static class FarmerRendererBowAndGunPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insns_)
        {
            List<CodeInstruction> insns = new(insns_);
            List<CodeInstruction> ret = new();

            int insertCounter = -1;
            for (int i = 0; i < insns.Count; ++i)
            {
                var insn = insns[i];

                if (insn.opcode == OpCodes.Ldfld && insn.operand is FieldInfo { Name: "usingSlingshot" } f)
                {
                    insertCounter = 1;
                }
                ret.Add(insn);

                if (insertCounter != -1 && insertCounter-- == 0)
                {
                    ret.Add(new(OpCodes.Ldarg_0));
                    ret.Add(new(OpCodes.Ldarg_1));
                    ret.Add(new(OpCodes.Ldarg_S, (short)12));
                    ret.Add(new(OpCodes.Ldarg_S, (short)5));
                    ret.Add(new(OpCodes.Ldarg_S, (short)8));
                    ret.Add(new(OpCodes.Ldarg_S, (short)11));
                    ret.Add(new(OpCodes.Ldarg_S, (short)7));
                    ret.Add(new(OpCodes.Call, AccessTools.Method(typeof(FarmerRendererBowAndGunPatch), nameof(DrawBowOrGunIfNeeded))));
                    ret.Add(new(OpCodes.Brtrue, insns[i].operand));
                }
            }

            return ret;
        }

        public static bool DrawBowOrGunIfNeeded(FarmerRenderer renderer, SpriteBatch b, Farmer who, Vector2 position, int facingDirection, float scale, float layerDepth)
        {
            if (who.CurrentTool is not Slingshot slingshot)
                return false;


            var baseTexture = ModSnS.instance.Helper.Reflection.GetField<Texture2D>(renderer, "baseTexture").GetValue();

            if (slingshot.IsGun())
            {
                Point point = Utility.Vector2ToPoint(slingshot.AdjustForHeight(Utility.PointToVector2(slingshot.aimPos.Value)));
                int mouseX = point.X;
                int y = point.Y;
                int backArmDistance = slingshot.GetBackArmDistance(who);
                Vector2 shoot_origin = slingshot.GetShootOrigin(who);
                float frontArmRotation = (float)Math.Atan2((float)y - shoot_origin.Y, (float)mouseX - shoot_origin.X) + (float)Math.PI;
                if (!Game1.options.useLegacySlingshotFiring)
                {
                    frontArmRotation -= (float)Math.PI;
                    if (frontArmRotation < 0f)
                    {
                        frontArmRotation += (float)Math.PI * 2f;
                    }
                }
                var tex = ItemRegistry.GetDataOrErrorItem(slingshot.QualifiedItemId).GetTexture();
                Rectangle rect = ItemRegistry.GetDataOrErrorItem(slingshot.QualifiedItemId.Replace("_gun", "")).GetSourceRect();
                switch (facingDirection)
                {
                    case 0:
                        b.Draw(baseTexture, position + new Vector2(4f + frontArmRotation * 8f, -44f), new Rectangle(173, 238, 9, 14), Color.White, 0f, new Vector2(4f, 11f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                        b.Draw(tex, position + new Vector2(4f + frontArmRotation * 8f, -44f) + new Vector2(40, -16), rect, Color.White, (-135-90) * MathF.PI / 180, new Vector2(0, 0), 4f * scale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, FarmerRenderer.GetLayerDepth(layerDepth + 0.000001f, FarmerSpriteLayers.SlingshotUp));
                        break;
                    case 1:
                        {
                            frontArmRotation = 0;
                            b.Draw(baseTexture, position + new Vector2(52 - backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(8f, 3f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                            b.Draw(baseTexture, position + new Vector2(36f, -44f), new Rectangle(156, 244, 9, 10), Color.White, frontArmRotation, new Vector2(0f, 3f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                            b.Draw(tex, position + new Vector2(36f, -44f) - new Vector2(16 - 48, -8 + 48), rect, Color.White, frontArmRotation + (45) * MathF.PI / 180, new Vector2(0, 0), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth + 0.00002f, FarmerSpriteLayers.SlingshotUp));

                            break;
                        }
                    case 3:
                        {
                            frontArmRotation = MathF.PI;
                            b.Draw(baseTexture, position + new Vector2(40 + backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(9f, 4f), 4f * scale, SpriteEffects.FlipHorizontally, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                            b.Draw(baseTexture, position + new Vector2(24f, -40f), new Rectangle(156, 244, 9, 10), Color.White, frontArmRotation + (float)Math.PI, new Vector2(8f, 3f), 4f * scale, SpriteEffects.FlipHorizontally, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                            b.Draw(tex, position + new Vector2(24f, -40f) - new Vector2(4+16, -12+32), rect, Color.White, (float)Math.PI + (45+90) * MathF.PI / 180, new Vector2(13, 5), 4f * scale, SpriteEffects.FlipHorizontally, FarmerRenderer.GetLayerDepth(layerDepth + 0.00002f, FarmerSpriteLayers.SlingshotUp));

                            break;
                        }
                    case 2:
                        b.Draw(baseTexture, position + new Vector2(4f, -32 - backArmDistance / 2), new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));
                        //b.Draw(baseTexture, position + new Vector2(44f - frontArmRotation * 10f, -16f), new Rectangle(167, 235, 7, 9), Color.White, 0f, new Vector2(3f, 5f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, dyeLayer: true));
                        b.Draw(tex, position + new Vector2(44f - frontArmRotation * 10f, -16f) - new Vector2(0, 16), rect, Color.White, 45 * MathF.PI / 180, new Vector2(0, 0), 4f * scale, SpriteEffects.FlipVertically, FarmerRenderer.GetLayerDepth(layerDepth + 0.00002f, FarmerSpriteLayers.Slingshot, dyeLayer: true));
                        break;
                }
            }
            else if (slingshot.IsBow())
            {
                Point point = Utility.Vector2ToPoint(slingshot.AdjustForHeight(Utility.PointToVector2(slingshot.aimPos.Value)));
                int mouseX = point.X;
                int y = point.Y;
                int backArmDistance = slingshot.GetBackArmDistance(who);
                Vector2 shoot_origin = slingshot.GetShootOrigin(who);
                float frontArmRotation = (float)Math.Atan2((float)y - shoot_origin.Y, (float)mouseX - shoot_origin.X) + (float)Math.PI;
                if (!Game1.options.useLegacySlingshotFiring)
                {
                    frontArmRotation -= (float)Math.PI;
                    if (frontArmRotation < 0f)
                    {
                        frontArmRotation += (float)Math.PI * 2f;
                    }
                }
                var tex = ModSnS.instance.Helper.ModContent.Load<Texture2D>("assets/bow-nostring.png");
                switch (facingDirection)
                {
                    case 0:
                        b.Draw(baseTexture, position + new Vector2(4f + frontArmRotation * 8f, -44f), new Rectangle(173, 238, 9, 14), Color.White, 0f, new Vector2(4f, 11f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                        b.Draw(tex, position + new Vector2(4f + frontArmRotation * 8f, -44f) + new Vector2(0, 16), null, Color.White, -135 * MathF.PI / 180, new Vector2(0, 0), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth + 0.000002f, FarmerSpriteLayers.SlingshotUp));
                        break;
                    case 1:
                        {
                            b.Draw(baseTexture, position + new Vector2(52 - backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(8f, 3f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                            b.Draw(baseTexture, position + new Vector2(36f, -44f), new Rectangle(156, 244, 9, 10), Color.White, frontArmRotation, new Vector2(0f, 3f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                            b.Draw(tex, position + new Vector2(36f, -44f) - new Vector2(16, -8), null, Color.White, frontArmRotation - 45 * MathF.PI / 180, new Vector2(0, 0), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth + 0.00002f, FarmerSpriteLayers.SlingshotUp));
                            
                            int slingshotAttachX = (int)(Math.Cos(frontArmRotation + (float)Math.PI / 2f - 30 * MathF.PI / 180) * (double)(20 - backArmDistance - 8) - Math.Sin(frontArmRotation + (float)Math.PI / 2f - 30 * MathF.PI / 180) * -68.0);
                            int slingshotAttachY = (int)(Math.Sin(frontArmRotation + (float)Math.PI / 2f - 30 * MathF.PI / 180) * (double)(20 - backArmDistance - 8) + Math.Cos(frontArmRotation + (float)Math.PI / 2f - 30 * MathF.PI / 180) * -68.0);
                            Utility.drawLineWithScreenCoordinates((int)(position.X + 52f - (float)backArmDistance), (int)(position.Y - 32f - 4f), (int)(position.X + 32f + (float)(slingshotAttachX / 2)), (int)(position.Y - 32f - 12f + (float)(slingshotAttachY / 2)), b, Color.White, FarmerRenderer.GetLayerDepth(layerDepth + 0.00001f, FarmerSpriteLayers.SlingshotUp));
                            slingshotAttachX = (int)(Math.Cos(frontArmRotation + (float)Math.PI / 2f + 55 * MathF.PI / 180) * (double)(20 - backArmDistance - 8) - Math.Sin(frontArmRotation + (float)Math.PI / 2f + 55 * MathF.PI / 180) * -90.0);
                            slingshotAttachY = (int)(Math.Sin(frontArmRotation + (float)Math.PI / 2f + 55 * MathF.PI / 180) * (double)(20 - backArmDistance - 8) + Math.Cos(frontArmRotation + (float)Math.PI / 2f + 55 * MathF.PI / 180) * -90.0);
                            Utility.drawLineWithScreenCoordinates((int)(position.X + 52f - (float)backArmDistance), (int)(position.Y - 32f - 4f), (int)(position.X + 32f + (float)(slingshotAttachX / 2)), (int)(position.Y - 32f - 12f + (float)(slingshotAttachY / 2)), b, Color.White, FarmerRenderer.GetLayerDepth(layerDepth + 0.00001f, FarmerSpriteLayers.SlingshotUp));
                            break;
                        }
                    case 3:
                        {
                            b.Draw(baseTexture, position + new Vector2(40 + backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(9f, 4f), 4f * scale, SpriteEffects.FlipHorizontally, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                            b.Draw(baseTexture, position + new Vector2(24f, -40f), new Rectangle(156, 244, 9, 10), Color.White, frontArmRotation + (float)Math.PI, new Vector2(8f, 3f), 4f * scale, SpriteEffects.FlipHorizontally, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                            b.Draw(tex, position + new Vector2(24f, -40f) - new Vector2(4, -12), null, Color.White, frontArmRotation + (float)Math.PI + 45 * MathF.PI / 180, new Vector2(13, 5), 4f * scale, SpriteEffects.FlipHorizontally, FarmerRenderer.GetLayerDepth(layerDepth + 0.00002f, FarmerSpriteLayers.SlingshotUp));
                            
                            int slingshotAttachX = (int)(Math.Cos(frontArmRotation + (float)Math.PI * 2f / 5f + 30 * MathF.PI / 180) * (double)(20 + backArmDistance - 8) - Math.Sin(frontArmRotation + (float)Math.PI * 2f / 5f + 30 * MathF.PI / 180) * -68.0);
                            int slingshotAttachY = (int)(Math.Sin(frontArmRotation + (float)Math.PI * 2f / 5f + 30 * MathF.PI / 180) * (double)(20 + backArmDistance - 8) + Math.Cos(frontArmRotation + (float)Math.PI * 2f / 5f + 30 * MathF.PI / 180) * -68.0);
                            Utility.drawLineWithScreenCoordinates((int)(position.X + 4f + (float)backArmDistance), (int)(position.Y - 32f - 8f), (int)(position.X + 26f + (float)slingshotAttachX * 4f / 10f), (int)(position.Y - 32f - 8f + (float)slingshotAttachY * 4f / 10f), b, Color.White, FarmerRenderer.GetLayerDepth(layerDepth + 0.00001f, FarmerSpriteLayers.SlingshotUp));
                            slingshotAttachX = (int)(Math.Cos(frontArmRotation + (float)Math.PI * 2f / 5f - 60 * MathF.PI / 180) * (double)(20 + backArmDistance - 8) - Math.Sin(frontArmRotation + (float)Math.PI * 2f / 5f - 60 * MathF.PI / 180) * -90);
                            slingshotAttachY = (int)(Math.Sin(frontArmRotation + (float)Math.PI * 2f / 5f - 60 * MathF.PI / 180) * (double)(20 + backArmDistance - 8) + Math.Cos(frontArmRotation + (float)Math.PI * 2f / 5f - 60 * MathF.PI / 180) * -90);
                            Utility.drawLineWithScreenCoordinates((int)(position.X + 4f + (float)backArmDistance), (int)(position.Y - 32f - 8f), (int)(position.X + 26f + (float)slingshotAttachX * 4f / 10f), (int)(position.Y - 32f - 8f + (float)slingshotAttachY * 4f / 10f), b, Color.White, FarmerRenderer.GetLayerDepth(layerDepth + 0.00001f, FarmerSpriteLayers.SlingshotUp));
                            break;
                        }
                    case 2:
                        b.Draw(baseTexture, position + new Vector2(4f, -32 - backArmDistance / 2), new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));
                        Utility.drawLineWithScreenCoordinates((int)(position.X + 16f), (int)(position.Y - 28f - (float)(backArmDistance / 2)), (int)(position.X + 12 - frontArmRotation * 10f), (int)(position.Y - 16f - 8f), b, Color.White, FarmerRenderer.GetLayerDepth(layerDepth + 0.00001f, FarmerSpriteLayers.Slingshot, dyeLayer: true));
                        Utility.drawLineWithScreenCoordinates((int)(position.X + 16f), (int)(position.Y - 28f - (float)(backArmDistance / 2)), (int)(position.X + 72f - frontArmRotation * 10f), (int)(position.Y - 16f - 8f), b, Color.White, FarmerRenderer.GetLayerDepth(layerDepth + 0.00001f, FarmerSpriteLayers.Slingshot, dyeLayer: true));
                        //b.Draw(baseTexture, position + new Vector2(44f - frontArmRotation * 10f, -16f), new Rectangle(167, 235, 7, 9), Color.White, 0f, new Vector2(3f, 5f), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, dyeLayer: true));
                        b.Draw(tex, position + new Vector2(44f - frontArmRotation * 10f, -16f) - new Vector2(0, 48), null, Color.White, 45 * MathF.PI / 180, new Vector2(0, 0), 4f * scale, SpriteEffects.None, FarmerRenderer.GetLayerDepth(layerDepth + 0.00002f, FarmerSpriteLayers.Slingshot, dyeLayer: true));
                        break;
                }
            }
            else return false;

            return true;
        }
    }
}
