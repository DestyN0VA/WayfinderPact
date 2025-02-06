using StardewValley.Menus;
using StardewValley;
using StardewValley.Extensions;
using SpaceCore.UI;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Object = StardewValley.Object;
using System;
using SpaceCore;
using StardewValley.BellsAndWhistles;

namespace SwordAndSorcerySMAPI
{
    internal class TransmuteMenu : IClickableMenu
    {
        private RootElement ui;
        private List<ItemWithBorder> Essences = [];
        private List<Label> TransmutableLabel = [];
        private int TransmutationCost = 4;
        public List<string> essenceIds = [];

        public TransmuteMenu()
            : base(Game1.uiViewport.Width / 2 - 724 / 2, Game1.uiViewport.Height / 2 - 480 / 2, 724, 480)
        {
            if (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionEssenceDrops))
            {
                TransmutationCost = 2;
            }
            if (Game1.player.HasCustomProfession(WitchcraftSkill.ProfessionPhilosopherStone))
            {
                TransmutationCost = 1;
            }

            ui = new()
            {
                LocalPosition = new(xPositionOnScreen, yPositionOnScreen)
            };

            ScrollContainer table = new()
            {
                Size = new(width - 64, height - 16),
                LocalPosition = new(32, 32)
            };

            var items = GetAllEssences();
            int w = 0;
            int h = 0;
            foreach (var item in items)
            {
                ItemWithBorder essence_ = new()
                {
                    ItemDisplay = item,
                    LocalPosition = new(16 + 110 * w, 110 * h + 32 * h)
                };
                essence_.Callback = (e) =>
                {
                    if (!essence_.TransparentItemDisplay)
                    {
                        if (!DoesPlayerHaveSpaceForEssence(item))
                        {
                            Game1.showRedMessageUsingLoadString("Strings/StringsFromCSFiles:Crop.cs.588");
                        }
                        else
                        {
                            DoTransmutation(item);
                        }
                    }
                };
                table.AddChild(essence_);
                Essences.Add(essence_);
                Label essencelabel_ = new()
                {
                    String = GetTransmutableCountFor(item).ToString(),
                    Font = Game1.smallFont,
                    UserData = essence_,
                    LocalPosition = new(48 + 110 * w, 110 * (h + 1) + 32 * h)
                };
                table.AddChild(essencelabel_);
                TransmutableLabel.Add(essencelabel_);
                
                w++;
                if ((items.Count < 6 && w >= 4) || (w >= 6))
                {
                    w = 0;
                    h++;
                }
            }

            ui.AddChild(table);
            UpdateEssenceAvailability();
        }

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        internal void UpdateEssenceAvailability()
        {
            foreach (var essence in Essences)
            {
                List<string> localEssenceIds = [];
                localEssenceIds.AddRange(essenceIds);
                localEssenceIds.Remove(essence.ItemDisplay.QualifiedItemId);

                int x = 0;
                foreach (var item in Game1.player.Items.Where(i => i is not null))
                {
                    if (localEssenceIds.Contains(item.QualifiedItemId))
                    {
                        x += item.Stack;
                    }
                }
                essence.TransparentItemDisplay = x < TransmutationCost;

                var label = TransmutableLabel.FirstOrDefault(l => l.UserData == essence);
                label.String = GetTransmutableCountFor(essence.ItemDisplay).ToString();
            }
        }

        private int GetTransmutableCountFor(Item essence)
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
            return x / TransmutationCost;
        }

        private void DoTransmutation(Item essence)
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

            Game1.player.addItemToInventory(ItemRegistry.Create(essence.QualifiedItemId, 1));
            UpdateEssenceAvailability();
        }

        private bool DoesPlayerHaveSpaceForEssence(Item essence)
        {
            if (Game1.player.freeSpotsInInventory() > 0)
                return true;
            else if (Game1.player.Items.Any(i => i.canStackWith(essence) && i.Stack < 999))
                return true;
            else return false;
        }

        internal List<Item> GetAllEssences()
        {
            List<Item> essences = [];
            var itemids = ItemRegistry.GetObjectTypeDefinition().GetAllIds().Where(i => (ItemRegistry.Create($"(O){i}") is Object o && o.HasContextTag("essence_item")) || i == "768" || i == "769");
            
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

        public override void update(GameTime time)
        {
            base.update(time);
            ui.Update();
        }

        public override void draw(SpriteBatch b)
        {
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            base.draw(b);
            ui.Draw(b);

            SpriteText.drawStringWithScrollCenteredAt(b, I18n.Transmutation_Cost($"{TransmutationCost}"), xPositionOnScreen + width / 2, yPositionOnScreen - 64);

            drawMouse(b);
        }
    }
}
