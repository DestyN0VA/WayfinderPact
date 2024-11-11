using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeverEndingAdventure.Utils;
using SpaceCore;
using SpaceCore.UI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace SwordAndSorcerySMAPI.Alchemy
{
    public class AlchemyRecipesMenu : IClickableMenu
    {
        private RootElement ui;
        private List<ItemWithBorder> recipes = new();

        public AlchemyRecipesMenu()
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
            var recipes = AlchemyRecipes.Get();
            foreach (var recipe in recipes)
            {
                if (recipe.Value.UnlockConditions is not null && !GameStateQuery.CheckConditions(recipe.Value.UnlockConditions))
                    continue;

                int x = 0;
                foreach (string item in recipe.Value.Ingredients.Keys)
                {
                    x += recipe.Value.Ingredients[item];
                }
                if (x > 6)
                {
                    Log.Warn($"Alchemy Recipe {recipe.Key} has more than 6 ingredients. Skipping");
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
                        var parent = GetParentMenu() as FancyAlchemyMenu;
                        if (parent == null) return;

                        if (parent.ingreds.Any(slot => slot.Item != null))
                            return;
                        if (!doesFarmerHaveIngredientsInInventory(fake.recipeList))
                            return;

                        List<Item> items = [];

                        foreach (string item in recipe.Value.Ingredients.Keys)
                        {
                            for (int i = 0; i < recipe.Value.Ingredients[item]; ++i)
                            {
                                if (parent.ingreds[i].Item != null) continue;
                                int? cat = null;
                                if (int.TryParse(item, out int cat1))
                                    cat = cat1;

                                for (int j = 0; j < Game1.player.Items.Count; ++j)
                                {
                                    var invItem = Game1.player.Items[j];
                                    if (invItem == null) continue;

                                    
                                    if (invItem.QualifiedItemId == item || cat != null && invItem.Category == cat || item == "essence_item" && invItem.HasContextTag(item))
                                    {
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
                            parent.ingreds[ingred].Item = item.getOne();
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

        public override void update(GameTime time)
        {
            base.update(time);
            ui.Update();
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
