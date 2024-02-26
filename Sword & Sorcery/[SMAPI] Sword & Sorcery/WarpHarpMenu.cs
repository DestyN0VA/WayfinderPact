using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwordAndSorcerySMAPI
{
    internal class WarpHarpMenu : IClickableMenu
    {
        private List<SongData.Note> playedNotes = new();

        public WarpHarpMenu()
        : base(0, 0, 200, 150) // todo size
        {
            Reposition();
        }

        private void Reposition()
        {
            xPositionOnScreen = (int)(Game1.player.Position.X - width / 2 + 32);
            yPositionOnScreen = (int)(Game1.player.Position.Y - height + 160);

            // Copied from BobberBar
            base.xPositionOnScreen -= Game1.viewport.X;
            base.yPositionOnScreen -= Game1.viewport.Y;
            if (base.xPositionOnScreen + 96 > Game1.viewport.Width)
            {
                base.xPositionOnScreen = Game1.viewport.Width - width;
            }
            else if (base.xPositionOnScreen < 0)
            {
                base.xPositionOnScreen = 0;
            }
            if (base.yPositionOnScreen < 0)
            {
                base.yPositionOnScreen = 0;
            }
            else if (base.yPositionOnScreen + height > Game1.viewport.Height)
            {
                base.yPositionOnScreen = Game1.viewport.Height - height;
            }
        }

        private void playNote(SongData.Note note)
        {
            int[] pitches = { 1100, 1300, 1500, 1800 };
            int pitch = pitches[(int)note];


            Game1.player.FarmerSprite.animateOnce(new[]
            {
                new FarmerSprite.AnimationFrame(98, 100, secondaryArm: false, flip: false),
                new FarmerSprite.AnimationFrame(99, 100, secondaryArm: false, flip: false),
                new FarmerSprite.AnimationFrame(100, 100, secondaryArm: false, flip: false),
            });
            Game1.player.FarmerSprite.PauseForSingleAnimation = true;

            DelayedAction.playSoundAfterDelay("miniharp_note", 50, Game1.player.currentLocation, Game1.player.Tile, pitch);

            playedNotes.Add(note);

            var songs = Game1.content.Load<Dictionary<string, SongData>>("DestyNova.SwordAndSorcery/HarpSongs");
            foreach (var song in songs)
            {
                if (playedNotes.SequenceEqual(song.Value.Notes) && GameStateQuery.CheckConditions( song.Value.UnlockCondition, Game1.player.currentLocation, Game1.player ) )
                {
                    DelayedAction.playSoundAfterDelay(song.Value.SongCue, 300, Game1.player.currentLocation, Game1.player.Tile);
                    DelayedAction.warpAfterDelay(song.Value.WarpLocationName, song.Value.WarpLocationTile.ToPoint(), 1800);
                    DelayedAction.functionAfterDelay(() =>
                    {
                        Game1.player.forceCanMove();
                        Game1.exitActiveMenu();
                    }, 2000);

                    return;
                }
            }

            if (playedNotes.Count >= 4)
            {
                DelayedAction.functionAfterDelay(() => Game1.drawObjectDialogue(I18n.Harp_BadSong()), 300);
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            SongData.Note? note = null;
            if (key == Game1.options.moveUpButton[0].key) note = SongData.Note.Up;
            if (key == Game1.options.moveDownButton[0].key) note = SongData.Note.Down;
            if (key == Game1.options.moveLeftButton[0].key) note = SongData.Note.Left;
            if (key == Game1.options.moveRightButton[0].key) note = SongData.Note.Right;

            if (note != null)
                playNote(note.Value);
        }

        public override void draw(SpriteBatch b)
        {
            Reposition();

            Game1.StartWorldDrawInUI(b);
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 12, yPositionOnScreen + 28 * 0 + 32, width-24, 1), Color.Black);
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 12, yPositionOnScreen + 28 * 1 + 32, width-24, 1), Color.Black);
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 12, yPositionOnScreen + 28 * 2 + 32, width-24, 1), Color.Black);
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 12, yPositionOnScreen + 28 * 3 + 32, width-24, 1), Color.Black);

            //SpriteText.drawString(b, "@`>", xPositionOnScreen, yPositionOnScreen);

            for (int i = 0; i < playedNotes.Count; ++i)
            {
                Texture2D tex = SpriteText.spriteTexture;
                Rectangle sourceRect = default(Rectangle);
                SpriteEffects fx = SpriteEffects.None;

                switch (playedNotes[i])
                {
                    case SongData.Note.Up:
                        sourceRect = ModSnS.instance.Helper.Reflection.GetMethod(typeof(SpriteText), "getSourceRectForChar").Invoke<Rectangle>('`', false);
                        break;
                    case SongData.Note.Down:
                        sourceRect = ModSnS.instance.Helper.Reflection.GetMethod(typeof(SpriteText), "getSourceRectForChar").Invoke<Rectangle>('`', false);
                        fx = SpriteEffects.FlipVertically;
                        break;
                    case SongData.Note.Left:
                        sourceRect = ModSnS.instance.Helper.Reflection.GetMethod(typeof(SpriteText), "getSourceRectForChar").Invoke<Rectangle>('@', false);
                        break;
                    case SongData.Note.Right:
                        sourceRect = ModSnS.instance.Helper.Reflection.GetMethod(typeof(SpriteText), "getSourceRectForChar").Invoke<Rectangle>('>', false);
                        break;
                }

                b.Draw(tex, new Vector2(xPositionOnScreen + 16 + i * 40, yPositionOnScreen + 28 * (int)playedNotes[i]), sourceRect, Color.White, 0, Vector2.Zero, Game1.pixelZoom, fx, 1);
            }

            Game1.EndWorldDrawInUI(b);
        }
    }
}
