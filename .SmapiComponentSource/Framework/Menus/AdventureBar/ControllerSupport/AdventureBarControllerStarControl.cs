using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StarControl;
using StardewValley;
using SwordAndSorcerySMAPI.Framework.Abilities;
using System.Collections.Generic;

namespace SwordAndSorcerySMAPI.Framework.Menus.AdventureBar.ControllerSupport;

internal class AdventureBarStarControlPageFactory : IRadialMenuPageFactory
{

    public IRadialMenuPage CreatePage(Farmer who)
    {
        return new AdventureBarStarControlPage(who);
    }
}

internal class AdventureBarStarControlPage : IRadialMenuPage
{
    private readonly List<IRadialMenuItem> items = [];
    public int SelectedItemIndex => -1;

    public IReadOnlyList<IRadialMenuItem> Items => items;

    public AdventureBarStarControlPage(Farmer who)
    {
        var ext = who.GetFarmerExtData();
        items.Add(new AdventureBarStarControlOpenConfigMenu());

        for (int i = 0; i < ext.adventureBar.Count; ++i)
        {
            items.Add(new AdventureBarStarControlItem(who, ext.adventureBar.Fields[i]));
        }
    }
}

internal class AdventureBarStarControlItem(Farmer who, NetString abilSlot) : IRadialMenuItem
{
    private readonly Farmer who = who;
    private readonly NetString abilSlot = abilSlot;

    public bool IsActive => abilSlot.Value != null;
    public Abilities.Ability CurrentAbility => IsActive ? Abilities.Ability.Abilities[abilSlot.Value] : null;
    public bool CanCast => IsActive && CurrentAbility.ManaCost() <= who.GetFarmerExtData().mana.Value && CurrentAbility.CanUseForAdventureBar();

    public string Title => CurrentAbility?.Name() ?? I18n.EmptySlot_Title();

    public string Description => CurrentAbility?.Description().Replace('^', '\n') ?? I18n.EmptySlot_Description();

    public Texture2D Texture => IsActive ? Game1.content.Load<Texture2D>(CurrentAbility.TexturePath) : Game1.staminaRect;
    public Rectangle? SourceRectangle => IsActive ? Game1.getSquareSourceRectForNonStandardTileSheet(Texture, 16, 16, CurrentAbility.SpriteIndex) : null;

    public Color? TintColor => !IsActive ? Color.Transparent : IsActive && !CanCast ? Color.Gray * 0.5f : Color.White;

    public ItemActivationResult Activate(Farmer who, DelayedActions delayedActions, ItemActivationType activationType = ItemActivationType.Primary)
    {
        if (delayedActions != DelayedActions.None)
            return ItemActivationResult.Delayed;

        if (CanCast)
        {
            CurrentAbility.CanUse();
            ModSnS.CastAbility(CurrentAbility);
        }

        return ItemActivationResult.Used;
    }
}

internal class AdventureBarStarControlOpenConfigMenu : IRadialMenuItem
{
    public string Title => I18n.AdventureBarConfigMenu_Name();

    public string Description => I18n.AdventureBarConfigMenu_Description();

    public Texture2D Texture => Game1.content.Load<Texture2D>("Textures/DN.SnS/SnSObjects");

    public Rectangle? SourceRectangle => new(32, 160, 16, 16);

    public ItemActivationResult Activate(Farmer who, DelayedActions delayedActions, ItemActivationType activationType = ItemActivationType.Primary)
    {
        if (delayedActions != DelayedActions.None)
            return ItemActivationResult.Delayed;

        Game1.activeClickableMenu = new AdventureBarConfigureMenu();
        return ItemActivationResult.Used;
    }
}