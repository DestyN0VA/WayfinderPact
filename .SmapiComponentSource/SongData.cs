using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace SwordAndSorcerySMAPI
{
    public class SongData
    {
        public enum Note
        {
            Up,
            Down,
            Left,
            Right,
        }

        public string DisplayName { get; set; }

        public string WarpLocationName { get; set; }
        public Vector2 WarpLocationTile { get; set; }

        public List<Note> Notes { get; set; } = [];
        public string SongCue { get; set; } = "clank";

        public string UnlockCondition { get; set; }
    }
}
