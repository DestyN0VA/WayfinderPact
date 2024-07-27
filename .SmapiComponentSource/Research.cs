using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore.UI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwordAndSorcerySMAPI
{
    public class ResearchEntry
    {
        public enum ResearchType
        {
            CraftingRecipe,
            Spell,
        }

        public ResearchType Type { get; set; }
        public string Value { get; set; } // Spell ID for spells, recipe ID for crafting recipe

        public class Page
        {
            public string Image { get; set; }
            public string Description { get; set; }
        }

        public string Name { get; set; }
        public List<Page> DocPages { get; set; } = new();
        public string IconTexture { get; set; }
        public Rectangle IconSourceRect { get; set; }

        public Dictionary<string, int> ResearchCosts { get; set; } = new();
    }

    public class ResearchMenu : IClickableMenu
    {
        public RootElement ui { get; set; }

        public Table ResearchList { get; set; }
        public Dictionary<string, StaticContainer> ResearchListRows { get; set; } = new();

        public StaticContainer DocPageParent { get; set; }
        public Label ResearchTitle { get; set; }
        public Label LeftPageButton { get; set; }
        public Label CurrentPageDisplay { get; set; }
        public Label RightPageButton { get; set; }
        public Label LearnButton { get; set; }

        public (string id, int page)? deferSwitch = null;

        public Dictionary<string, List<StaticContainer>> DocPages { get; set; } = new();
        public string CurrentResearch { get; set; }
        public int CurrentPage { get; set; } = 0;

        public ResearchMenu()
        : base(Game1.uiViewport.Width / 2 - 600, Game1.uiViewport.Height / 2 - 300, 1200, 600)
        {
            ui = new();

            var container = new StaticContainer()
            {
                LocalPosition = new(xPositionOnScreen, yPositionOnScreen),
                Size = new(width, height),
                //OutlineColor = Color.White,
            };
            ui.AddChild(container);

            ResearchList = new Table()
            {
                LocalPosition = new(24, 24),
                RowHeight = 85,
                Size = new(width / 2 - 48 - 25, height - 48),
            };
            container.AddChild(ResearchList);

            DocPageParent = new StaticContainer()
            {
                LocalPosition = new(width / 2 + 12, 12),
                Size = new(width / 2 - 24, height - 24),
                OutlineColor = Color.White,
            };
            container.AddChild(DocPageParent);

            ResearchTitle = new Label()
            {
                LocalPosition = new(0, 10),
                String = "",
                Bold = true,
            };
            DocPageParent.AddChild(ResearchTitle);

            LeftPageButton = new Label()
            {
                LocalPosition = new(10, 60),
                String = "@",
                Callback = (elem) => deferSwitch = new(CurrentResearch, CurrentPage - 1)
            };
            DocPageParent.AddChild(LeftPageButton);
            RightPageButton = new Label()
            {
                LocalPosition = new(DocPageParent.Width - 10 - Label.MeasureString( ">", bold: true ).X, 60),
                String = ">",
                Callback = (elem) => deferSwitch = new(CurrentResearch, CurrentPage + 1)
            };
            DocPageParent.AddChild(RightPageButton);
            CurrentPageDisplay = new Label()
            {
                LocalPosition = new(DocPageParent.Width / 2 - Label.MeasureString("0/0", bold: true).X / 2, 60),
                String = "",
            };
            DocPageParent.AddChild(CurrentPageDisplay);

            var size = Label.MeasureString(I18n.Research_Learn(), bold: true);
            LearnButton = new Label()
            {
                LocalPosition = new(DocPageParent.Width / 2 - size.X / 2, DocPageParent.Height - 10 - size.Y ),
                String = I18n.Research_Learn(),
                Bold = true,
            };
            DocPageParent.AddChild(LearnButton);

            foreach (var entry in ModTOP.Research)
            {
                StaticContainer row = new()
                {
                    Size = new(ResearchList.Width, ResearchList.RowHeight),
                };

                string name = null;
                switch (entry.Value.Type)
                {
                    case ResearchEntry.ResearchType.CraftingRecipe: name = I18n.Research_CraftingRecipe(entry.Value.Name); break;
                    case ResearchEntry.ResearchType.Spell: name = I18n.Research_Spell(entry.Value.Name); break;
                }

                row.AddChild(new Label()
                {
                    LocalPosition = new(10, 10),
                    String = name,
                    Bold = true,
                    UserData = entry.Key,
                    Callback = (elem) => deferSwitch = new((string)elem.UserData, 0),
                });

                if (!Game1.player.hasOrWillReceiveMail($"WitchcraftResearch_{entry.Key}"))
                {
                    var components = entry.Value.ResearchCosts.Keys.ToList();
                    components.Sort();

                    for (int i = 0; i < components.Count; ++i)
                    {
                        int qty = entry.Value.ResearchCosts[components[i]];

                        ItemWithBorder slot = new()
                        {
                            LocalPosition = new(ResearchList.Width / 2 - (components.Count) * 80 / 2 + 80 * i, 30),
                            ItemDisplay = ItemRegistry.Create(components[i], qty),
                            TransparentItemDisplay = Game1.player.Items.CountId(components[i]) >= qty ? false : true,
                            BoxColor = null,
                        };

                        row.AddChild(slot);
                    }
                }
                else
                {
                    row.AddChild(new Label()
                    {
                        LocalPosition = new(ResearchList.Width / 2 - Label.MeasureString(I18n.Research_KnownParenthesis()).X / 2, 50),
                        String = I18n.Research_KnownParenthesis(),
                    });
                }

                Element[] rowArray = [ row ];
                ResearchList.AddRow(rowArray);
                ResearchListRows.Add(entry.Key, row);

                DocPages.Add(entry.Key, new());
                foreach (var docEntry in entry.Value.DocPages)
                {
                    StaticContainer page = new()
                    {
                        LocalPosition = new(0, CurrentPageDisplay.LocalPosition.Y + CurrentPageDisplay.Height + 50),
                        Size = new(DocPageParent.Width, LearnButton.LocalPosition.Y - ( CurrentPageDisplay.LocalPosition.Y + CurrentPageDisplay.Height)),
                    };

                    int yOffset = 0;
                    if (docEntry.Image != null)
                    {
                        Image img = new()
                        {
                            Texture = Game1.content.Load<Texture2D>(docEntry.Image),
                            Scale = 1,
                        };
                        img.LocalPosition = new(page.Width / 2 - img.Width / 2, 0);
                        page.AddChild(img);
                        yOffset = img.Height;
                    }

                    Label desc = new()
                    {
                        LocalPosition = new(25, yOffset),
                        String = docEntry.Description == null ? "" : Game1.parseText( docEntry.Description.Replace( "^", "\n" ), Game1.dialogueFont, page.Width - 50 )
                    };
                    page.AddChild(desc);

                    DocPages[entry.Key].Add(page);
                }
            }
        }

        public void SwitchToResearch(string id, int page = 0)
        {
            if (id == null) return;

            if (page < 0) page = 0;
            if (page >= DocPages[id].Count) page = DocPages[id].Count - 1;

            foreach (var child in DocPageParent.Children.ToList())
            {
                if (child == ResearchTitle || child == LeftPageButton ||
                    child == CurrentPageDisplay || child == RightPageButton ||
                    child == LearnButton)
                    continue;

                DocPageParent.RemoveChild(child);
            }

            CurrentResearch = id;
            ResearchTitle.String = ModTOP.Research[id].Name;
            ResearchTitle.LocalPosition = new(ResearchTitle.Parent.Width / 2 - Label.MeasureString(ResearchTitle.String, bold: true).X / 2, ResearchTitle.LocalPosition.Y);
            CurrentPage = page;
            CurrentPageDisplay.String = $"{page + 1}/{DocPages[id].Count}";
            DocPageParent.AddChild(DocPages[id][CurrentPage]);

            if (CanLearn(id))
            {
                LearnButton.Callback = (elem) => Learn();
            }
            else
            {
                LearnButton.Callback = null;
            }
        }

        private bool CanLearn(string id)
        {
            if (Game1.player.hasOrWillReceiveMail($"WitchcraftResearch_{id}"))
                return false;

            foreach (var cost in ModTOP.Research[id].ResearchCosts)
            {
                if (Game1.player.Items.CountId(cost.Key) < cost.Value)
                    return false;
            }

            return true;
        }

        public void Learn()
        {
            Game1.player.mailReceived.Add($"WitchcraftResearch_{CurrentResearch}");

            if (ModTOP.Research[CurrentResearch].Type == ResearchEntry.ResearchType.CraftingRecipe)
            {
                Game1.player.craftingRecipes.Add(ModTOP.Research[CurrentResearch].Value, 0);
            }

            foreach (var entry in ResearchListRows[CurrentResearch].Children.ToList())
            {
                if (entry is ItemWithBorder item)
                    ResearchListRows[CurrentResearch].RemoveChild(item);
            }

            ResearchListRows[CurrentResearch].AddChild(new Label()
            {
                LocalPosition = new(ResearchList.Width / 2 - Label.MeasureString(I18n.Research_KnownParenthesis()).X / 2, 50),
                String = I18n.Research_KnownParenthesis(),
            });

            LearnButton.Callback = null;
        }

        public override void update(GameTime time)
        {
            base.update(time);

            ui.Update();

            if (deferSwitch != null)
            {
                SwitchToResearch(deferSwitch.Value.id, deferSwitch.Value.page);
                deferSwitch = null;
            }
        }

        public override void draw(SpriteBatch b)
        {
            ui.Draw(b);

            if (CurrentResearch != null && !CanLearn(CurrentResearch) && LearnButton.Hover)
            {
                if (Game1.player.hasOrWillReceiveMail($"WitchcraftResearch_{CurrentResearch}"))
                    drawToolTip(b, I18n.Research_Known(), null, null);
                else
                    drawToolTip(b, I18n.Research_MissingComponents(), null, null);
            }

            drawMouse(b);
        }

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            ResearchList.Scrollbar.ScrollBy(direction / -120);
        }
    }
}
