using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;

namespace SwordAndSorcerySMAPI;

public static class DualWieldExtensions
{
    public static MeleeWeapon GetOffhand(this Farmer farmer)
    {
        return ModSnS.sc.GetItemInEquipmentSlot(farmer, $"{ModSnS.instance.ModManifest.UniqueID}_Offhand") as MeleeWeapon;
    }
}

// TODO: Transpile calls for checking sword guarding so we can have offhand block correctly
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
        try
        {
            ModSnS.instance.Helper.Reflection.GetMethod(offhand, "doAnimateSpecialMove").Invoke();
        }
        finally
        {
            lastUser.Sprite = realSpr;
            doingDualWieldCall = false;
        }
    }
}

// TODO: Actually affect things, not just drawing here
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

        var offhand = lastUser.GetOffhand();
        if (offhand == null)
            return;
        
        doingDualWieldCall = true;
        try
        {
            MeleeWeapon.drawDuringUse(frameOfFarmerAnimation, facingDirection, spriteBatch, playerPosition, f, offhand.QualifiedItemId, offhand.type, isOnSpecial);
        }
        finally
        {
            doingDualWieldCall = false;
        }
    }
}