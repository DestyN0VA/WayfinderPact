using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeverEndingAdventure.Utils;
using SpaceCore.UI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace SwordAndSorcerySMAPI.Alchemy
{
    public class AlchemyRecipesMenu : IClickableMenu
    {
        private RootElement ui;
        private Table table;
        private List<ItemWithBorder> recipes = new();

        public AlchemyRecipesMenu(FancyAlchemyMenu parent)
        : base(Game1.uiViewport.Width / 2 - 320, Game1.uiViewport.Height / 2 - 240, 640, 480, true)
        {
            ui = new();
            ui.LocalPosition = new(xPositionOnScreen, yPositionOnScreen);

            table = new()
            {
                RowHeight = 110,
                Size = new(640, 480),
            };

            List<Element> currRow = new();
            var recipes = AlchemyRecipes.Get();
            foreach (var recipe in recipes)
            {
                if (recipe.Value.UnlockConditions is not null && !GameStateQuery.CheckConditions(recipe.Value.UnlockConditions))
                    continue;

                if (recipe.Value.Ingredients.Keys.Count > 6)
                {
                    Log.Error($"Alchemy Recipe {recipe.Key} has more than 6 ingredients. Skipping");
                    continue;
                }

                CraftingRecipe fake = new("");
                fake.recipeList.Clear();
                fake.itemToProduce.Clear();
                var tmp = ItemRegistry.Create(recipe.Value.OutputItem, recipe.Value.OutputQuantity);
                fake.DisplayName = tmp.DisplayName;
                fake.description = tmp.getDescription();
                fake.recipeList.TryAddMany(recipe.Value.Ingredients);

                ItemWithBorder recipe_ = new()
                {
                    ItemDisplay = tmp,
                    LocalPosition = new Vector2(currRow.Count * 110, 0),
                    UserData = fake,
                    Callback = (e) =>
                    {
                        if (parent.ingreds.Any(slot => slot.Item != null))
                            return;
                        if (!doesFarmerHaveIngredientsInInventory(fake.recipeList))
                            return;

                        List<Item> items = [];


                        foreach (string item in recipe.Value.Ingredients.Keys)
                        {
                            for (int i = 0; i < recipe.Value.Ingredients[item]; ++i)
                            {
                                int? cat = null;
                                if (int.TryParse(item, out int cat1))
                                    cat = cat1;

                                for (int j = 0; j < Game1.player.Items.Count; ++j)
                                {
                                    var invItem = Game1.player.Items[j];
                                    if (invItem == null) continue;
                                    if (invItem.QualifiedItemId == item || cat != null && invItem.Category == cat)
                                    {
                                        if (!items.Any(i => i.QualifiedItemId == invItem.QualifiedItemId))
                                            items.Add(invItem);
                                        invItem.Stack--;
                                        if (invItem.Stack <= 0)
                                            Game1.player.Items[j] = null;
                                        break;
                                    }
                                }
                            }
                        }

                        int ingred = 0;
                        foreach (Item item in items)
                        {
                            Item item2 = item.getOne();
                            item2.Stack = recipe.Value.Ingredients.First(i => i.Key == item.QualifiedItemId).Value;
                            parent.ingreds[ingred].Item = item2;
                            ingred++;
                        }


                        parent.CheckRecipe();
                        exitThisMenu();
                    },
                };
                if (!doesFarmerHaveIngredientsInInventory(fake.recipeList))
                    recipe_.TransparentItemDisplay = true;
                currRow.Add(recipe_);
                this.recipes.Add(recipe_);
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

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        private int scrollCounter = 0;
        public override void update(GameTime time)
        {
            base.update(time);
            ui.Update();

            if (Game1.input.GetGamePadState().ThumbSticks.Right.Y != 0)
            {
                if (++scrollCounter == 5)
                {
                    scrollCounter = 0;
                    this.table.Scrollbar.ScrollBy(-Math.Sign(Game1.input.GetGamePadState().ThumbSticks.Right.Y));
                }
            }
            else scrollCounter = 0;
        }

        public static bool doesFarmerHaveIngredientsInInventory(Dictionary<string, int> recipeList)
        {
            foreach (KeyValuePair<string, int> recipe in recipeList)
            {
                int value = recipe.Value;
                value -= Game1.player.Items.CountId(recipe.Key);
                if (value <= 0) continue;

                if (recipe.Key.EqualsIgnoreCase("essence_item"))
                {
                    var itemIds = ItemRegistry.GetObjectTypeDefinition().GetAllIds().Where(i => ItemRegistry.Create($"(O){i}") is Object o && o.HasContextTag("essence_item"));
                    foreach (var id in itemIds)
                        value -= Game1.player.Items.CountId(id);
                    if (value <= 0) continue;
                }

                return false;
            }

            return true;
        }

        public override void draw(SpriteBatch b)
        {
            var menu = Game1.activeClickableMenu;
            bool check = menu == GetParentMenu();
            bool active = IsActive();

            ui.Draw(b);

            drawMouse(b);

            if (ItemWithBorder.HoveredElement != null)
            {
                var fake = ItemWithBorder.HoveredElement.UserData as CraftingRecipe;
                drawHoverText(b, " ", Game1.smallFont, boldTitleText: fake.DisplayName, craftingIngredients: fake);
            }
        }
    }
}
