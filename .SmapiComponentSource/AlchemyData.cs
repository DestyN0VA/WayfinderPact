using System.Collections.Generic;

namespace SwordAndSorcerySMAPI
{
    internal class AlchemyData
    {
        public string OutputItem { get; set; }
        public int OutputQuantity { get; set; } = 1;
        public Dictionary<string, int> Ingredients { get; set; }
        public string UnlockConditions { get; set; }
    }
}
