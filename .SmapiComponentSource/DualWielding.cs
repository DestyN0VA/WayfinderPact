using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using NeverEndingAdventure.Utils;

namespace SwordAndSorcerySMAPI;

public static class DualWieldExtensions
{
    public static MeleeWeapon GetOffhand(this Farmer farmer)
    {
        return ModSnS.sc.GetItemInEquipmentSlot(farmer, $"{ModSnS.instance.ModManifest.UniqueID}_Offhand") as MeleeWeapon;
    }
}

[HarmonyPatch(typeof(MeleeWeapon), "doAnimateSpecialMove")]
public static class DualWieldingSpecialMovePatch
{
    internal static ConditionalWeakTable<MeleeWeapon, FarmerSprite> fakeSprites = new();
    
    internal static bool doingDualWieldCall = false;
    public static void Postfix(MeleeWeapon __instance)
    {
        if (doingDualWieldCall)
            return;

        var lastUser = __instance.lastUser;
        if (lastUser == null)
            return;

        var offhand = lastUser.GetOffhand();
        if (offhand == null)
            return;

        var fakeSpr = fakeSprites.GetOrCreateValue(offhand);
        
        doingDualWieldCall = true;
        var realSpr = lastUser.Sprite;
        lastUser.Sprite = fakeSpr;
        int[] lastCooldowns = new int[3];
        lastCooldowns[0] = MeleeWeapon.defenseCooldown;
        lastCooldowns[1] = MeleeWeapon.clubCooldown;
        lastCooldowns[2] = MeleeWeapon.daggerCooldown;
        MeleeWeapon.defenseCooldown = 0;
        MeleeWeapon.clubCooldown = 0;
        MeleeWeapon.daggerCooldown = 0;
        try
        {
            ModSnS.instance.Helper.Reflection.GetMethod(offhand, "doAnimateSpecialMove").Invoke();
        }
        finally
        {
            lastUser.Sprite = realSpr;
            MeleeWeapon.defenseCooldown = lastCooldowns[0];
            MeleeWeapon.clubCooldown = lastCooldowns[1];
            MeleeWeapon.daggerCooldown = lastCooldowns[2];
            doingDualWieldCall = false;
        }
    }
}

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.drawDuringUse), [typeof(int), typeof(int), typeof(SpriteBatch),typeof(Vector2),typeof(Farmer), typeof(string), typeof(int), typeof(bool)])]
public static class DualWieldingDrawPatch
{
    internal static bool doingDualWieldCall = false;

    public static void Postfix(//MeleeWeapon __instance,
        int frameOfFarmerAnimation,
        int facingDirection,
        SpriteBatch spriteBatch,
        Vector2 playerPosition,
        Farmer f,
        string weaponItemId,
        int type,
        bool isOnSpecial)
    {
        if (doingDualWieldCall)
            return;

        var __instance = f.CurrentTool as MeleeWeapon;

        var lastUser = __instance?.lastUser;
        if (lastUser == null)
            return;

        var offhand = lastUser.GetOffhand() as MeleeWeapon;
        if (offhand == null)
            return;
        
        doingDualWieldCall = true;
        try
        {
            offhand.drawDuringUse(frameOfFarmerAnimation, facingDirection, spriteBatch, playerPosition, f);
        }
        finally
        {
            doingDualWieldCall = false;
        }
    }
}

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage))]
public static class DualWieldingDamagePatch
{
    internal static bool doingDualWieldCall = false;

    public static void Postfix(MeleeWeapon __instance,
        GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
    {
        if (doingDualWieldCall)
            return;

        var lastUser = __instance?.lastUser;
        if (lastUser == null)
            return;

        var offhand = lastUser.GetOffhand();
        if (offhand == null)
            return;

        if (DualWieldingDaggerSpecialDetectionPatch.counter > 0 && offhand.type.Value != MeleeWeapon.dagger)
            return;

        doingDualWieldCall = true;
        Dictionary<Monster, int> oldInvincCounters = new();
        Vector2 a = default, b = default;
        Rectangle offhandRect = offhand.getAreaOfEffect(x, y, facingDirection, ref a, ref b, who.GetBoundingBox(), who.FarmerSprite.currentAnimationIndex);
        foreach (var monster in location.characters.Where(npc => npc is Monster).Cast<Monster>())
        {
            if (monster.TakesDamageFromHitbox(offhandRect))
            {
                oldInvincCounters.Add(monster, monster.invincibleCountdown);
                monster.invincibleCountdown = 0;
            }
        }
        try
        {
            offhand.DoDamage(location, x, y, facingDirection, power, who);
        }
        finally
        {
            foreach (var entry in oldInvincCounters)
            {
                entry.Key.invincibleCountdown = entry.Value;
            }
            doingDualWieldCall = false;
        }
    }
}

[HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
public static class DualWieldingOffhandParryPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
    {
        var local = ModSnS.sc.GetLocalIndexForMethod(original, "playerParryable")[0];

        List <CodeInstruction> ret = new();
        foreach (var insn in instructions)
        {
            ret.Add(insn);
            if (insn.opcode == OpCodes.Stloc && (int) insn.operand == local ||
                 insn.opcode.ToString().StartsWith("stloc.") && insn.opcode.ToString().Substring("stloc.".Length) == local.ToString() )
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

        if ( offhand != null && ( ((instance.CurrentTool as MeleeWeapon)?.isOnSpecial ?? false) || MeleeWeapon.daggerHitsLeft >= 1 ) && offhand.type.Value == MeleeWeapon.defenseSword )
        {
            return true;
        }

        return orig;
    }
}

[HarmonyPatch(typeof(MeleeWeapon), "doDaggerFunction")]
public static class DualWieldingDaggerSpecialDetectionPatch
{
    public static int counter = 0;
    public static void Prefix()
    {
        ++counter;
    }
    public static void Postfix()
    {
        --counter;
    }
}