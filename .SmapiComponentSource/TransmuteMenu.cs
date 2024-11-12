using StardewValley.Menus;
using StardewValley;
using SpaceCore.UI;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Object = StardewValley.Object;

namespace SwordAndSorcerySMAPI
{
    internal class TransmuteMenu : IClickableMenu
    {
        private RootElement ui;
        private List<ItemSlot> essenceTo;

        public TransmuteMenu()
            : base(Game1.uiViewport.Width / 2 - (64 * 12 + 32) / 2, Game1.uiViewport.Height / 2 - (480 + 250) / 2, 64 * 12 + 32, 480 + 250)
        {
            ui = new()
            {
                LocalPosition = new(xPositionOnScreen, yPositionOnScreen)
            };

            var container = new StaticContainer()
            {
                Size = new(this.width, this.height),
                OutlineColor = Color.White
            };
            ui.AddChild(container);

            var ingredientsButton = new Image()
            {
                Texture = ModTOP.Grimoire,
                Scale = 4,
                Callback = (e) => SetChildMenu(new TransmuteIngredients()),
                LocalPosition = new(32, 32),
            };
            container.AddChild(ingredientsButton);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);
        }

        internal List<Item> GetAllEssences()
        {
            List<Item> essences = [];
            var itemids = ItemRegistry.GetObjectTypeDefinition().GetAllIds().Where(i => (ItemRegistry.Create(i) is Object o && o.HasContextTag("essence_item")) || i == "(O)768" || i == "(O)769");

            foreach (var itemid in itemids)
            {
                essences.Add(ItemRegistry.Create(itemid));
            }
            return essences;
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            ui.Draw(b);
            drawMouse(b);
        }
    }

    internal class TransmuteIngredients : IClickableMenu
    {
        private RootElement ui;
        private List<ItemWithBorder> Essences = new();

        public TransmuteIngredients()
            : base(Game1.uiViewport.Width / 2 - 320, Game1.uiViewport.Height / 2 - 240, 640, 480, true)
        {
            ui = new();
            ui.LocalPosition = new(xPositionOnScreen, yPositionOnScreen);

            Table table = new()
            {
                RowHeight = 110,
                Size = new(640, 480),
            };

            List<Element> currRow = new();
            var items = GetAllEssences();
            foreach (var item in items)
            {
                ItemWithBorder essence_ = new()
                {
                    ItemDisplay = item,
                    LocalPosition = new(currRow.Count * 110, 0),
                    UserData = item,
                    Callback = (e) =>
                    {
                        var slot = Essences.First(l => l.ItemDisplay == item);
                        slot.TransparentItemDisplay = !slot.TransparentItemDisplay;
                    },
                };
                currRow.Add(essence_);
                this.Essences.Add(essence_);
                if (currRow.Count == 6)
                {
                    table.AddRow(currRow.ToArray());
                    currRow.Clear();
                }
            }
            if (currRow.Count != 0)
                table.AddRow(currRow.ToArray());
            ui.AddChild(table);
        }

        internal List<Item> GetAllEssences()
        {
            List<Item> essences = [];
            var itemids = ItemRegistry.GetObjectTypeDefinition().GetAllIds().Where(i => (ItemRegistry.Create(i) is Object o && o.HasContextTag("essence_item")) || i == "(O)768" || i == "(O)769");

            foreach (var itemid in itemids)
            {
                essences.Add(ItemRegistry.Create(itemid));
            }
            return essences;
        }

        public override void update(GameTime time)
        {
            base.update(time);
            ui.Update();
        }

        public override void draw(SpriteBatch b)
        {
            ui.Draw(b);

            drawMouse(b);

            if (ItemWithBorder.HoveredElement != null)
            {
                var fake = ItemWithBorder.HoveredElement.UserData as Item;
                drawHoverText(b, " ", Game1.smallFont, boldTitleText: fake.DisplayName);
            }
        }
    }
}
