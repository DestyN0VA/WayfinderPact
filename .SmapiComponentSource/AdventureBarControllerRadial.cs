using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using SwordAndSorcerySMAPI.Integrations;
using System.Collections.Generic;

namespace SwordAndSorcerySMAPI;

internal class AdventureBarRadialMenuPageFactory : IRadialMenuPageFactory
{
    public IRadialMenuPage CreatePage(Farmer who)
    {
        return new AdventureBarRadialMenuPage(who);
    }
}

internal class AdventureBarRadialMenuPage : IRadialMenuPage
{
    private readonly List<IRadialMenuItem> items = [];
    public IReadOnlyList<IRadialMenuItem> Items => items;

    public int SelectedItemIndex => -1;

    public AdventureBarRadialMenuPage(Farmer who)
    {
        var ext = who.GetFarmerExtData();
        items.Add(new AdventureBarRadialMenuOpenConfigMenu());
        for (int i = 0; i < ext.adventureBar.Count; ++i)
        {
            items.Add(new AdventureBarRadialMenuItem(who, ext.adventureBar.Fields[i]));
        }
    }
}

internal class AdventureBarRadialMenuItem(Farmer who, NetString abilSlot) : IRadialMenuItem
{
    private readonly Farmer who = who;
    private readonly NetString abilSlot = abilSlot;

    public bool IsActive => abilSlot.Value != null;
    public Ability CurrentAbility => IsActive ? Ability.Abilities[abilSlot.Value] : null;
    public bool CanCast => IsActive && (who.GetFarmerExtData().mana.Value >= CurrentAbility.ManaCost() && CurrentAbility.CanUseForAdventureBar());

    public string Title => CurrentAbility?.Name() ?? I18n.EmptySlot_Title();

    public string Description => CurrentAbility?.Description().Replace( '^', '\n' ) ?? I18n.EmptySlot_Description();

    public Texture2D Texture => IsActive ? Game1.content.Load<Texture2D>(CurrentAbility.TexturePath) : Game1.staminaRect;
    public Rectangle? SourceRectangle => IsActive ? Game1.getSquareSourceRectForNonStandardTileSheet(Texture, 16, 16, CurrentAbility.SpriteIndex) : null;

    public Color? TintColor => !IsActive ? Color.Transparent : ((IsActive && !CanCast) ? (Color.Gray * 0.5f) : Color.White);

    public MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction)
    {
        if (delayedActions != DelayedActions.None)
            return MenuItemActivationResult.Delayed;

        if (CanCast)
        {
            CurrentAbility.CanUse();
            who.GetFarmerExtData().mana.Value -= CurrentAbility.ManaCost();
            ModSnS.CastAbility(CurrentAbility);
        }

        return MenuItemActivationResult.Used;
    }
}

internal class AdventureBarRadialMenuOpenConfigMenu : IRadialMenuItem
{
    public string Title => I18n.AdventureBarConfigMenu_Name();

    public string Description => I18n.AdventureBarConfigMenu_Description();

    public Texture2D? Texture => Game1.content.Load<Texture2D>("Textures/DN.SnS/SnSObjects");

    public Rectangle? SourceRectangle => new(32, 160, 16, 16);

    public MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction)
    {
        if (delayedActions != DelayedActions.None)
            return MenuItemActivationResult.Delayed;

        Game1.activeClickableMenu = new AdventureBarConfigureMenu();
        return MenuItemActivationResult.Used;
    }
}