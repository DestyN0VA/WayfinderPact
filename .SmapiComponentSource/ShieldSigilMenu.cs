using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using SpaceCore.UI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace SwordAndSorcerySMAPI;

public class ShieldSigilMenu : IClickableMenu
{
    private RootElement ui;
    private ItemSlot main;
    private ItemSlot[] sub = new ItemSlot[4];

    private InventoryMenu invMenu;
    
    public ShieldSigilMenu()
    :   base( Game1.uiViewport.Width / 2 - 200, Game1.uiViewport.Height / 2 - 200 - 100, 400, 400 )
    {
        List<string> choices =
        [
            "(W)DN.SnS_PaladinShield",
            "(W)DN.SnS_ArtificerShield",
            "(W)DN.SnS_DruidShield",
            "(W)DN.SnS_BardShield",
            "(W)DN.SnS_SorcererShield",
        ];

        int[] levelChecks = [0, 2, 4, 6, 8];
        
        invMenu = new(Game1.uiViewport.Width / 2 - 72 * 5 - 36 + 8, yPositionOnScreen + height, true, highlightMethod:
            (item) =>
            {
                return (item == null || choices.Contains(item.QualifiedItemId));
            });
        
        ui = new();

        StaticContainer container = new()
        {
            Size = new( 400, 400 ),
            LocalPosition = new( xPositionOnScreen, yPositionOnScreen ),
            OutlineColor = Color.White,
        };
        ui.AddChild(container);
        
        main = new()
        {
            BoxColor = Color.White,
            BoxIsThin = false,
            Callback = (elem) =>
            {
                if (main.Item != null && Game1.player.CursorSlotItem == null)
                {
                    Game1.player.CursorSlotItem = main.Item;
                    main.Item = null;
                    foreach (var slot in sub)
                        slot.Item = null;
                }
                else if (main.Item == null && choices.Contains(Game1.player.CursorSlotItem?.QualifiedItemId ?? ""))
                {
                    main.Item = Game1.player.CursorSlotItem;
                    Game1.player.CursorSlotItem = null;

                    List<string> restChoices = new(choices);
                    restChoices.Remove(main.Item.QualifiedItemId);
                    for (int i = 0; i < 4; ++i)
                    {
                        int ind = choices.IndexOf(restChoices[i]);
                        int level = levelChecks[ind];
                        if (Game1.player.GetCustomSkillLevel(ModTOP.PaladinSkill) >= level)
                            sub[i].Item = ItemRegistry.Create(restChoices[i]);
                    }
                }
            },
            ItemDisplay = ItemRegistry.Create("(W)DN.SnS_PaladinShield"),
            TransparentItemDisplay = true,
        };
        main.LocalPosition = new(200 - main.Bounds.Width / 2, 200 - main.Bounds.Height / 2);
        container.AddChild(main);

        Vector2[] offsets = new Vector2[]
        {
            new( 0, -main.Bounds.Height - 32 ),
            new( -main.Bounds.Width - 32, 0 ),
            new( main.Bounds.Width + 32, 0 ),
            new( 0, main.Bounds.Height + 32 ),
        };
        for (int i_ = 0; i_ < 4; ++i_)
        {
            int i = i_;
            sub[i] = new ItemSlot()
            {
                BoxColor = Color.White,
                BoxIsThin = false,
                Callback = (elem) =>
                {
                    if (sub[i].Item != null && Game1.player.CursorSlotItem == null)
                    {
                        Game1.player.CursorSlotItem = sub[i].Item;
                        main.Item = null;
                        foreach (var slot in sub)
                            slot.Item = null;
                    }
                }
            };
            sub[i].LocalPosition = main.LocalPosition + offsets[i];
            container.AddChild(sub[i]);
        }
    }

    public override void update(GameTime time)
    {
        base.update(time);
        ui.Update();
        invMenu.update(time);
    }

    public override void draw(SpriteBatch b)
    {
        
        IClickableMenu.drawTextureBox(b, this.invMenu.xPositionOnScreen - IClickableMenu.borderWidth, this.invMenu.yPositionOnScreen - IClickableMenu.borderWidth, this.invMenu.width + IClickableMenu.borderWidth * 2, this.invMenu.height + IClickableMenu.borderWidth * 2, Color.White);
        ui.Draw(b);
        invMenu.draw(b);

        if (ItemWithBorder.HoveredElement != null)
        {
            if (ItemWithBorder.HoveredElement is ItemSlot slot && slot.Item != null)
            {
                drawToolTip(b, slot.Item.getDescription(), slot.Item.DisplayName, slot.Item );
            }
        }
        else
        {
            var hover = invMenu.hover(Game1.getMouseX(), Game1.getMouseY(), null);
            if (hover != null)
            {
                drawToolTip(b, hover.getDescription(), invMenu.hoverTitle, hover);
            }
        }

        drawMouse(b);
        Game1.player.CursorSlotItem?.drawInMenu(b, Game1.getMousePosition().ToVector2(), 1);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        Game1.player.CursorSlotItem = this.invMenu.leftClick(x, y, Game1.player.CursorSlotItem, playSound);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        base.receiveRightClick(x, y, playSound);
        Game1.player.CursorSlotItem = this.invMenu.rightClick(x, y, Game1.player.CursorSlotItem, playSound);
    }

    protected override void cleanupBeforeExit()
    {
        base.cleanupBeforeExit();
        if (this.main.Item != null)
            Game1.player.addItemByMenuIfNecessary(this.main.Item);
    }

    public override void emergencyShutDown()
    {
        base.emergencyShutDown();
        if (this.main.Item != null)
            Game1.player.addItemByMenuIfNecessary(this.main.Item);
    }
}