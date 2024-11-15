using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI
{

    internal class AdventureBar : IClickableMenu
    {
        private bool editing;

        public static bool Hide = false;

        public AdventureBar( bool editing )
            : base( 0, (Game1.uiViewport.Height - 64 * 8 + 12 * 2) / 2, 64 * 2 + 12 * 2, 64 * 8 + 12 * 2 )
        {
            this.editing = editing;
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            yPositionOnScreen = (Game1.uiViewport.Height - 64 * 8 + 12 * 2) / 2;
        }

        public void tryPlace(ref Ability abil, int x, int y)
        {
            var ext = Game1.player.GetFarmerExtData();
            for (int ibar = 0; ibar < 2; ++ibar)
            {
                for (int islot = 0; islot < 8; ++islot)
                {
                    var pos = new Vector2(xPositionOnScreen + 12 + 64 * ibar, yPositionOnScreen + 12 + 64 * islot);
                    
                    if (new Rectangle(pos.ToPoint(), new Point(64, 64)).Contains(x, y))
                    {
                        ext.adventureBar[8 * ibar + islot] = abil?.Id;
                        abil = null;
                    }
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            if (!editing)
            {
                if (Hide)
                    return;
                if (Game1.activeClickableMenu != null || Game1.CurrentEvent != null)
                    return;
            }

            var ext = Game1.player.GetFarmerExtData();

            Ability hover = null;

            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 1f, drawShadow: false );
            for (int ibar = 0; ibar < 2; ++ibar)
            {
                for (int islot = 0; islot < 8; ++islot)
                {
                    var pos = new Vector2(xPositionOnScreen + 12 + 64 * ibar, yPositionOnScreen + 12 + 64 * islot);
                    b.Draw(Game1.menuTexture, pos, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
                    // tinyFont only supports digits :( -- TODO find custom font
                    b.DrawString(Game1.smallFont, (ibar == 0 ? "Ctrl+" : "Shift+") + $"{islot + 1}", pos + new Vector2(4, 4), Color.DimGray, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);

                    if (!Ability.Abilities.TryGetValue(ext.adventureBar[8 * ibar + islot] ?? "", out Ability abil))
                        continue;

                    var tex = Game1.content.Load<Texture2D>(abil.TexturePath);

                    Color col = Color.White;
                    if (ext.mana.Value < abil.ManaCost() || !abil.CanUse())
                        col *= 0.5f;

                    b.Draw(tex, pos, Game1.getSquareSourceRectForNonStandardTileSheet(tex, 16, 16, abil.SpriteIndex), col, 0, Vector2.Zero, 4, SpriteEffects.None, 1);

                    if  ( new Rectangle( pos.ToPoint(), new Point( 64, 64 ) ).Contains( Game1.getMouseX(), Game1.getMouseY() ) &&
                          GameStateQuery.CheckConditions(abil.KnownCondition, new(Game1.currentLocation, Game1.player, null, null, new Random())))
                    {
                        hover = abil;
                    }
                }
            }

            if (!editing)
            {
                Color color = Utility.StringToColor($"{ModSnS.Config.Red} {ModSnS.Config.Green} {ModSnS.Config.Blue}") ?? Color.Aqua;
                IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen + height - 12, width, 32 + 12 + 12, Color.White);
                float perc = 0;
                if (ext.maxMana.Value > 0)
                    perc = ext.mana.Value / (float)ext.maxMana.Value;
                if (perc > 1) perc = 1;
                if (perc > 0)
                {
                    b.Draw(Game1.staminaRect, new Rectangle(12, yPositionOnScreen + height, (int)((width - 24) * perc), 32), color);
                }
                Color textColor = Utility.StringToColor($"{ModSnS.Config.TextRed} {ModSnS.Config.TextGreen} {ModSnS.Config.TextBlue}") ?? Color.Black;
                string manaStr = $"{ext.mana}/{ext.maxMana}";
                b.DrawString(Game1.smallFont, manaStr, new Vector2(width / 2 - Game1.smallFont.MeasureString(manaStr).X / 2, yPositionOnScreen + height + 2), textColor);
            }

            if ( hover != null )
            {
                IClickableMenu.drawToolTip(b, hover.Description().Replace('^', '\n'), hover.Name(), null);
            }
        }
    }
}
