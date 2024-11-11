using System.Collections.Generic;
using StardewValley;

namespace SwordAndSorcerySMAPI.Alchemy
{
    internal static class AlchemyRecipes
    {
        public static Dictionary<string, AlchemyData> Get()
        {
            return Game1.content.Load<Dictionary<string,AlchemyData>>("DN.SnS/AlchemyRecipes");
        }
    }
}
