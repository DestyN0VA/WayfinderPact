using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace SwordAndSorcerySMAPI;

public static class DualWieldExtensions
{
    public static MeleeWeapon GetOffhand(this Farmer farmer)
    {
        return ModSnS.SpaceCore.GetItemInEquipmentSlot(farmer, $"{ModSnS.Instance.ModManifest.UniqueID}_Offhand") as MeleeWeapon ?? null;
    }
}

/*[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.animateSpecialMove))]
public static class DualWieldingSpecialMovePatch
{
    public static void Postfix(MeleeWeapon __instance)
    {
        if (!__instance.lastUser.IsLocalPlayer || __instance != Game1.player.CurrentTool)
            return;

        var offhand = __instance.lastUser.GetOffhand();
        if (offhand == null)
            return;

        offhand.animateSpecialMove(__instance.lastUser);

        if (offhand.type.Value == __instance.type.Value)
            return;

        if (offhand.IsShieldItem())
        {
            ModSnS.State.ThrowCooldown = GetActiveWeaponCooldown(__instance);
        }
        else
        {
            int cooldown = (int)GetActiveWeaponCooldown(__instance);
            switch (offhand.type.Value)
            {
                case MeleeWeapon.dagger:
                    MeleeWeapon.daggerCooldown = cooldown;
                    break;
                case MeleeWeapon.club:
                    MeleeWeapon.clubCooldown = cooldown;
                    break;
                case MeleeWeapon.stabbingSword:
                    MeleeWeapon.attackSwordCooldown = cooldown;
                    break;
                case MeleeWeapon.defenseSword:
                    MeleeWeapon.defenseCooldown = cooldown;
                    break;
            }
        }
    }

    public static float GetActiveWeaponCooldown(MeleeWeapon weapon)
    {
        if (weapon.IsShieldItem()) return ModSnS.State.ThrowCooldown;
        else return weapon.type.Value switch
        {
            MeleeWeapon.dagger => MeleeWeapon.daggerCooldown,
            MeleeWeapon.club => MeleeWeapon.clubCooldown,
            MeleeWeapon.defenseSword => MeleeWeapon.defenseSword,
            MeleeWeapon.stabbingSword => MeleeWeapon.attackSwordCooldown,
            _ => 0
        };
    }
}*/

[HarmonyPatch(typeof(MeleeWeapon), "doAnimateSpecialMove")]
static class DualWieldingSpecialMovePatch
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
                new(OpCodes.Call, AccessTools.Method(typeof(DualWieldingSpecialMovePatch), nameof(DoCheck))),
                new(OpCodes.Brtrue, operand)
                ]);

        return match.Instructions();
    }

    public static bool DoCheck(MeleeWeapon w)
    {
        return (w == w.lastUser.CurrentTool || w == w.lastUser.GetOffhand());
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
/*
[HarmonyPatch(typeof(Game1), nameof(Game1.drawTool), [typeof(Farmer), typeof(int)])]
public static class DualWieldingDrawDuringUseOffhandPatch
{
    internal static Vector2[] offsets = [new(0, -16), new(-16, 0), new(0, 16), new(16, 0)];

    static void Postix(Farmer f)
    {
        MeleeWeapon Offhand = f.GetOffhand();

        if (Offhand == null || f.CurrentTool is not MeleeWeapon mw || mw.isScythe() || mw.IsShieldItem() || Offhand.IsShieldItem())
            return;

        Vector2 fPosition = f.getLocalPosition(Game1.viewport) + f.jitter + f.armOffset + offsets[f.FacingDirection];
        FarmerSprite farmerSprite = (FarmerSprite)f.Sprite;

        if (f.CurrentTool is MeleeWeapon weapon)
        {
            weapon.drawDuringUse(farmerSprite.currentAnimationIndex, f.FacingDirection, Game1.spriteBatch, fPosition, f);
            return;
        }
        if (f.FarmerSprite.isUsingWeapon())
        {
            MeleeWeapon.drawDuringUse(farmerSprite.currentAnimationIndex, f.FacingDirection, Game1.spriteBatch, fPosition, f, Offhand.CurrentParentTileIndex.ToString(), f.FarmerSprite.getWeaponTypeFromAnimation(), isOnSpecial: false);
            return;
        }
    }
}*/

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage))]
static class DualWieldingDamagePatchTest
{
    readonly static Dictionary<Monster, int> origInvinc = [];

    static void Prefix(MeleeWeapon __instance, GameLocation location)
    {
        if (__instance != __instance.lastUser.CurrentTool || __instance.isScythe())
            return;

        origInvinc.Clear();
        foreach (Monster m in location.characters.Where(npc => npc is Monster).Cast<Monster>())
            origInvinc.Add(m, m.invincibleCountdown);
    }

    static void Postfix(MeleeWeapon __instance, GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
    {
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
            spriteBatch.Draw(texture, location + ((__instance.type.Value == 1) ? new Vector2(38f, 25f) : new Vector2(32f, 32f)), sourceRect, color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);
            if (coolDownLevel > 0f && drawShadow && !drawing_as_debris && !__instance.isScythe() && (Game1.activeClickableMenu == null || Game1.activeClickableMenu is not ShopMenu || scaleSize != 1f))
            {
                spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)location.X, (int)location.Y + (64 - (int)(coolDownLevel * 64f)), 64, (int)(coolDownLevel * 64f)), Color.Red * 0.66f);
            }
            __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
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
/*
[HarmonyPatch(typeof(MeleeWeapon), "doAnimateSpecialMove")]
public static class DualWieldingSpecialMovePatch
{
    internal static ConditionalWeakTable<MeleeWeapon, FarmerSprite> fakeSprites = [];

    public static bool CanSwitch = true;
    internal static bool doingDualWieldCall = false;

    public static void Prefix()
    {
        CanSwitch = false;
    }

    public static void Postfix(MeleeWeapon __instance)
    {
        DelayedAction.functionAfterDelay(() => CanSwitch = true, MeleeWeapon.defenseCooldown);

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
        int[] lastCooldowns =
        [
            MeleeWeapon.defenseCooldown,
            MeleeWeapon.clubCooldown,
            MeleeWeapon.daggerCooldown,
        ];
        MeleeWeapon.defenseCooldown = 0;
        MeleeWeapon.clubCooldown = 0;
        MeleeWeapon.daggerCooldown = 0;
        try
        {
            ModSnS.Instance.Helper.Reflection.GetMethod(offhand, "doAnimateSpecialMove").Invoke();
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
        Farmer f)
    {
        if (f != Game1.player) return;

        Vector2 playerPosition = f.getLocalPosition(Game1.viewport) + f.jitter + f.armOffset;
        if (doingDualWieldCall)
            return;

        var __instance = f.CurrentTool as MeleeWeapon;
        if ((__instance.GetData()?.CustomFields?.ContainsKey("DN.SnS_Shield") ?? false))
            return;

        var lastUser = __instance?.lastUser;
        if (lastUser == null)
            return;

        var offhand = lastUser.GetOffhand();
        if (offhand == null || (offhand.GetData()?.CustomFields?.ContainsKey("DN.SnS_Shield") ?? false))
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

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.triggerClubFunction))]
public static class ClubRemoveIsOnSpecial
{
    public static void Postfix(MeleeWeapon __instance)
    {
        __instance.isOnSpecial = false;
    }
}*/
