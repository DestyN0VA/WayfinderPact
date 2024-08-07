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
using static System.Net.Mime.MediaTypeNames;
using Image = SpaceCore.UI.Image;

namespace SwordAndSorcerySMAPI
{
    public class AdventureBarConfigureMenu : IClickableMenu
    {
        private RootElement ui;
        private List<Image> abilImages = new();
        private Ability held;

        private AdventureBar bar;

        private Image grimoire;

        public AdventureBarConfigureMenu(bool everything = false)
            : base(Game1.uiViewport.Width / 2 - 432 / 2, Game1.uiViewport.Height / 2 - 432 / 2, 432, 432, true)
        {
            ui = new RootElement();

            var container = new StaticContainer()
            {
                LocalPosition = new Vector2( xPositionOnScreen, yPositionOnScreen ),
                Size = new Vector2(432, 432),
                OutlineColor = Color.White,
            };
            ui.AddChild(container);

            var abils = Ability.Abilities.Values.ToList();
            int ip = 0;
            for ( int i = 0; i < abils.Count; ++i)
            {
                int ix = ip % 6;
                int iy = ip / 6;

                var tex = Game1.content.Load<Texture2D>(abils[i].TexturePath);
                bool known = GameStateQuery.CheckConditions(abils[i].KnownCondition, new(Game1.currentLocation, Game1.player, null, null, new Random()));
                if (!known && abils[i].HiddenIfLocked)
                    continue;

                ++ip;

                var img = new Image()
                {
                    LocalPosition = new Vector2(4 + ix * 72, 4 + iy * 72),
                    Texture = tex,
                    TexturePixelArea = Game1.getSquareSourceRectForNonStandardTileSheet(tex, 16, 16, abils[i].SpriteIndex),
                    Scale = 4,
                    DrawColor = Color.White * (known ? 1 : 0.5f),
                    Callback = (elem) => { if (known) held = (Ability) elem.UserData; },
                    UserData = abils[i],
                };
                container.AddChild(img);
                abilImages.Add(img);
            }

            if (Game1.player.eventsSeen.Contains(ModTOP.WitchcraftUnlock))
            {
                grimoire = new()
                {
                    LocalPosition = new(width + 32, 32),
                    Texture = ModTOP.Grimoire,
                    Scale = 4,
                    Callback = (elem) => SetChildMenu(new ResearchMenu()),
                };
                container.AddChild(grimoire);
            }


            bar = new AdventureBar(editing: true);
            bar.xPositionOnScreen = xPositionOnScreen - bar.width - 12;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            if (held != null)
            {
                bar.tryPlace(ref held, x, y);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);
            Ability abil = null;
            bar.tryPlace(ref abil, x, y);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            ui.Update();
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            ui.Draw(b);
            bar.draw(b);

            foreach ( var img in abilImages )
            {
                if (img.Hover)
                {
                    bool known = img.DrawColor.A == 255;
                    drawToolTip(b, known ? (img.UserData as Ability).Description().Replace('^', '\n') : (img.UserData as Ability).UnlockHint(), known ? (img.UserData as Ability).Name() : "???", null);
                }
            }

            if (held != null)
            {
                var tex = Game1.content.Load<Texture2D>(held.TexturePath);
                b.Draw(tex, Game1.getMousePosition().ToVector2() + new Vector2( 32, 32 ), Game1.getSquareSourceRectForNonStandardTileSheet(tex, 16, 16, held.SpriteIndex), Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1);
            }

            if (grimoire?.Hover ?? false)
            {
                drawHoverText(b, I18n.OpenGrimoire(), Game1.dialogueFont);
            }

            drawMouse(b);
        }
    }
}
