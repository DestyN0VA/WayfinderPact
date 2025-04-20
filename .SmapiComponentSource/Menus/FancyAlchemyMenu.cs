using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeverEndingAdventure.Utils;
using SpaceCore;
using SpaceCore.UI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Objects;
using SwordAndSorcerySMAPI.Alchemy;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = StardewValley.Object;

namespace SwordAndSorcerySMAPI.Menus
{
    public class FancyAlchemyMenu : IClickableMenu
    {
        private readonly RootElement ui;
        internal readonly ItemSlot[] ingreds;
        private readonly ItemSlot output;

        private readonly Table Recipes;
        private readonly List<ItemWithBorder> RecipesList = [];

        private readonly Table Transmutations;
        private readonly List<ItemSlot> Essences = [];
        private readonly List<string> essenceIds = [];
        private readonly int TransmutationCost = 4;

        private Rectangle RecipesBounds;
        private Rectangle TransmutationsBounds;
        private int scrollCounter = 0;

        internal class Pixel(float X, float Y, Color Color, float Scale, Vector2 Velocity, Vector2 Destination, float AnimStart, Action<Pixel> EndAction, bool PlaySound = true)
        {
            public float x = X;
            public float y = Y;
            public Color color = Color;
            public float scale = Scale;
            public Vector2 velocity = Velocity;
            public Vector2 destination = Destination;
            public float animStart = AnimStart;
            public Action<Pixel> endAction = EndAction;
            public bool playSound = PlaySound;

            public void Update()
            {
                float delta = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                float ts = (float)(Game1.currentGameTime.TotalGameTime.TotalSeconds - animStart);
                if (ts < 0) ts = 0;
                float velMult = ts * ts * ts * ts * 5;
                List<Pixel> toRemove = [];
                Vector2 center = destination;
                if (ts >= 1.4 && playSound)
                {
                    playSound = false;
                    Game1.playSound("spacechase0.MageDelve_alchemy_synthesize");
                }
                float actualScale = (scale + MathF.Sin(ts * 3) - 3) % 3 + 3;

                Vector2 ppos = new Vector2(x, y) + velocity * delta;
                x = ppos.X;
                y = ppos.Y;
                Vector2 toCenter = center - ppos;
                float dist = Vector2.Distance(center, ppos);
                velocity = velocity * 0.99f + toCenter / dist * velMult;

                if ((dist < 24 || float.IsNaN(dist)) && animStart + 1 <= Game1.currentGameTime.TotalGameTime.TotalSeconds && !playSound)
                    endAction(this);
            }

            public void Draw(SpriteBatch b)
            {
                float ts = (float)(Game1.currentGameTime.TotalGameTime.TotalSeconds - animStart);
                if (ts < 0) ts = 0;
                float actualScale = (scale + MathF.Sin(ts * 3) - 3) % 3 + 3;
                b.Draw(Game1.staminaRect, new Vector2(x, y), null, color, 0, Vector2.Zero, actualScale, SpriteEffects.None, 1);
            }
        }
        private readonly List<Pixel> pixels = [];

        public FancyAlchemyMenu()
        : base(Game1.uiViewport.Width / 2 - (64 * 12 + 32) / 2, Game1.uiViewport.Height / 2 - 520 / 2, 64 * 12 + 32, 560)
        {
            ui = new RootElement()
            {
                LocalPosition = new Vector2(xPositionOnScreen, yPositionOnScreen - 24),
            };

            Vector2 basePoint = new(width / 2, height / 2 + 24.5f);

            Recipes = new()
            {
                LocalPosition = new Vector2(basePoint.X - width / 2 - 184, basePoint.Y - height / 2 + 24.5f),
                RowHeight = 112,
                Size = new Vector2(96, height)
            };

            Transmutations = new()
            {
                LocalPosition = new Vector2(basePoint.X + width / 2 + 88, basePoint.Y - height / 2 + 24.5f),
                RowHeight = 112,
                Size = new Vector2(96, height)
            };

            output = new ItemSlot()
            {
                LocalPosition = basePoint,
                TransparentItemDisplay = true,
                Callback = (e) =>
                {
                    if (output.Item == null)
                        DoCrafting();
                    else
                    {
                        Game1.player.addItemByMenuIfNecessary(output.Item);
                        output.Item = null;
                    }
                }
            };

            output.LocalPosition -= new Vector2(output.Width / 2, output.Height / 2);

            ingreds = new ItemSlot[6];
            for (int i = 0; i < 6; ++i)
            {
                ingreds[i] = new ItemSlot()
                {
                    LocalPosition = basePoint +
                                    new Vector2(MathF.Cos(3.14f * 2 / 6 * i) * 200,
                                                 MathF.Sin(3.14f * 2 / 6 * i) * 200) +
                                    -new Vector2(output.Width / 2, output.Height / 2)
                };
                ui.AddChild(ingreds[i]);
            };

            ui.AddChild(Recipes);
            ui.AddChild(Transmutations);
            ui.AddChild(output);

            PopulateTables(Recipes, Transmutations);
            SetupScrollBounds();

            Transmutations.Scrollbar.LocalPosition = new Vector2(-Transmutations.Width + 24, 0);

            if (Game1.player.HasCustomProfession(SorcerySkill.ProfessionEssenceDrops))
                TransmutationCost = 2;
            if (Game1.player.HasCustomProfession(SorcerySkill.ProfessionPhilosopherStone))
                TransmutationCost = 1;
        }

        private void PopulateTables(Table Recipes, Table Trans)
        {
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
                fake.itemToProduce = [recipe.Value.OutputItem];
                fake.numberProducedPerCraft = recipe.Value.OutputQuantity;
                fake.recipeList.TryAddMany(recipe.Value.Ingredients);

                ItemWithBorder recipe_ = new()
                {
                    ItemDisplay = tmp,
                    UserData = fake,
                    Callback = (e) =>
                    {
                        TryPlaceIngredients(fake);
                    },
                };
                if (!fake.doesFarmerHaveIngredientsInInventory(GetAdditionalMaterials()))
                    recipe_.TransparentItemDisplay = true;
                RecipesList.Add(recipe_);
                Recipes.AddRow([recipe_]);
            }

            var items = GetAllEssences();
            foreach (var essence in items)
            {
                ItemSlot essence_ = new()
                {
                    ItemDisplay = essence,
                    //LocalPosition = new(16 + 110, 110 * i + 32 * i),
                };
                essence_.Callback = (e) =>
                {
                    if (!essence_.TransparentItemDisplay)
                    {
                        if (!DoesPlayerHaveSpaceFor(essence))
                        {
                            Game1.showRedMessageUsingLoadString("Strings/StringsFromCSFiles:Crop.cs.588");
                        }
                        else
                        {
                            DoTransmutation(essence, essence_);
                        }
                    }
                };
                Trans.AddRow([essence_]);
                Essences.Add(essence_);
            }
        }

        private void TryPlaceIngredients(CraftingRecipe fake)
        {
            //New

            if (!fake.doesFarmerHaveIngredientsInInventory(GetAdditionalMaterials()))
                return;

            if (output.Item != null)
            {
                Game1.player.addItemByMenuIfNecessary(output.Item);
                output.Item = null;
            }

            bool returnItems = false;
            if (ingreds.Where(i => i.Item != null).Count() != fake.recipeList.Count)
                returnItems = true;

            for (int i = 0; i < ingreds.Length; i++)
            {
                var ingred = ingreds[i];
                if (ingred.Item == null || fake.recipeList.ContainsKey(ingred.Item.QualifiedItemId))
                    continue;
                returnItems = true;
                break;
            }

            if (returnItems)
            {
                foreach (var ingred in ingreds.Where(i => i.Item != null))
                {
                    Game1.player.addItemByMenuIfNecessary(ingred.Item);
                    ingred.Item = null;
                }
                output.ItemDisplay = null;
            }

            if (!AnySpaceToCraftMore(fake))
                return;

            List<Item> Ingreds = [];
            List<Chest> Chests = GetNearbyChests();

            void TryAddItemElseIncreaseStack(Item item)
            {
                if (!Ingreds.Any(i => i is not null && i.QualifiedItemId == item.QualifiedItemId))
                    Ingreds.Add(item.getOne());
                else
                    Ingreds.First(i => i.QualifiedItemId == item.QualifiedItemId).Stack++;
            }

            foreach (string item in fake.recipeList.Keys)
            {
                for (int i = 0; i < fake.recipeList[item]; i++)
                {
                    if (Game1.player.Items.Any(i => i is not null && i.QualifiedItemId == item))
                    {
                        var Item = Game1.player.Items.First(i => i is not null && i.QualifiedItemId == item);

                        TryAddItemElseIncreaseStack(Item);

                        Item.Stack--;
                        if (Item.Stack <= 0)
                            Game1.player.Items[Game1.player.Items.IndexOf(Item)] = null;

                        continue;
                    }
                    else
                    {
                        var chest = Chests.First(c => c.Items.Any(i => i is not null && i.QualifiedItemId == item));
                        var Item = chest.Items.First(i => i is not null && i.QualifiedItemId == item);

                        TryAddItemElseIncreaseStack(Item);

                        Item.Stack--;
                        if (Item.Stack <= 0)
                            chest.Items[chest.Items.IndexOf(Item)] = null;

                        continue;
                    }
                }
            }

            foreach (var ingred in Ingreds)
            {
                if (ingreds.Any(i => i.Item is not null && i.Item.QualifiedItemId == ingred.QualifiedItemId))
                {
                    ingreds.First(i => i.Item is not null && i.Item.QualifiedItemId == ingred.QualifiedItemId).Item.Stack += ingred.Stack;
                }
                else
                {
                    ingreds.First(i => i.Item is null).Item = ingred;
                }
            }

            if (output.ItemDisplay == null)
            {
                output.ItemDisplay = ItemRegistry.Create(fake.itemToProduce.First());
                output.ItemDisplay.Stack = fake.numberProducedPerCraft;
            }
            else
            {
                output.ItemDisplay.Stack += fake.numberProducedPerCraft;
            }
        }

        private bool AnySpaceToCraftMore(CraftingRecipe fake)
        {
            if (!ingreds.Any(i => i.Item is not null))
                return true;

            if (output.ItemDisplay.Stack + fake.numberProducedPerCraft > output.ItemDisplay.maximumStackSize())
                return false;

            foreach (var ingred in fake.recipeList)
            {
                var slot = ingreds.FirstOrDefault(i => i.Item.QualifiedItemId == ingred.Key);
                if (slot == default) return false;
                else if (slot.Item.Stack + ingred.Value > slot.Item.maximumStackSize()) return false;
            }

            return true;
        }

        private int GetCraftableCountFor(CraftingRecipe recipe = null, Item essence = null)
        {
            if (recipe == null && essence == null)
                return 0;

            if (recipe != null)
                return recipe.getCraftableCount(GetNearbyChests());
            else
            {
                List<string> localEssenceIds = [];
                localEssenceIds.AddRange(essenceIds);
                localEssenceIds.Remove(essence.QualifiedItemId);

                int x = 0;
                foreach (var item in Game1.player.Items.Where(i => i is not null))
                {
                    if (localEssenceIds.Contains(item.QualifiedItemId))
                    {
                        x += item.Stack;
                    }
                }

                foreach (Chest c in GetNearbyChests())
                    foreach (var item in c.Items.Where(o => o is not null))
                        if (localEssenceIds.Contains(item.QualifiedItemId))
                            x += item.Stack;

                return x / TransmutationCost;
            }
        }

        private List<Item> GetAllEssences()
        {
            List<Item> essences = [];
            var itemids = ItemRegistry.GetObjectTypeDefinition().GetAllIds().Where(i => i == "768" || i == "769" || ItemRegistry.Create($"(O){i}") is Object o && o.HasContextTag("essence_item"));

            foreach (var itemid in itemids)
            {
                essences.Add(ItemRegistry.Create($"(O){itemid}"));
                if (!essenceIds.Contains($"(O){itemid}"))
                {
                    essenceIds.Add($"(O){itemid}");
                }
            }
            return essences;
        }

        private void DoTransmutation(Item essence, ItemSlot itemSlot)
        {
            List<string> localEssenceIds = [];
            foreach (string essenceId in essenceIds)
            {
                localEssenceIds.Add(essenceId.ToLower());
            }
            localEssenceIds.Remove(essence.QualifiedItemId.ToLower());

            for (int i = 0; i < TransmutationCost; ++i)
            {
                for (int j = 0; j < Game1.player.Items.Count; ++j)
                {
                    var invItem = Game1.player.Items[j];
                    if (invItem == null) continue;

                    if (localEssenceIds.Contains(invItem.QualifiedItemId.ToLower()))
                    {
                        invItem.Stack--;
                        if (invItem.Stack <= 0)
                            Game1.player.Items[j] = null;
                        break;
                    }
                }
            }
            Pixelize(itemSlot, Utility.PointToVector2(itemSlot.Bounds.Center), (float)Game1.currentGameTime.TotalGameTime.TotalSeconds, (p) => { pixels.Remove(p); });
            Game1.playSound("spacechase0.MageDelve_alchemy_particlize");

            Game1.player.addItemToInventory(essence.getOne());
        }

        private static bool DoesPlayerHaveSpaceFor(Item item)
        {
            if (Game1.player.freeSpotsInInventory() > 0)
                return true;
            else if (Game1.player.Items.Any(i => i.canStackWith(item) && i.Stack < 1000 - item.Stack))
                return true;
            return false;
        }

        private static List<Chest> GetNearbyChests()
        {
            List<Chest> chests = [];
            Rectangle Rect = new(Game1.player.TilePoint + new Point(-2, -2), new Point(5, 5));

            foreach (var kvp in Game1.currentLocation.Objects.Pairs)
                if (kvp.Value is Chest c && Rect.Contains(kvp.Key))
                    chests.Add(c);

            Vector2 Pos = Game1.player.Tile;

            chests.Sort((a, b) =>
            {
                if (Vector2.Distance(a.TileLocation, Pos) < Vector2.Distance(b.TileLocation, Pos))
                    return 1;
                else if (Vector2.Distance(a.TileLocation, Pos) > Vector2.Distance(b.TileLocation, Pos))
                    return -1;
                else return 0;
            });

            return chests;
        }

        private static IList<Item> GetAdditionalMaterials() => GetNearbyChests().SelectMany(c => c.Items.Where(i => i is not null)).ToList();

        private void Pixelize(ItemSlot slot, Vector2 Dest, float animStart, Action<Pixel> action, bool playSound = true)
        {
            if (slot.Item is not Object && slot.ItemDisplay is not Object)
                return;
            string id = slot.Item != null ? slot.Item.QualifiedItemId : slot.ItemDisplay.QualifiedItemId;
            var tex = ItemRegistry.GetData(id).GetTexture();
            var rect = ItemRegistry.GetData(id).GetSourceRect();

            var cols = new Color[16 * 16];
            tex.GetData(0, rect, cols, 0, cols.Length);

            for (int i = 0; i < cols.Length; ++i)
            {
                int ix = i % 16;
                int iy = i / 16;

                float velDir = (float)Game1.random.NextDouble() * 3.14f * 2;
                Vector2 vel = new Vector2(MathF.Cos(velDir), MathF.Sin(velDir)) * (60 + Game1.random.Next(70));
                pixels.Add(new(
                    X: slot.Bounds.Location.X + 16 + ix * Game1.pixelZoom,
                    Y: slot.Bounds.Location.Y + 16 + iy * Game1.pixelZoom,
                    Color: cols[i],
                    Scale: 3 + (float)Game1.random.NextDouble() * 3,
                    Velocity: vel,
                    Destination: Dest,
                    AnimStart: animStart,
                    EndAction: action,
                    PlaySound: playSound
                    ));
                playSound = false;
            }
        }

        private void DoCrafting()
        {
            if (output.Item == null && output.ItemDisplay != null && !pixels.Any(p => p.destination == Utility.PointToVector2(output.Bounds.Center)))
            {
                bool play = true;
                foreach (var ingred in ingreds)
                {
                    Pixelize(ingred, Utility.PointToVector2(output.Bounds.Center), (float)Game1.currentGameTime.TotalGameTime.TotalSeconds, (p) =>
                    {
                        pixels.Remove(p);
                        if (!pixels.Any(p1 => p1.destination == p.destination))
                        {
                            if (output.Item == null && output.ItemDisplay != null)
                            {
                                output.Item = output.ItemDisplay;
                                output.ItemDisplay = null;
                            }
                        }
                    }, play);
                    play = false;
                }
                Game1.playSound("spacechase0.MageDelve_alchemy_particlize");
                foreach (var ingred in ingreds)
                    ingred.Item = null;
            }
        }

        private void SetupScrollBounds()
        {
            TransmutationsBounds = Transmutations.Bounds;
            TransmutationsBounds.Inflate(32, 32);
            RecipesBounds = Recipes.Bounds;
            RecipesBounds.Inflate(32, 32);
        }

        protected override void cleanupBeforeExit()
        {
            List<Item> itemsToAdd = [];
            if (output.Item != null)
                itemsToAdd.Add(output.Item);
            foreach (var ingred in ingreds)
                if (ingred.Item != null)
                    itemsToAdd.Add(ingred.Item);

            if (itemsToAdd.Count > 0)
                Game1.player.addItemsByMenuIfNecessary(itemsToAdd);
        }

        public override bool overrideSnappyMenuCursorMovementBan() => true;

        public override void receiveScrollWheelAction(int direction)
        {
            if (TransmutationsBounds.Contains(Game1.getMousePosition()))
                Transmutations.Scrollbar.ScrollBy(direction / -120);
            if (RecipesBounds.Contains(Game1.getMousePosition()))
                Recipes.Scrollbar.ScrollBy(direction / -120);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            ui.Update();

            //Controller Scrolling for the alchemy recipes and transmutations side menus
            if (Game1.input.GetGamePadState().ThumbSticks.Right.Y != 0)
            {
                int ScrollBy = (int)Game1.input.GetGamePadState().ThumbSticks.Right.Y;
                if (TransmutationsBounds.Contains(Game1.getMousePosition()))
                {
                    if (++scrollCounter == 5)
                    {
                        scrollCounter = 0;
                        Transmutations.Scrollbar.ScrollBy(-Math.Sign(ScrollBy));
                    }
                }
                else if (RecipesBounds.Contains(Game1.getMousePosition()))
                {
                    if (++scrollCounter == 5)
                    {
                        scrollCounter = 0;
                        Recipes.Scrollbar.ScrollBy(-Math.Sign(ScrollBy));
                    }
                }
            }
            else scrollCounter = 0;

            //Particle magic
            for (int i = 0; i < pixels.Count; i++)
                pixels[i].Update();
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
            ui.Draw(b);

            //Particle magic
            for (int i = 0; i < pixels.Count; i++)
                pixels[i].Draw(b);

            //"Recipes" and "Transmutation" labels
            SpriteText.drawStringWithScrollCenteredAt(b, I18n.AlchRecipes_Label(), (int)Recipes.Position.X + Recipes.Width / 2, (int)Recipes.Position.Y - 100, I18n.Transmutation_Label());
            SpriteText.drawStringWithScrollCenteredAt(b, I18n.Transmutation_Label(), (int)Transmutations.Position.X + Transmutations.Width / 2, (int)Transmutations.Position.Y - 100);

            //Craftable Count for Essences
            foreach (var essence in Essences)
            {
                if (essence.Position.Y + essence.Height + 32 > Transmutations.Position.Y + Transmutations.Height)
                    continue;
                else if (essence.Position.Y < Transmutations.Position.Y)
                    continue;
                else
                {
                    int Count = GetCraftableCountFor(essence: essence.ItemDisplay);
                    Utility.drawTextWithShadow(b, $"{Count}", Game1.smallFont, essence.Position + new Vector2(essence.Width / 2 - Game1.smallFont.MeasureString($"{Count}").X / 2, essence.Height + 4), Game1.textColor);
                    if (essence.TransparentItemDisplay && Count > 0)
                        essence.TransparentItemDisplay = false;
                    else if (!essence.TransparentItemDisplay && Count <= 0)
                        essence.TransparentItemDisplay = true;
                }
            }

            //Craftable Count for Recipes
            foreach (var recipe in RecipesList)
            {
                if (recipe.Position.Y + recipe.Height + 32 > Recipes.Position.Y + Recipes.Height)
                    continue;
                else if (recipe.Position.Y < Transmutations.Position.Y)
                    continue;
                else
                {
                    int Count = GetCraftableCountFor(recipe: recipe.UserData as CraftingRecipe);
                    Utility.drawTextWithShadow(b, $"{Count}", Game1.smallFont, recipe.Position + new Vector2(recipe.Width / 2 - Game1.smallFont.MeasureString($"{Count}").X / 2, recipe.Height + 4), Game1.textColor);
                    if (recipe.TransparentItemDisplay && Count > 0)
                        recipe.TransparentItemDisplay = false;
                    else if (!recipe.TransparentItemDisplay && Count <= 0)
                        recipe.TransparentItemDisplay = true;
                }
            }

            //Tooltips
            if (ItemWithBorder.HoveredElement != null)
            {
                if (ItemWithBorder.HoveredElement is ItemSlot slot && slot.Item != null)
                    //Normal Item Tooltips
                    drawToolTip(b, slot.Item.getDescription(), slot.Item.DisplayName, slot.Item);
                else if (ItemWithBorder.HoveredElement.ItemDisplay != null)
                {
                    if (ItemWithBorder.HoveredElement.UserData != null)
                    {
                        //Alchemy Recipe Tooltips with ingredients
                        var fake = ItemWithBorder.HoveredElement.UserData as CraftingRecipe;
                        drawHoverText(b, " ", Game1.smallFont, boldTitleText: fake.DisplayName, craftingIngredients: fake, additional_craft_materials: GetAdditionalMaterials());
                    }
                    else
                        //Again Normal Item Tooltips but for ItemSlot.ItemDisplay instead of ItemSlot.Item
                        drawToolTip(b, ItemWithBorder.HoveredElement.ItemDisplay.getDescription(), ItemWithBorder.HoveredElement.ItemDisplay.DisplayName, ItemWithBorder.HoveredElement.ItemDisplay);
                }
            }

            //Draw the mouse
            drawMouse(b);
        }
    }
}
