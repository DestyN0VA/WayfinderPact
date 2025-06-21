using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Tools;
using SwordAndSorcerySMAPI.Framework.NEA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace SwordAndSorcerySMAPI.Framework.DualWieldingAndWeapons;

public static class DualWieldExtensions
{
    public static MeleeWeapon GetOffhand(this Farmer farmer)
    {
        return ModSnS.SpaceCore.GetItemInEquipmentSlot(farmer, $"{ModSnS.Instance.ModManifest.UniqueID}_Offhand") as MeleeWeapon ?? null;
    }
}

public static class DualWieldingEnchants
{
    public static void HandleEnchants(object sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        var who = Game1.player;
        var OrigEnchs = who.GetFarmerExtData().OrigEnchs;

        if (OrigEnchs.Any())
        {
            foreach (var ench in OrigEnchs)
            {
                ench.Key.enchantments.Set(ench.Value);

            }
            OrigEnchs.Clear();
        }

        Utility.ForEachItem(item =>
        {

            if (item is MeleeWeapon or Slingshot)
            {
                Tool t = item as Tool;
                t.enchantments.RemoveWhere(e => t.enchantments.Any(e2 => e != e2 && e.GetType() == e2.GetType()));
                if (t?.ItemId?.ContainsIgnoreCase("DN.SnS_longlivetheking") ?? false)
                {
                    t.specialItem = true;
                    if (t.attachments.Count != 2)
                    {
                        INetSerializable parent = t.attachments.Parent;
                        try
                        {
                            t.attachments.Parent = null;
                            t.AttachmentSlotsCount = 2;
                        }
                        catch (Exception e)
                        {
                            Log.Warn(e.ToString());
                        }
                        finally
                        {
                            if (parent != null)
                                t.attachments.Parent = parent;
                        }
                    }
                }
            }
            return true;
        });
    }
}

[HarmonyPatch(typeof(MeleeWeapon), "doAnimateSpecialMove")]
static class DualWieldingDoAnimateSpecialMovePatch
{
    internal static ConditionalWeakTable<MeleeWeapon, FarmerSprite> fake = [];

    static void Postfix(MeleeWeapon __instance)
    {
        if (__instance.lastUser == null || __instance.lastUser.CurrentTool != __instance) return;

        var offhand = __instance.lastUser.GetOffhand();
        if (offhand == null) return;

        var realSprite = __instance.lastUser.Sprite;
        __instance.lastUser.Sprite = fake.GetOrCreateValue(offhand);

        float[] OldCooldowns = [
            ModSnS.State.ThrowCooldown,
            MeleeWeapon.defenseCooldown,
            MeleeWeapon.daggerCooldown,
            MeleeWeapon.clubCooldown
            ];

        try
        {
            offhand.animateSpecialMove(__instance.lastUser);
        }
        finally
        {
            __instance.lastUser.Sprite = realSprite;
            ModSnS.State.ThrowCooldown = OldCooldowns[0];
            MeleeWeapon.defenseCooldown = (int)OldCooldowns[1];
            MeleeWeapon.daggerCooldown = (int)OldCooldowns[2];
            MeleeWeapon.clubCooldown = (int)OldCooldowns[3];
        }
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insns)
    {
        CodeMatcher match = new(insns);

        var operand = match.MatchStartForward([
            new(OpCodes.Beq_S)
            ]).Operand;

        match.Start()
            .MatchEndForward([
                new(OpCodes.Ldfld),
                new(OpCodes.Brfalse_S)
                ])
            .Advance(1)
            .RemoveInstructions(5)
            .InsertAndAdvance([
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, AccessTools.Method(typeof(DualWieldingDoAnimateSpecialMovePatch), nameof(DoCheck))),
                new(OpCodes.Brtrue, operand)
                ]);

        return match.Instructions();
    }

    public static bool DoCheck(MeleeWeapon w)
    {
        return w == w.lastUser?.CurrentTool || w == w.lastUser?.GetOffhand();
    }
}

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.drawDuringUse), [typeof(int), typeof(int), typeof(SpriteBatch), typeof(Vector2), typeof(Farmer)])]
public static class DualWieldingDrawDuringUsePatch
{
    internal static Vector2[] offsets = [new(0, -32), new(-32, 0), new(0, 32), new(32, 0)];

    static void Postfix(MeleeWeapon __instance, int frameOfFarmerAnimation, int facingDirection, SpriteBatch spriteBatch, Vector2 playerPosition, Farmer f)
    {
        var offhand = f.GetOffhand();
        if (offhand != null && __instance == f.CurrentTool && !__instance.isScythe() && !__instance.IsShieldItem() && !offhand.IsShieldItem())
            MeleeWeapon.drawDuringUse(frameOfFarmerAnimation, facingDirection, spriteBatch, playerPosition, f, offhand.GetDrawnItemId(), offhand.type.Value, offhand.isOnSpecial);
    }
}

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage))]
static class DualWieldingDoDamagePatch
{
    readonly static Dictionary<Monster, int> origInvinc = [];

    static void Prefix(MeleeWeapon __instance, GameLocation location, Farmer who)
    {
        __instance.lastUser ??= who;

        if (__instance != __instance.lastUser.CurrentTool || __instance.isScythe())
            return;

        origInvinc.Clear();
        foreach (Monster m in location.characters.Where(npc => npc is Monster).Cast<Monster>())
            origInvinc.Add(m, m.invincibleCountdown);
    }

    static void Postfix(MeleeWeapon __instance, GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
    {
        __instance.lastUser ??= who;

        if (__instance != __instance.lastUser.CurrentTool || __instance.isScythe() || __instance.lastUser.GetOffhand() == null)
            return;

        var offhand = __instance.lastUser.GetOffhand();

        if (DualWieldingDaggerSpecialDetectionPatch.DaggerCounter > 0 && offhand.type.Value != MeleeWeapon.dagger)
            return;

        Dictionary<Monster, int> oldInvincCounters = [];
        Vector2 a = default, b = default;
        Rectangle offhandRect = offhand.getAreaOfEffect(x, y, facingDirection, ref a, ref b, who.GetBoundingBox(), who.FarmerSprite.currentAnimationIndex);

        foreach (var monster in location.characters.Where(npc => npc is Monster).Cast<Monster>())
        {
            if (monster.TakesDamageFromHitbox(offhandRect))
            {
                oldInvincCounters.Add(monster, monster.invincibleCountdown);
                monster.invincibleCountdown = origInvinc.TryGetValue(monster, out int i) ? i : 0;
            }
        }

        try
        {
            offhand.DoDamage(location, x, y, facingDirection, power, who);
        }
        finally
        {
            foreach (var invinc in oldInvincCounters)
                invinc.Key.setInvincibleCountdown(invinc.Value);
        }
    }
}

[HarmonyPatch(typeof(MeleeWeapon), "doDaggerFunction")]
public static class DualWieldingDaggerSpecialDetectionPatch
{
    public static int DaggerCounter { get; set; } = 0;

    public static void Prefix()
    {
        ++DaggerCounter;
    }
    public static void Postfix()
    {
        --DaggerCounter;
    }
}

[HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
public static class DualWieldingOffhandParryPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var local = ModSnS.SpaceCore.GetLocalIndexForMethod(original, "playerParryable")[0];
        List<CodeInstruction> ret = [];

        foreach (var insn in instructions)
        {
            ret.Add(insn);
            if (insn.opcode == OpCodes.Stloc && (int)insn.operand == local || insn.opcode.ToString().StartsWith("stloc.") && insn.opcode.ToString()["stloc.".Length..] == local.ToString())
            {
                ret.Add(new(OpCodes.Ldloc, local));
                ret.Add(new(OpCodes.Ldarg_0));
                ret.Add(new(OpCodes.Call, AccessTools.Method(typeof(DualWieldingOffhandParryPatch), nameof(OffhandSpecialCheck))));
                ret.Add(new(OpCodes.Stloc, local));
            }
        }

        return ret;
    }

    public static bool OffhandSpecialCheck(bool orig, Farmer instance)
    {
        var offhand = instance.GetOffhand();

        if (offhand != null && (((instance.CurrentTool as MeleeWeapon)?.isOnSpecial ?? false) || MeleeWeapon.daggerHitsLeft >= 1) && offhand.type.Value == MeleeWeapon.defenseSword)
            return true;

        return orig;
    }
}

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.drawInMenu))]
public static class DrawShieldCooldownPatch
{
    public static bool Prefix(MeleeWeapon __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        if (__instance.IsShieldItem())
        {
            float coolDownLevel = CooldownLevel();
            bool drawing_as_debris = drawShadow && drawStackNumber == StackDrawType.Hide;

            ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(__instance.GetDrawnItemId());
            Texture2D texture = dataOrErrorItem.GetTexture();
            Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
            spriteBatch.Draw(texture, location + (__instance.type.Value == 1 ? new Vector2(38f, 25f) : new Vector2(32f, 32f)), sourceRect, color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);
            if (coolDownLevel > 0f && drawShadow && !drawing_as_debris && !__instance.isScythe() && (Game1.activeClickableMenu == null || Game1.activeClickableMenu is not ShopMenu || scaleSize != 1f))
            {
                spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)location.X, (int)location.Y + (64 - (int)(coolDownLevel * 64f)), 64, (int)(coolDownLevel * 64f)), Color.Red * 0.66f);
            }
            __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
            return false;
        }
        if (ModSnS.State.LltkAnim.ShouldDraw(__instance.QualifiedItemId))
        {
            spriteBatch.Draw(ModSnS.State.LltkAnim.Texture, location + ((__instance.type.Value == 1) ? new Vector2(38f, 25f) : new Vector2(32f, 32f)), ModSnS.State.LltkAnim.SourceRect, color * transparency, 0f, new Vector2(24, 24), 4f * scaleSize, SpriteEffects.None, layerDepth);
            // draw shadow?
            return false;
        }
        return true;
    }

    private static float CooldownLevel()
    {
        if (ModSnS.State.ThrowCooldown > 0)
            return ModSnS.State.ThrowCooldown / 3500;
        return 0;
    }
}