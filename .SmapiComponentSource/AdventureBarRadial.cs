using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using RadialMenu;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

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
    private readonly Farmer who;

    private readonly List<AdventureBarRadialMenuItem> items = new();
    public IReadOnlyList<IRadialMenuItem> Items => items/*.Where( rmi => rmi.IsActive )*/.Cast<IRadialMenuItem>().ToList();

    public int SelectedItemIndex => -1;

    public AdventureBarRadialMenuPage(Farmer who)
    {
        this.who = who;
        var ext = who.GetFarmerExtData();
        for (int i = 0; i < ext.adventureBar.Count; ++i)
        {
            items.Add(new(who, ext.adventureBar.Fields[ i ]));
        }
    }
}

internal class AdventureBarRadialMenuItem : IRadialMenuItem
{
    private readonly Farmer who;
    private readonly NetString abilSlot;

    public AdventureBarRadialMenuItem(Farmer who, NetString abilSlot)
    {
        this.who = who;
        this.abilSlot = abilSlot;
    }

    public bool IsActive => abilSlot.Value != null;
    public Ability CurrentAbility => IsActive ? Ability.Abilities[abilSlot.Value] : null;
    public bool CanCast => IsActive ? (who.GetFarmerExtData().mana.Value >= CurrentAbility.ManaCost() && CurrentAbility.CanUse()) : false;

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
            who.GetFarmerExtData().mana.Value -= CurrentAbility.ManaCost();
            ModSnS.CastAbility(CurrentAbility);
        }

        return MenuItemActivationResult.Used;
    }
}